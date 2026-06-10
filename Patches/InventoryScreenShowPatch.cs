using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace AutoCorpseSearch
{
    internal class InventoryScreenShowPatch : ModulePatch
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
            if (lootItem is not InventoryEquipment equipment) return;

            var playerController = controller as Player.PlayerInventoryController;
            if (playerController?.Inventory?.Equipment == equipment) return;

            var psc = controller?.SearchController as IPlayerSearchController;
            if (psc == null) return;

            Plugin.StartSequentialSearch(psc, equipment);
        }
    }

    internal class InventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Close));
        }

        [PatchPrefix]
        static void Prefix()
        {
            Plugin.CancelSearch();
        }
    }
}
