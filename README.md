# Who Buys This?

A vanilla-friendly informational QoL mod for Graveyard Keeper 1.407.

## Stable release

Current stable version: **1.0.1**. The stable binary is published in the GitHub Release `v1.0.1`.

Who Buys This? extends the standard item tooltip with:
- the merchant(s) who buy the exact item variant;
- the trading Tier at which selling that item becomes available;
- only merchants already known/unlocked in the current save;
- a buyer heading localized for all 11 languages supported by the game.

Example: `Мельник II, Фермер I`.

The mod uses Graveyard Keeper's native trading data and standard tooltip UI. It does not maintain a manual item-to-vendor table.

## Scope

Who Buys This? is informational only. It does **not** change:
- prices or sale-value calculations;
- merchant stock;
- trading tiers;
- items;
- economy;
- progression.

Sale prices are intentionally not displayed.

## Runtime / architecture

Target: Graveyard Keeper 1.407 on PC with BepInEx/Harmony.

The stable architecture is:
`native GameBalance definitions -> one immutable exact-item buyer index -> cheap on-demand state filters -> ItemDefinition.GetTooltipData() postfix`.

There is no polling, per-frame scanning, artificial Vendor/NPC construction, or recurring catalog rebuild.

See `docs/VERIFIED_GAME_DATA.md` for the verified game-data semantics and architecture, and `docs/TEST_BUILD_LOG.md` for exact candidate/build identity and acceptance evidence.

## Installation

Copy `WhoBuysThis.dll` into your Graveyard Keeper `BepInEx/plugins` folder.
