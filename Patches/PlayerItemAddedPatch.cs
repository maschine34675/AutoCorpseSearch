using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AutoCorpseSearch
{
    internal class AcsPlayerItemAddedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnItemAddedOrRemoved));
        }

        [PatchPostfix]
        static void Postfix(Player __instance, Item item, ItemAddress location, bool added)
        {
            if (!added || !Plugin.SearchOnEquip) return;
            if (__instance == null || !__instance.IsYourPlayer) return;
            if (!(item is SearchableItem searchable)) return;
            if (!(location is SlotItemAddress slotAddress)) return;

            var equipment = __instance.InventoryController?.Inventory?.Equipment;
            if (equipment == null) return;
            bool isCarriedContainerSlot = false;
            foreach (var slotType in Plugin.CarriedContainerSlots)
            {
                if (slotAddress.Slot == Plugin.GetSlotSafe(equipment, slotType))
                {
                    isCarriedContainerSlot = true;
                    break;
                }
            }
            if (!isCarriedContainerSlot) return;

            var psc = __instance.SearchController;
            if (psc == null || !psc.CanSearch) return;
            if (!Plugin.NeedsSearch(psc, searchable)) return;
            Plugin.EnqueueSingle(psc, searchable, SearchOrigin.Equip);
        }
    }
}
