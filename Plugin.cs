using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AutoCorpseSearch
{
    [BepInPlugin("com.maschine.AutoCorpseSearch", "maschine-AutoCorpseSearch", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log;

        private static ConfigEntry<int> _tacticalVestOrder;
        private static ConfigEntry<int> _pocketsOrder;
        private static ConfigEntry<int> _backpackOrder;
        private static ConfigEntry<bool> _resumePartialSearch;
        private static ConfigEntry<bool> _resumeContainerSearch;

        public static bool ResumeContainerSearch => _resumeContainerSearch.Value;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            const string section = "Search Order";
            const string desc = "Priority order (lower number = searched first)";

            _tacticalVestOrder = Config.Bind(section, "Chest Rig", 1, desc);
            _pocketsOrder = Config.Bind(section, "Pockets", 2, desc);
            _backpackOrder = Config.Bind(section, "Backpack", 3, desc);

            _resumePartialSearch = Config.Bind(
                "General",
                "Resume Partial Search",
                true,
                "Automatically resume searching slots that were interrupted (items still hidden).");

            _resumeContainerSearch = Config.Bind(
                "General",
                "Resume Container Searches",
                true,
                "Automatically resume interrupted searches on regular loot containers when re-opening them.");

            new InventoryScreenShowPatch().Enable();
            new InventoryScreenClosePatch().Enable();

            Log.LogInfo("AutoCorpseSearch loaded.");
        }

        public static EquipmentSlot[] GetOrderedSlots()
        {
            var slots = new (EquipmentSlot slot, int order)[]
            {
                (EquipmentSlot.TacticalVest, _tacticalVestOrder.Value),
                (EquipmentSlot.Pockets,      _pocketsOrder.Value),
                (EquipmentSlot.Backpack,     _backpackOrder.Value),
            };
            return slots.OrderBy(s => s.order).Select(s => s.slot).ToArray();
        }

        private static Coroutine _activeSearch;

        public static void StartSequentialSearch(IPlayerSearchController psc, InventoryEquipment equipment)
        {
            CancelSearch();
            _activeSearch = Instance.StartCoroutine(SearchCorpseSlots(psc, equipment));
        }

        public static void CancelSearch()
        {
            if (_activeSearch == null) return;
            Instance.StopCoroutine(_activeSearch);
            _activeSearch = null;
        }

        private static IEnumerator SearchCorpseSlots(IPlayerSearchController psc, InventoryEquipment equipment)
        {
            SearchableItemItemClass previous = null;

            foreach (var slotType in GetOrderedSlots())
            {
                var item = equipment.GetSlot(slotType)?.ContainedItem as SearchableItemItemClass;
                if (item == null) continue;

                bool needsSearch = !psc.IsSearched(item)
                    || (_resumePartialSearch.Value && psc.ContainsUnknownItems(item));
                if (!needsSearch) continue;

                while (previous != null && psc.SearchOperations.Any(op => op.Item == previous))
                    yield return null;

                if (previous != null && psc.ContainsUnknownItems(previous))
                    yield break;

                if (!psc.IsSearched(item) || psc.ContainsUnknownItems(item))
                {
                    psc.SearchContents(item);
                    previous = item;
                }
            }

            _activeSearch = null;
        }
    }
}
