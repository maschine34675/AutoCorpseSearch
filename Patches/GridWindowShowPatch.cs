using EFT;
using EFT.InventoryLogic;
using EFT.Settings.Game;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AutoCorpseSearch
{
    internal class AcsGridWindowShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridWindow), nameof(GridWindow.Show), new[]
            {
                typeof(CompoundItem),
                typeof(ItemContext),
                typeof(ItemController),
                typeof(ItemUiContext),
                typeof(GameSettingsGroup.EPriorityWindowMode),
            });
        }

        [PatchPostfix]
        static void Postfix(CompoundItem compoundItem, ItemController itemController)
        {
            if (!Plugin.SearchOnOpen) return;
            if (!(compoundItem is SearchableItem searchable)) return;
            var psc = itemController?.SearchController as IPlayerSearchController;
            if (psc == null || !psc.CanSearch) return;

            if (Plugin.OnlyOwnContainers && !Plugin.IsOwnedByPlayer(searchable, itemController)) return;
            if (!Plugin.NeedsSearch(psc, searchable)) return;

            Plugin.EnqueueSingle(psc, searchable, SearchOrigin.Open);
        }
    }
}
