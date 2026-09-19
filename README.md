AutoCorpseSearch

Takes the clicking out of searching. Corpses get searched slot by slot the moment you open them, and
rigs and backpacks you pick up get searched when you put them on or open them.

Features

Auto-search on open — the moment you open a corpse's inventory, searching begins automatically on all
equipped containers (Chest Rig, Pockets, Backpack)

Configurable search order — set the priority of each slot via the BepInEx F12 config menu
(default: Chest Rig → Pockets → Backpack)

Search picked-up rigs and backpacks — a rig or backpack you equip is searched right away, including
when you grab one off the ground with the use key and never open the inventory at all. Opening one in
its own window searches it too, and an unsearched one you are already wearing is picked up the next
time you open your inventory.

Belt mods supported — belts from PackNStrap and anything else that carries a container in the armband
slot are searched exactly like a rig: on a corpse, when you equip one, and when you open one. Bots wear
them too, so their belts are part of the corpse chain. Nothing needs to be configured and the mod does
not require the belt mod to be installed.

Resume partial searches — optionally re-start searching containers that were interrupted and still
have hidden items (toggle in F12 menu, on by default)

Manual cancel is respected — cancelling a search mid-container stops the rest of that chain

Inventory-close safe — closing the loot screen drops everything queued for it; nothing keeps searching
a corpse in the background

Never over the limit — every trigger feeds a single queue that only starts a search when a search slot
is free, and it counts searches the mod did not start, so nothing stacks on top of one you began

Uses the double-search elite skill — with Attention on elite the game allows two searches at once, and
the mod fills both. Switch it off if you would rather keep the second slot for searching by hand

Configuration (F12 → AutoCorpseSearch)

Search Order
  Chest Rig / Pockets / Backpack — search priority for corpse slots, lower number goes first
  Belt (Armband Slot) — same, for belt mods that carry their belts in the armband slot. Without such a
                        mod the slot holds a plain armband, which is not searchable, so it is skipped

General
  Resume Partial Search — re-search containers that still have hidden items
  Resume Container Searches — resume an interrupted search when you re-open a loot container
  Use Double Search — with the Attention elite skill, run two searches at once (on by default). While
                      the mod holds both slots the search button on other containers is greyed out;
                      turn it off to keep one slot free. Without the elite skill it changes nothing

Picked-up Containers
  Search On Equip — search a rig, backpack or belt the moment you equip it
  Search On Open — search a container when you open it in its own window
  Search Equipped On Inventory Open — catch up on equipped gear that is still unsearched
  Only Own Containers — restrict "Search On Open" to containers you carry yourself; turn it off to also
                        auto-search containers you open on a corpse or on the ground

Notes

Ctrl+click quick-move does not count as equipping — the game routes it differently from a real equip,
so it does not trigger a search. Equip it normally, or open it, and it will.

Closing the inventory screen while a rig you are wearing is being searched cancels that search. That is
the game's own behaviour for any search, not something the mod can hold open. Re-opening the inventory
starts it again.

With two searches running, cancelling one of them stops the rest of that chain from starting, but the
other search that is already running keeps going. Cancel that one too if you want everything to stop.

A corpse is always searched before your own gear. If you open a corpse while still carrying an
unsearched rig, the corpse's containers go first and the catch-up on your own gear waits its turn.
