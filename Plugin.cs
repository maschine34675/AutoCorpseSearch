using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoCorpseSearch
{
    internal enum SearchOrigin
    {
        Corpse,
        LootScreen,
        Equip,
        Open,
        EquippedSweep,
    }
    internal sealed class SearchJob
    {
        public IPlayerSearchController Psc;
        public readonly List<SearchableItem> Items = new List<SearchableItem>();
        public SearchOrigin Origin;
        public bool AbortOnUserCancel;
        public bool Cancelled;
        public bool AllowPartialResume = true;
        public bool IsScreenBound =>
            Origin == SearchOrigin.Corpse
            || Origin == SearchOrigin.LootScreen
            || Origin == SearchOrigin.EquippedSweep;
    }

    [BepInPlugin("com.maschine.AutoCorpseSearch", "maschine-AutoCorpseSearch", "2.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log;

        private static ConfigEntry<int> _tacticalVestOrder;
        private static ConfigEntry<int> _pocketsOrder;
        private static ConfigEntry<int> _backpackOrder;
        private static ConfigEntry<int> _armBandOrder;
        private static ConfigEntry<bool> _resumePartialSearch;
        private static ConfigEntry<bool> _resumeContainerSearch;
        private static ConfigEntry<bool> _useDoubleSearch;
        private static ConfigEntry<bool> _searchOnEquip;
        private static ConfigEntry<bool> _searchOnOpen;
        private static ConfigEntry<bool> _searchEquippedOnInventoryOpen;
        private static ConfigEntry<bool> _onlyOwnContainers;

        public static bool ResumeContainerSearch => _resumeContainerSearch.Value;
        public static bool SearchOnEquip => _searchOnEquip.Value;
        public static bool SearchOnOpen => _searchOnOpen.Value;
        public static bool SearchEquippedOnInventoryOpen => _searchEquippedOnInventoryOpen.Value;
        public static bool OnlyOwnContainers => _onlyOwnContainers.Value;
        private const float OperationWaitTimeout = 300f;

        private static readonly Queue<SearchJob> _queue = new Queue<SearchJob>();
        private static SearchJob _current;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            const string orderSection = "Search Order";
            const string orderDesc = "Priority order (lower number = searched first)";

            _tacticalVestOrder = Config.Bind(orderSection, "Chest Rig", 1, orderDesc);
            _pocketsOrder = Config.Bind(orderSection, "Pockets", 2, orderDesc);
            _backpackOrder = Config.Bind(orderSection, "Backpack", 3, orderDesc);
            _armBandOrder = Config.Bind(
                orderSection,
                "Belt (Armband Slot)",
                4,
                orderDesc + ". Only does anything with a belt mod such as PackNStrap installed, "
                + "which carries its belts in the armband slot. A plain armband is not searchable "
                + "and is skipped.");

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

            _useDoubleSearch = Config.Bind(
                "General",
                "Use Double Search",
                true,
                "With the Attention elite skill the game allows two searches at once. On: the mod fills "
                + "both slots, so looting goes faster, but while it does the search button on other "
                + "containers stays greyed out. Off: the mod keeps to one search and leaves the second "
                + "slot free for you. Without the elite skill this changes nothing.");

            const string pickedUpSection = "Picked-up Containers";

            _searchOnEquip = Config.Bind(
                pickedUpSection,
                "Search On Equip",
                true,
                "Automatically search a rig, backpack or belt the moment you equip it.");

            _searchOnOpen = Config.Bind(
                pickedUpSection,
                "Search On Open",
                true,
                "Automatically search a container when you open it in its own window.");

            _searchEquippedOnInventoryOpen = Config.Bind(
                pickedUpSection,
                "Search Equipped On Inventory Open",
                true,
                "Catch-up: search an already equipped rig, backpack or belt that is still "
                + "unsearched whenever the inventory screen opens.");

            _onlyOwnContainers = Config.Bind(
                pickedUpSection,
                "Only Own Containers",
                true,
                "Restrict 'Search On Open' to containers you carry yourself. Turn off to also "
                + "auto-search containers you open on a corpse or on the ground.");

            new AcsInventoryScreenShowPatch().Enable();
            new AcsInventoryScreenClosePatch().Enable();
            new AcsPlayerItemAddedPatch().Enable();
            new AcsGridWindowShowPatch().Enable();
            StartCoroutine(Pump());

            Log.LogInfo("AutoCorpseSearch loaded.");
        }
        internal static readonly EquipmentSlot[] CarriedContainerSlots =
        {
            EquipmentSlot.TacticalVest,
            EquipmentSlot.Backpack,
            EquipmentSlot.ArmBand,
        };

        public static EquipmentSlot[] GetOrderedSlots()
        {
            var slots = new (EquipmentSlot slot, int order)[]
            {
                (EquipmentSlot.TacticalVest, _tacticalVestOrder.Value),
                (EquipmentSlot.Pockets,      _pocketsOrder.Value),
                (EquipmentSlot.Backpack,     _backpackOrder.Value),
                (EquipmentSlot.ArmBand,      _armBandOrder.Value),
            };
            return slots.OrderBy(s => s.order).Select(s => s.slot).ToArray();
        }
        internal static Slot GetSlotSafe(InventoryEquipment equipment, EquipmentSlot slotType)
        {
            if (equipment == null) return null;

            try
            {
                return equipment.GetSlot(slotType);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
        public static bool NeedsSearch(IPlayerSearchController psc, SearchableItem item)
        {
            return NeedsSearch(psc, item, _resumePartialSearch.Value);
        }

        internal static bool NeedsSearch(IPlayerSearchController psc, SearchableItem item, bool allowPartialResume)
        {
            if (psc == null || item == null) return false;

            return !psc.IsSearched(item)
                || (allowPartialResume && psc.ContainsUnknownItems(item));
        }
        public static bool IsOwnedByPlayer(Item item, ItemController viewingController)
        {
            var equipment = (viewingController as Player.PlayerInventoryController)?.Inventory?.Equipment;
            if (equipment == null) return false;
            var owner = item?.Owner;
            return owner != null && ReferenceEquals(owner.RootItem, equipment);
        }

        internal static void EnqueueSingle(IPlayerSearchController psc, SearchableItem item, SearchOrigin origin)
        {
            var job = new SearchJob { Psc = psc, Origin = origin };
            job.Items.Add(item);
            Enqueue(job);
        }

        public static void EnqueueCorpseChain(IPlayerSearchController psc, InventoryEquipment equipment)
        {
            var job = new SearchJob
            {
                Psc = psc,
                Origin = SearchOrigin.Corpse,
                AbortOnUserCancel = true,
            };

            foreach (var slotType in GetOrderedSlots())
            {
                var item = GetSlotSafe(equipment, slotType)?.ContainedItem as SearchableItem;
                if (item != null && NeedsSearch(psc, item))
                    job.Items.Add(item);
            }

            Enqueue(job);
        }

        internal static void Enqueue(SearchJob job)
        {
            if (job?.Psc == null) return;

            job.AllowPartialResume = job.Origin == SearchOrigin.LootScreen || _resumePartialSearch.Value;
            job.Items.RemoveAll(item => item == null
                || job.Psc.SearchOperations.Any(op => op.Item == item)
                || AlreadyQueued(item));

            if (job.Items.Count == 0) return;

            _queue.Enqueue(job);
        }

        private static bool AlreadyQueued(SearchableItem item)
        {
            if (_current != null && !_current.Cancelled && _current.Items.Contains(item))
                return true;

            foreach (var job in _queue)
            {
                if (job.Items.Contains(item)) return true;
            }
            return false;
        }
        public static void CancelScreenBoundJobs()
        {
            if (_current != null && _current.IsScreenBound)
                _current.Cancelled = true;

            if (_queue.Count == 0) return;

            var kept = _queue.Where(job => !job.IsScreenBound).ToList();
            _queue.Clear();
            foreach (var job in kept)
                _queue.Enqueue(job);
        }

        private static IEnumerator Pump()
        {
            while (true)
            {
                if (_queue.Count == 0)
                {
                    yield return null;
                    continue;
                }

                _current = _queue.Dequeue();
                try
                {
                    var awaitingVerdict = new List<SearchableItem>();

                    foreach (var item in _current.Items)
                    {
                        bool timedOut = false;
                        float deadline = Time.unscaledTime + OperationWaitTimeout;

                        while (ShouldKeepWaiting(_current))
                        {
                            if (Time.unscaledTime > deadline)
                            {
                                timedOut = true;
                                break;
                            }
                            yield return null;
                        }
                        if (timedOut)
                        {
                            Log.LogWarning("Gave up waiting for a free search slot; dropping the rest of this job.");
                            break;
                        }

                        var step = TryStartSearch(_current, item, awaitingVerdict);
                        if (step == StepResult.Stop) break;
                        if (step == StepResult.Started) awaitingVerdict.Add(item);
                    }
                }
                finally
                {
                    _current = null;
                }
            }
        }

        private enum StepResult
        {
            Started,
            Skip,
            Stop,
        }
        private static bool ShouldKeepWaiting(SearchJob job)
        {
            try
            {
                return !job.Cancelled && JobValid(job) && !HasFreeSlot(job.Psc);
            }
            catch (Exception ex)
            {
                Log.LogError("Search-state check failed, continuing: " + ex);
                return false;
            }
        }
        private static bool HasFreeSlot(IPlayerSearchController psc)
        {
            return _useDoubleSearch.Value
                ? psc.CanStartNewSearchOperation()
                : !psc.SearchOperations.Any();
        }
        private static bool AnyInterrupted(SearchJob job, List<SearchableItem> awaitingVerdict)
        {
            for (int i = awaitingVerdict.Count - 1; i >= 0; i--)
            {
                var started = awaitingVerdict[i];

                if (job.Psc.SearchOperations.Any(op => op.Item == started)) continue;

                if (!job.Psc.IsSearched(started) || job.Psc.ContainsUnknownItems(started))
                    return true;

                awaitingVerdict.RemoveAt(i);
            }
            return false;
        }

        private static StepResult TryStartSearch(SearchJob job, SearchableItem item, List<SearchableItem> awaitingVerdict)
        {
            try
            {
                if (job.Cancelled || !JobValid(job)) return StepResult.Stop;
                if (job.AbortOnUserCancel && AnyInterrupted(job, awaitingVerdict))
                    return StepResult.Stop;
                if (item == null || item.CurrentAddress == null) return StepResult.Skip;

                if (!NeedsSearch(job.Psc, item, job.AllowPartialResume)) return StepResult.Skip;

                job.Psc.SearchContents(item);
                return StepResult.Started;
            }
            catch (Exception ex)
            {
                Log.LogError("Search step failed, dropping the rest of this job: " + ex);
                return StepResult.Stop;
            }
        }
        private static bool JobValid(SearchJob job)
        {
            if (job?.Psc == null) return false;
            if (!Singleton<GameWorld>.Instantiated) return false;

            var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null) return false;

            return ReferenceEquals(job.Psc, mainPlayer.SearchController) && job.Psc.CanSearch;
        }
    }
}
