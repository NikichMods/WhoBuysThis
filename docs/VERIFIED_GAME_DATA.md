# Verified Game Data

Target: Graveyard Keeper 1.407 (PC).

This is the durable evidence ledger for facts that production code may rely on. Facts marked **verified** are supported by the 1.407 decompile and/or established open-source mod code. Items marked **runtime-open** still require an installed-game probe before production architecture is frozen.

## Evidence baseline

Primary static game-code evidence:
- `Kupie/GYK_DECOMP`, ref `6abf79199d92482af1c7573870dd9a20ec2270b9`.
- `Assembly-CSharp/LazyConsts.cs` at that ref reports `VERSION => 1.407f`, matching the project target.

Open-source mod evidence:
- `p1xel8ted/Graveyard-Keeper-Mods`, ref `ebac55b3fe58402ae7cd5c061d9eb5b0c8e610eb`.
- Historical supporting example: `qaweofghasdlhtge/GYK-Mods-QMod`, ref `c721a986e03264e52307543ebae132e976a70d6d`.

Do not treat wiki tables or manually curated item/vendor mappings as authoritative when native balance/runtime data can answer the question.

## 1. Vendor data source

**Verified:** `GameBalance` owns `List<VendorDefinition> vendors_data`. Its balance-table registry exposes this collection under `"Vendors"`.

Evidence:
- `Assembly-CSharp/GameBalance.cs`
- `Assembly-CSharp/GameBalanceBase.cs`

`GameBalance.LoadGameBalance()` loads the `game_data` resource and builds ID caches. A vendor definition can therefore be retrieved by ID through `GameBalance.me.GetDataOrNull<VendorDefinition>(id)`.

**Important distinction:** `vendors_data` is the complete set of vendor *definitions in the loaded balance*. It is not yet proven to be identical to the set of currently reachable/active merchants for the player's installed DLC and save state. That active-set question is runtime-open.

## 2. Native rule for a vendor buying an item

**Verified:** the actual player -> merchant sale filter uses:

`Trading.BuyableItemsFilter -> Vendor.CanBuyItem(itemDefinition, true)`

Evidence:
- `Assembly-CSharp/Trading.cs`

`Vendor.CanBuyItem(ItemDefinition item_def, bool check_tier = false)`:
1. rejects null items / items with no `product_types`;
2. when `check_tier == true`, rejects `item_def.product_tier > vendor.cur_tier`;
3. requires at least one item product type accepted by `VendorDefinition.GetProductTypes()`;
4. checks vendor-specific `not_buying` entries:
   - matching entry with `tier < 1` rejects the item;
   - matching entry whose `tier == current vendor tier` rejects the item.

Evidence:
- `Assembly-CSharp/Vendor.cs`
- `Assembly-CSharp/VendorDefinition.cs`

**Verified:** `Vendor.CanTradeItem(ItemDefinition)` is weaker. It only tests product-type compatibility; it does not apply the item tier gate or `not_buying` item exclusions. It is not sufficient by itself to answer “can I sell this item to this merchant?”.

## 3. Tier ownership and semantics

**Verified:** `ItemDefinition.product_tier` is the normal/global trading-tier gate. `ItemDefinition.MAX_PRODUCT_TIER == 3`.

When the vanilla trade UI checks whether the merchant buys the item now, `CanBuyItem(..., true)` compares `product_tier` against the vendor's current tier.

Evidence:
- `Assembly-CSharp/ItemDefinition.cs`
- `Assembly-CSharp/Vendor.cs`

**Important nuance:** effective per-vendor availability is not guaranteed to be described by `product_tier` alone because `VendorDefinition.not_buying` can disable a specific item at a specific vendor tier, or permanently when its modifier tier is below 1.

Therefore the eventual tooltip's “Tier II” should mean “lowest effective tier at which this vendor accepts this item”, not blindly echo `item.product_tier`, unless runtime data proves the two are equivalent for the relevant vendor set.

## 4. Dynamic vendor product types

**Verified:** `VendorDefinition.GetProductTypes()` returns the vendor's base product types plus `additional_types` whose `SmartExpression` evaluates true against `MainGame.me.player`.

Evidence:
- `Assembly-CSharp/VendorDefinition.cs`

Consequence: merchant eligibility can theoretically depend on current player/save state independently of trading tier.

**Runtime-open:** determine whether active vanilla/DLC vendors in 1.407 actually use `additional_types`, what those expressions depend on, and whether they can change during normal play. This decides whether a truly one-time buyer cache is sufficient or whether a small event-driven invalidation path is required.

## 5. Static index feasibility

**Verified in principle:** the loaded balance contains all `ItemDefinition` and `VendorDefinition` objects required to enumerate candidate item/vendor pairs, and the relevant IDs/tier/type/exclusion data are exposed in those objects.

A compact `item ID -> buyer entries` index is therefore feasible without maintaining a manual item/vendor table.

**Not yet accepted for production:** a pure static reimplementation of `CanBuyItem` would duplicate game logic and would not observe arbitrary Harmony patches that change `Vendor.CanBuyItem` or `Vendor.CanTradeItem`. Runtime evidence is required before choosing the exact evaluator.

## 6. Real Vendor instances and side-effect risk

**Verified:** `WorldGameObject.vendor` lazily creates a `Vendor` when first accessed, using a `VendorDefinition` whose ID equals the WGO's `obj_id`.

Evidence:
- `Assembly-CSharp/WorldGameObject.cs`

**Verified:** constructing a `Vendor` is not a read-only operation. Its constructor writes vendor data, resets `levelup_bar_1` / `levelup_bar_2`, and, when the vendor was not initialized, sets starting tier/money, fills inventory, and sets `vendor_inited`.

Evidence:
- `Assembly-CSharp/Vendor.cs`

Therefore production must **not** discover merchants by blindly calling `wgo.vendor` or forcing `WorldMap.FillVendorsList()` merely for tooltip data until runtime evidence proves this is harmless at the chosen lifecycle point.

## 7. Runtime world vendor discovery without constructing vendors

**Verified:** `WorldMap.RescanWGOsList()` enumerates all `WorldGameObject` children of the current world with `GetComponentsInChildren<WorldGameObject>(true)`, including inactive objects, and stores them in `WorldMap.objs`.

Evidence:
- `Assembly-CSharp/WorldMap.cs`

Because `WorldGameObject.vendor` resolves its `VendorDefinition` by the WGO's `obj_id`, a vendor definition can be matched to an existing world object by ID **without accessing the lazy vendor property**.

This is a promising side-effect-free way to distinguish balance definitions from objects present in the loaded world, but DLC/reachability semantics remain runtime-open.

## 8. Native tier projection

**Verified:** vanilla `Vendor` itself temporarily increments `cur_tier`, runs normal trade/inventory calculations for the projected tier, then restores the previous tier when calculating next-tier goods/costs.

Evidence:
- `Vendor.GetMoneyNeededForVendorLevelUp()`
- `Vendor.GetTotalGoodsOnNextTier()`
- `Assembly-CSharp/Vendor.cs`

This establishes temporary tier projection as a native game technique.

**Not yet accepted for production:** using this technique from the mod on live vendors must first prove:
- the vendor instance already exists naturally;
- no save/runtime values change after restoration;
- installed Harmony patches behave correctly under the projected tier;
- no vendor is artificially initialized just for the query.

## 9. Harmony-mod compatibility

**Verified:** real Graveyard Keeper mods patch these methods.

Current open-source example:
- `p1xel8ted/Graveyard-Keeper-Mods/src/AppleTreesEnhanced/Patches.cs` patches both `Vendor.CanBuyItem(ItemDefinition,bool)` and `Vendor.CanTradeItem(ItemDefinition)` and can force otherwise-unsellable bee-related items to be accepted.

Historical examples in `qaweofghasdlhtge/GYK-Mods-QMod` do the same.

Consequence:
- calling the actual patched `Vendor.CanBuyItem` can compose with arbitrary Harmony behavior;
- reading only `VendorDefinition` cannot generically reproduce arbitrary code patches;
- inspecting Harmony patch metadata can tell us that a method was patched, but cannot infer the patch's business semantics.

So “full compatibility with any mod that patches CanBuyItem/CanTradeItem” and “never obtain/use real Vendor instances” may be mutually incompatible requirements. The research harness must determine whether real naturally-created vendor instances are available safely enough to bridge this gap. Otherwise production should prefer the side-effect-free native-data architecture and document code-patch compatibility as a limitation rather than inventing vendor instances.

## 10. Localized merchant display names

**Verified:** vanilla `VendorGUI.Open` sets the merchant panel title with:

`GJL.L(vendor_obj.obj_id)`

Evidence:
- `Assembly-CSharp/VendorGUI.cs`

Therefore the native localization source for the merchant display name is the vendor object's/vendor definition's ID passed to `GJL.L`. Production should not maintain its own Krezvold/Tress/Cory name table.

**Runtime-open:** enumerate all active vanilla/DLC vendor IDs and confirm each returns a non-empty/sane localized display string in the current language.

## 11. Tooltip seam

**Verified:** `ItemDefinition.GetTooltipData(Item item = null, bool full_detail = true)` returns the standard `List<BubbleWidgetData>`.

**Verified:** `BaseItemCellGUI` passes `ItemDefinition.GetTooltipData(item, true)` directly to the standard tooltip for ordinary item cells. `TechUnlock` also uses this method for item tooltip data.

Evidence:
- `Assembly-CSharp/ItemDefinition.cs`
- `Assembly-CSharp/BaseItemCellGUI.cs`
- `Assembly-CSharp/TechUnlock.cs`

**Verified ecosystem precedent:** DecompDelight uses a Harmony postfix on exactly `ItemDefinition.GetTooltipData` to append standard tooltip data.

Evidence:
- `p1xel8ted/Graveyard-Keeper-Mods/src/DecompDelight/Patches.cs`

Conclusion: this is the preferred production UI seam. No separate UI and no per-frame tooltip patch are indicated.

## 12. Quality items

**Verified:** quality variants are represented as actual item definitions/IDs. `ItemDefinition.GetNameWithoutQualitySuffix()` strips the suffix after the final `:`, and `GameBalance` builds a base-name cache grouping those variants.

Evidence:
- `Assembly-CSharp/ItemDefinition.cs`
- `Assembly-CSharp/GameBalance.cs`

Native vendor checks receive the exact `ItemDefinition`, and `not_buying` compares exact item IDs. The buyer index should therefore be keyed by the exact item definition ID. It must not collapse quality variants to a base item unless later evidence explicitly supports doing so.

## 13. “Known/met merchant” technical path

**Verified:** the save stores `MainGame.me.save.known_npcs` as a `KnownNPCList`.

**Verified:** `Flow_Talk` calls `known_npcs.GetOrCreateNPC(obj_wgo.obj_id)` when talking to an NPC.

**Verified:** `KnownNPCList.GetOrCreateNPC` resolves `ObjectDefinition.npc_alias` before storing the known NPC. `GetNPC` itself simply looks up the stored ID.

Evidence:
- `Assembly-CSharp/KnownNPCList.cs`
- `Assembly-CSharp/FlowCanvas/Nodes/Flow_Talk.cs`
- `Assembly-CSharp/ObjectDefinition.cs`

Therefore “only merchants the player has met” is technically feasible without maintaining a custom discovery database. The likely lookup identity is `obj_def.npc_alias` when present, otherwise the vendor/object ID.

**Runtime-open:** verify that every relevant special/DLC trader maps cleanly to `KnownNPCList`. Some trade objects may not behave like ordinary NPCs.

## 14. Load lifecycle and candidate cache timing

**Verified load order:**
- `MainGame.GeneralInit()` calls `GameBalance.LoadGameBalance()` early.
- Save/world startup later calls `PrepareAfterLoad`, activates the world, rescans chunks, initializes WGOs, spawns the player, initializes quests, rescans `WorldMap`, restores save state, and initializes DLC systems.
- Near the end of startup it calls `MainGame.OnGameStartedPlaying()`; after that it starts flow behaviours and calls `GameSave.LateSaveFixer()` and `GameSave.GlobalEventsCheck()`.

Evidence:
- `Assembly-CSharp/MainGame.cs`
- `Assembly-CSharp/SaveSlotsMenuGUI.cs`

Open-source mods also commonly mutate balance data in postfixes of `GameBalance.LoadGameBalance`. `GameBalanceDumper` intentionally uses `Priority.First` to capture pristine data before other balance-modifying postfixes, proving patch order matters.

Evidence:
- `p1xel8ted/Graveyard-Keeper-Mods/src/GameBalanceDumper/Patches.cs`

Conclusion: building the final buyer index directly in an ordinary `LoadGameBalance` postfix is premature because player/save/world vendor state is not ready and mod ordering can matter.

Candidate one-time lifecycle seams for runtime verification:
1. a postfix on `MainGame.OnGameStartedPlaying()`, possibly with a single deferred callback if final save-fix/global-event state matters; or
2. a late postfix on the one-per-load `GameSave.GlobalEventsCheck()`.

Do not select one for production until the research harness proves the required data are complete and stable there.

## 15. Potential buyer vs current buyer

These are different questions.

**Current buyer:** vanilla answers this with `Vendor.CanBuyItem(item, true)`, which depends on the merchant's current tier and may depend on player-state-driven additional product types.

**Potential buyer:** for this mod, the useful interpretation is “there exists a legitimate merchant tier/state in which this vendor accepts the item”. Vanilla does not expose a single pure, side-effect-free method with that exact semantic.

The mod therefore needs a verified derivation strategy for potential buyer + effective required tier. Static source proves the ingredients exist, but runtime data must establish which edge cases actually occur before the display contract is frozen.

## 16. DLC and special vendors

Static source proves DLC-specific merchant/inventory logic exists (for example Refugees content references DLC-specific vendor-related IDs), but the decompile does not contain the serialized `game_data` balance rows needed to enumerate every 1.407 vendor definition and its live DLC gating.

Status: **runtime-open**.

Production must not infer DLC membership from ID prefixes or maintain a hardcoded DLC vendor list unless runtime evidence proves no native alternative exists.

## 17. Prices

Out of scope for the first release. No price field, estimate, current sale value, or simulated price calculation should be added while implementing the buyer/tier feature.

## Proposed architecture, pending runtime verification

Preferred path remains:

`loaded native balance/world data -> one buyer index -> ItemDefinition.GetTooltipData() postfix`

More concretely:

1. At a verified one-per-save-load lifecycle point, enumerate `GameBalance.me.items_data` and the relevant vendor definitions.
2. Match vendor definitions to current-world WGOs by ID without forcing `wgo.vendor`.
3. Resolve native localized merchant names with `GJL.L(vendorId)`.
4. Build a compact dictionary keyed by exact item definition ID. Each entry contains merchant ID, localized-name key/result as appropriate, and effective required tier.
5. The tooltip postfix performs only a dictionary lookup, optional cheap “known merchant” filtering, formatting, and appending standard `BubbleWidgetData`.
6. No per-frame work, polling, recurring scans, heavy reflection per tooltip, artificial NPC/vendor creation, or sale-price simulation.

If runtime evidence proves that all relevant vendor instances already exist naturally and tier projection is side-effect-free, the index evaluator may call the real patched `Vendor.CanBuyItem` to maximize Harmony compatibility. If not, use the safest native-data derivation and document the narrower compatibility boundary.

## Required research-only runtime probe

Static research is insufficient to freeze production. The next executable should be a diagnostic harness, not the production mod.

It should run only after the normal save/world startup and emit one structured report containing:

### Catalog snapshot
For every `GameBalance.me.vendors_data` entry:
- vendor ID;
- `GJL.L(id)` result;
- matching `WorldMap.objs` count;
- object type, `npc_alias`, and whether the NPC is in `known_npcs`;
- `start_tire`;
- base product types;
- `additional_types` count and enough expression information to classify whether they are state-dependent;
- all `not_buying` entries;
- relevant DLC availability state when it can be obtained natively.

### Live-instance safety snapshot
Without invoking `wgo.vendor`:
- inspect whether the WGO already has a naturally-created backing `Vendor`;
- log `vendor_inited`, saved/current tier parameters, and level-up parameters before any experiment;
- count how many active vendor WGOs are naturally instantiated at the candidate lifecycle seam.

Do **not** instantiate missing vendors.

### Native parity test
For naturally-existing Vendor instances only:
- compare the proposed data-derived result to the actual patched `CanBuyItem(item, true)` at the real current tier;
- for a small representative set, project tiers 1..3 using the vanilla set/restore pattern, call the real method, restore immediately, and prove all observed vendor/save parameters are byte/value-identical afterward;
- include ordinary items, quality variants, tools/special items, and DLC items/vendors.

### Harmony inventory
Use Harmony patch metadata to record patch owners on:
- `Vendor.CanBuyItem(ItemDefinition,bool)`;
- `Vendor.CanTradeItem(ItemDefinition)`.

This establishes whether the installed mod set contains code-level trade-eligibility overrides. Do not attempt to interpret arbitrary patch semantics from metadata alone.

### Data-shape/anomaly report
Enumerate:
- item `product_tier` values outside 1..3 among potentially tradeable items;
- vendor-specific tier gaps caused by `not_buying`;
- state-dependent `additional_types`;
- vendor definitions with no WGO;
- vendor WGOs with no definition;
- missing/placeholder localizations;
- special/DLC vendors that do not map cleanly to `known_npcs`;
- exact quality-item behavior.

### Lifecycle check
Run the same small counts at the candidate one-time lifecycle seams and identify the first seam where:
- GameBalance is final enough to include other mods' balance edits;
- player/save data exist;
- WorldMap has been rescanned;
- DLC systems relevant to vendor presence are initialized;
- no later startup step changes the catalog inputs.

The harness must not write persistent gameplay state and must not be merged into production by default.

## Product decisions fixed 2026-09-21

- **Display only merchants already met/known by the current save.** Use the game's own `KnownNPCList`/NPC alias identity rather than a custom discovery database.
- **Do not design first-release architecture around arbitrary third-party Harmony patches to `Vendor.CanBuyItem` / `Vendor.CanTradeItem`.** Native 1.407 game data/semantics are the compatibility target. Ordinary coexistence remains desirable, but generic semantic composition with trade-overhauling mods is explicitly out of scope.

### Lifecycle lesson carried from Day Wheel Quest Markers

A fresh Graveyard Keeper save may legitimately have no relevant known NPCs yet. In Day Wheel Quest Markers, treating missing weekday NPCs as "cache not ready" caused repeated/heavy initialization and later a measured 302.22 ms first-NPC structural rebuild.

For Who Buys This?:
- an empty `known_npcs` / zero known merchants is a valid **ready empty filter result**, not an initialization failure;
- structural vendor/item discovery must not depend on the first known merchant appearing;
- meeting a new merchant may only require cheap known-state rebinding/filtering, never rebuilding the structural buyer matrix;
- no retry loop or recurring heavy scan is permitted merely because zero merchants are known.

## Current architecture status

- Standard tooltip seam: **verified**.
- Native current-sale method: **verified**.
- Balance source for vendor/item definitions: **verified**.
- Native merchant localization key: **verified**.
- Exact-item/quality strategy: **verified**.
- Side-effect risk of forcing Vendor construction: **verified; avoid**.
- One-time item -> buyers/tier index: **feasible, evaluator not yet frozen**.
- Generic compatibility with arbitrary CanBuyItem/CanTradeItem Harmony patches: **explicitly out of scope for first release**.
- Active DLC/special vendor set: **runtime-open**.
- Dynamic `additional_types` / cache invalidation need: **runtime-open**.
- Final cache-build lifecycle seam: **runtime-open**.
