using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace AutoCorpseSearch
{
    internal class AcsInventoryScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m =>
                    m.Name == "Show" &&
                    m.GetParameters().Any(p => p.ParameterType == typeof(CompoundItem)));
        }

        [PatchPostfix]
        static void Postfix(InventoryController controller, CompoundItem lootItem)
        {
            var psc = controller?.SearchController as IPlayerSearchController;
            if (psc == null) return;

            var playerController = controller as Player.PlayerInventoryController;
            var ownEquipment = playerController?.Inventory?.Equipment;
            if (lootItem is InventoryEquipment equipment && ownEquipment != equipment)
            {
                Plugin.EnqueueCorpseChain(psc, equipment);
            }
            else if (Plugin.ResumeContainerSearch
                && lootItem is SearchableItem searchable
                && psc.IsSearched(searchable)
                && psc.ContainsUnknownItems(searchable))
            {
                Plugin.EnqueueSingle(psc, searchable, SearchOrigin.LootScreen);
            }
            if (Plugin.SearchEquippedOnInventoryOpen && ownEquipment != null)
            {
                var sweep = new SearchJob
                {
                    Psc = psc,
                    Origin = SearchOrigin.EquippedSweep,
                    AbortOnUserCancel = true,
                };

                foreach (var slotType in Plugin.CarriedContainerSlots)
                {
                    var equipped = Plugin.GetSlotSafe(ownEquipment, slotType)?.ContainedItem as SearchableItem;
                    if (equipped != null && Plugin.NeedsSearch(psc, equipped))
                        sweep.Items.Add(equipped);
                }

                Plugin.Enqueue(sweep);
            }
        }
    }

    internal class AcsInventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Close));
        }

        [PatchPrefix]
        static void Prefix()
        {
            Plugin.CancelScreenBoundJobs();
        }
    }
}
