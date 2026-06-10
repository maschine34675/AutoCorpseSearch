\# AutoCorpseSearch



Automatically starts searching a corpse's equipment slots when you open the loot screen — no more clicking "Search" on each slot individually.



\## Features



\- \*\*Auto-search on open\*\* — the moment you open a corpse's inventory, searching begins automatically on all equipped containers (Chest Rig, Pockets, Backpack)

\- \*\*Configurable search order\*\* — set the priority of each slot via the BepInEx F12 config menu (default: Chest Rig → Pockets → Backpack)

\- \*\*Resume partial searches\*\* — optionally re-start searching slots that were interrupted and still have hidden items (toggle in F12 menu, on by default)

\- \*\*Manual cancel is respected\*\* — cancelling a search mid-slot stops the entire chain; no slot is searched without your input

\- \*\*Inventory-close safe\*\* — closing the inventory immediately stops any pending searches; nothing runs in the background

\- \*\*Skills Extended compatible\*\* — works correctly with the double-search elite skill



\## Requirements



\- SPT (tested on 3.x)

\- BepInEx



\## Installation



Drop `AutoCorpseSearch.dll` into `BepInEx/plugins/`.



\## Configuration



Open the F12 menu in-game → \*\*AutoCorpseSearch\*\*



| Setting | Default | Description |

|---|---|---|

| Chest Rig | 1 | Search priority (lower = first) |

| Pockets | 2 | Search priority |

| Backpack | 3 | Search priority |

| Resume Partial Search | Enabled | Re-search slots with hidden items remaining |

