# Verified Game Data

Target: Graveyard Keeper 1.407 (PC).

This is the durable evidence ledger for production decisions in Who Buys This?. Static facts come from the 1.407 decompile/open-source precedent; runtime facts come from the research harness executed on 2026-09-21.

## Evidence baseline

Primary static evidence:
- `Kupie/GYK_DECOMP`, ref `6abf79199d92482af1c7573870dd9a20ec2270b9` (`LazyConsts.VERSION == 1.407f`).
- `p1xel8ted/Graveyard-Keeper-Mods`, ref `ebac55b3fe58402ae7cd5c061d9eb5b0c8e610eb`.
- historical supporting example: `qaweofghasdlhtge/GYK-Mods-QMod`, ref `c721a986e03264e52307543ebae132e976a70d6d`.

Runtime research artifact:
- research source: `97375f2d352c5ac1ca22bb186b4249cae16e4141` (`research/vendor-matrix`);
- GitHub Actions run: `35602817998`; artifact: `10639463705`;
- harness version: `0.0.0-research`;
- target runtime reported Graveyard Keeper 1.407, BepInEx 5.4.23.5, Windows x64, Russian localization;
- harness explicitly reported `save_mutation=none` and `vendor_instantiation=none`.

Runtime snapshot:
- world objects: 3545;
- known NPCs: 81;
- vendor definitions: 31;
- known vendors by ordinary known-NPC identity: 20;
- item definitions: 1157;
- items with at least one data-derived potential buyer: 234;
- items with at least one ordinary known buyer in this save: 181;
- vendors using `additional_types`: 1;
- product-type resolution failures: 0;
- vendor definitions with no current WGO: 9;
- localization anomalies: 1;
- potentially tradeable items outside product tier 1..3: 0;
- quality-like items: 543;
- quality groups with buyer differences: 37;
- naturally-existing live Vendor instances available to the harness: 0, therefore native parity checks: 0.

## 1. Authoritative vendor/item source

`GameBalance` owns the loaded `items_data` and `vendors_data` balance collections and builds ID caches during `GameBalance.LoadGameBalance()`.

Production must derive the buyer index from those loaded native definitions. Do not maintain a manual item -> vendor table.

The runtime catalog contained 31 vendor definitions. Most ordinary merchants had one matching WGO. Nine definitions had no current WGO: the internal `body_spawn` test-like definition plus inactive staged Game of Crone vendor definitions.

## 2. Native sale rule

The actual player -> merchant sale filter is:

`Trading.BuyableItemsFilter -> Vendor.CanBuyItem(itemDefinition, true)`.

`Vendor.CanBuyItem` applies:
1. item/product-type validity;
2. product-tier <= current vendor tier;
3. intersection with `VendorDefinition.GetProductTypes()`;
4. vendor-specific `not_buying` exclusions.

`Vendor.CanTradeItem` is weaker and is not sufficient by itself.

Production will preserve these native data semantics without constructing Vendor instances.

## 3. Tier semantics

`ItemDefinition.product_tier` is the normal 1..3 trading-tier gate.

Runtime evidence:
- no potentially tradeable item had a tier outside 1..3;
- every serialized `not_buying` modifier in the loaded 1.407 balance used tier `0` (140 entries), meaning permanent exclusion rather than a tier-specific gap;
- for every data-derived buyer-matrix row, computed effective tier equaled `ItemDefinition.product_tier`.

Production should still compute the minimum accepted tier generically from tiers 1..3 so the code follows the native rule rather than depending on the observed simplification.

For staged Game of Crone proxy vendors, the staged definition's `start_tier` is also a lower bound on when that proxy definition exists. Conceptual required tier is therefore the minimum valid tier across the staged family, respecting both item tier and member-stage availability.

## 4. Dynamic product types

`VendorDefinition.GetProductTypes()` returns base product types plus conditionally-enabled `additional_types` evaluated against the current player.

Runtime evidence found exactly one dynamic vendor:

`npc_carpenter` (Tress): `paints => Ppar("tress_paints_unlocked")>0`.

In the tested save the condition was false, so `paints` was not in the active product-type list.

This means a cache containing only currently-active types would become stale later. Production must instead:
- index the union of base + possible additional product-type keys once;
- mark entries whose match depends on a conditional type;
- at tooltip creation, call native `GetProductTypes()` only for those conditional entries and suppress the buyer while the condition is false.

This gives immediate correctness after the quest parameter changes with no polling or structural cache rebuild.

## 5. Vendor construction is not a read operation

`WorldGameObject.vendor` lazily creates a `Vendor`. The Vendor constructor can initialize money/tier/inventory/save-backed state.

Production must not force `wgo.vendor`, call `WorldMap.FillVendorsList()` merely for discovery, or manufacture Vendor/NPC instances for queries.

The research harness confirmed it could produce the full catalog/matrix without vendor construction or save mutation.

## 6. World-object presence and special staged vendors

`WorldMap.RescanWGOsList()` populates the current world-object list before gameplay begins. Vendor definitions can be matched to WGOs by `obj_id` without accessing the lazy vendor property.

Runtime evidence showed the Game of Crone cook as three staged definitions:
- `vendor_refugee_cook_1` start tier 1, no current WGO;
- `vendor_refugee_cook_2` start tier 2, one current WGO;
- `vendor_refugee_cook_3` start tier 3, no current WGO.

The same data shape exists for undertaker and tanner families. Supporting story-graph evidence in `ZlordHUN/GYK-Back-From-The-Grave` shows these vendor proxies are spawned/replaced as their NPC stories advance and explicitly pairs:
- `vendor_refugee_cook` with `npc_refugee_cook`;
- `vendor_refugee_undertaker` with `npc_refugee_coffin_maker`;
- `vendor_refugee_tanner` with `npc_refugee_tanner`.

Production should not treat the three staged definitions as three merchants.

Accepted rule:
- detect staged families generically from the native definition shape (same base ID after `_1/_2/_3`, matching stage/start-tier pattern and shared localized merchant name);
- merge a staged family into one conceptual buyer;
- keep all stages in the structural index so future upgrades do not require rebuilding;
- consider the staged merchant visible only while at least one member proxy WGO currently exists. Proxy existence is the native progression/unlock signal for these special merchants.

Only staged-family candidates need a live WGO existence check at tooltip time. This is intentionally on-demand and rare; do not add polling or spawn/destroy bookkeeping merely to avoid it.

`WorldMap.GetWorldGameObjectByObjId` is a linear scan of `WorldMap._objs`, so do not use it for every ordinary buyer. Restrict it to staged special-vendor visibility checks.

## 7. Known/met merchant filter

The save stores `MainGame.me.save.known_npcs` as `KnownNPCList`. `Flow_Talk` records talked-to NPCs, and `GetOrCreateNPC` resolves `ObjectDefinition.npc_alias` before storage.

For ordinary merchants, production should use the native known-NPC identity at tooltip time. Do not cache the known set structurally and do not rebuild the buyer matrix when a merchant is met.

Runtime evidence confirms this works for ordinary merchants, including a useful negative control: `npc_hunchback` had a current WGO but was not known and was correctly marked unknown.

Special staged Game of Crone vendor proxies do not map directly to `known_npcs` (the active `vendor_refugee_cook_2` proxy was not itself a known NPC while `npc_refugee_cook` was known). For these proxies use the stage-family WGO-presence rule from section 6 rather than inventing a custom discovery database.

Zero known merchants is a valid ready-empty filter result. It must never trigger retry loops or structural rebuilds.

## 8. Localization

Vanilla `VendorGUI.Open` uses `GJL.L(vendor_obj.obj_id)` for merchant display text.

The runtime catalog resolved sane Russian names for every relevant ordinary and staged merchant definition. The one localization anomaly was `body_spawn -> body_spawn`; it had no WGO and is an internal test-like vendor definition, not a player-facing merchant.

Production should store localization keys/IDs in the index and call `GJL.L` when formatting a visible tooltip so a language change does not require rebuilding the index.

## 9. Quality items

Quality variants are exact item definitions/IDs and native vendor exclusions compare exact IDs.

Runtime evidence found 543 quality-like items and 37 base groups whose buyer/tier signatures differed across variants. Examples include wine, hops, seeds, fish, meals, books and tools.

Production index key: exact item definition ID. Never collapse quality variants to a base ID.

## 10. Tooltip seam

`ItemDefinition.GetTooltipData(Item item = null, bool full_detail = true)` returns the standard `List<BubbleWidgetData>` used by ordinary item cells.

DecompDelight independently demonstrates a Harmony postfix on this exact method.

Accepted production UI seam: Harmony postfix on `ItemDefinition.GetTooltipData()` that appends standard BubbleWidgetData. No separate UI and no per-frame tooltip patch.

## 11. Lifecycle

Static load order shows GameBalance loading early, then save/world restore, WGO rescan, player/quest/DLC setup, and finally `MainGame.OnGameStartedPlaying()`.

In the runtime log, `Rescan WGOs list` occurred before `OnGameStartedPlaying`; the harness then reached a stable ready state and emitted the full 31-vendor/1157-item snapshot without later catalog changes during the observed session.

Accepted production initialization seam: one build of the immutable structural buyer index from a postfix on `MainGame.OnGameStartedPlaying()`.

No delayed retry loop is required. If the required GameBalance/save objects are unexpectedly missing at that verified seam, fail closed for that load and log once rather than polling.

Dynamic state is deliberately excluded from the immutable index:
- known/unknown merchant state is checked on tooltip creation;
- conditional additional product types are checked on tooltip creation only for conditional candidates;
- staged Game of Crone merchant presence is checked on tooltip creation only for staged-family candidates.

Therefore meeting merchants, unlocking Tress paints, and upgrading a refugee vendor do not require structural invalidation.

## 12. Compatibility boundary

Arbitrary third-party Harmony patches to `Vendor.CanBuyItem` / `Vendor.CanTradeItem` are explicitly out of scope for the first release by product decision.

The runtime harness found zero naturally-created Vendor instances at the research seam, so a parity call into live `CanBuyItem` could not be performed without violating the no-instantiation rule. This is not a blocker under the chosen compatibility boundary.

Target semantics are Graveyard Keeper 1.407 native balance/runtime data after normal balance-loading modifications. Ordinary coexistence with other mods remains desirable; semantic composition with trade-overhaul patches is not promised.

## 13. Prices

Sale prices remain explicitly out of scope. Do not calculate, simulate, cache, or display current/predicted sale value in the first release.

## Accepted production architecture

`native loaded definitions -> one immutable exact-item buyer index -> cheap on-demand state filters -> ItemDefinition.GetTooltipData postfix`

Build once on `MainGame.OnGameStartedPlaying()`:
1. enumerate item definitions and vendor definitions;
2. classify ordinary vendor definitions and staged proxy families;
3. ignore definitions that are neither current ordinary merchants nor valid staged families (for example `body_spawn`);
4. for each vendor/member, use the union of base and possible conditional product types to discover potential item matches;
5. apply exact-ID `not_buying` semantics and compute minimum effective tier from native 1..3 rules;
6. merge staged family members into one conceptual buyer and keep the minimum valid required tier;
7. store IDs/definition references, required tier, and whether current `GetProductTypes()` recheck is required; do not store localized strings or known-state booleans.

Tooltip path:
1. exact item-ID dictionary lookup;
2. for ordinary buyer entries, native `KnownNPCList.GetNPC` check;
3. for conditional entries, native `VendorDefinition.GetProductTypes()` check;
4. for staged proxy families, verify at least one member WGO currently exists;
5. resolve merchant display name with `GJL.L`;
6. append standard BubbleWidgetData;
7. if no visible buyers remain, append nothing.

Performance contract:
- no per-frame Update;
- no polling;
- no recurring catalog scans;
- no Vendor/NPC instantiation;
- no heavy reflection on hover;
- no manual item -> vendor mapping;
- no duplicated pricing/economy formula;
- no cache rebuild merely because known NPCs or story progression changed.

## Product decisions fixed 2026-09-21

- Show only merchants already known/unlocked for the current save. Ordinary merchants use native KnownNPCList; staged Game of Crone proxy merchants use their native spawned proxy presence because those trade objects are progression-created and are not themselves KnownNPC entries.
- Generic semantic support for arbitrary third-party trade-overhaul Harmony patches is not a first-release requirement.
- Prices are not part of the first release.

## Current architecture status

- Vendor/item source: **verified**.
- Native sale semantics: **verified**.
- Tier semantics/data shape: **verified**.
- Localization: **verified**.
- Standard tooltip seam: **verified**.
- Exact quality-item handling: **verified**.
- Dynamic additional product types: **verified; Tress paints handled on-demand**.
- DLC/special staged vendor shape: **verified; handled as conceptual proxy families**.
- Known merchant lifecycle: **verified for ordinary vendors; special proxies use native presence signal**.
- Cache-build lifecycle: **frozen at OnGameStartedPlaying**.
- Vendor construction: **forbidden/unused**.
- Per-frame/polling requirement: **none**.
- Production architecture: **frozen; implementation may begin**.
