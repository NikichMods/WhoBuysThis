# Project Working Contract

This repository follows the canonical global development rules in `NikichMods/DevRules`.

Read before substantive technical work:
- `ENGINEERING_RULES.md`
- `CI_POLICY.md`
- `GIT_WORKFLOW.md`
- `PROJECT_BOOTSTRAP.md`
- `RUNTIME_TEST_HARNESS.md` when installed-runtime evidence is relevant

This file contains only project-specific additions and constraints.

## Project identity

- Project name: Who Buys This?
- Repository: `NikichMods/WhoBuysThis`
- Target/runtime: Graveyard Keeper 1.407 on PC, BepInEx/Harmony
- Purpose: add native-style item-tooltip information showing which merchant(s) buy an item and the trading Tier at which that sale becomes available.

## Scope

First release is informational only. It must not change prices, economy, merchant stock, trading tiers, items, or progression, and it must not show current or predicted sale prices.

Do not maintain a manual item -> vendor table.

## Mandatory project-specific start-of-work checks

Before substantive implementation:
1. inspect current repository state and this file;
2. inspect `docs/VERIFIED_GAME_DATA.md`;
3. verify unfamiliar Graveyard Keeper internals from current game/decompiled evidence or established open-source mod code before production relies on them;
4. keep research probes separate from production.

## Architecture / runtime constraints

Frozen first-release architecture:
`native GameBalance definitions -> one immutable exact-item buyer index -> cheap on-demand state filters -> ItemDefinition.GetTooltipData() postfix`.

Build the structural index once from `MainGame.OnGameStartedPlaying()`.

Dynamic state must not cause structural rebuilds:
- ordinary known/met state: native `KnownNPCList` check at tooltip creation;
- conditional vendor product types: native `VendorDefinition.GetProductTypes()` check only for conditional candidates;
- staged Game of Crone vendor proxies: on-demand WGO presence check for staged-family candidates.

Staged `_1/_2/_3` Game of Crone vendor definitions represent one conceptual merchant family and must be merged rather than displayed as separate merchants. Detect the family from verified native definition shape; do not create item mappings.

Keep exact item IDs. Quality variants must not be collapsed.

Avoid:
- per-frame work;
- polling or recurring scans;
- repeated heavy reflection during tooltip creation;
- artificial NPC/vendor creation for queries;
- duplicated trade/pricing formulas;
- large hardcoded mappings;
- broad UI replacement when standard tooltip data can express the result.

Verified UI seam: `ItemDefinition.GetTooltipData()` with standard `BubbleWidgetData`.

## User-facing behavior requirements

- Vanilla-friendly informational QoL only.
- Use the standard tooltip.
- Do not expose current/predicted sale price.
- Show only merchants already known/unlocked by the current save.
- Ordinary merchants use the game's native known-NPC state.
- Special staged Game of Crone trade proxies use native proxy presence because the proxy objects are progression-spawned and are not themselves KnownNPC entries.
- Zero known merchants is a valid ready-empty state and must not trigger retry/rebuild behavior.

## Compatibility boundary

Native Graveyard Keeper 1.407 trading data/semantics are the target. Generic semantic composition with arbitrary third-party Harmony patches to `Vendor.CanBuyItem` / `Vendor.CanTradeItem` is not required for the first release.

Never force `WorldGameObject.vendor` or manufacture Vendor/NPC instances merely to answer tooltip queries.

## Git / version / acceptance workflow

- `main` is the stable/documentation baseline.
- Research-only work uses `research/<topic>`.
- Build-bearing production work uses `dev/<version>` or a semantic feature branch.
- Research-only work does not consume numbered release/test versions.
- Runtime behavior reaches `main` only after exact candidate build/runtime evidence and explicit user acceptance.
- Numbered handed artifacts are immutable and retain exact source identity per DevRules.
- Stable distribution uses GitHub Releases.

## Runtime test harness specifics

Research harnesses are setup/observation tools only and are never merged into production automatically. The completed vendor-matrix harness source is frozen at `97375f2d352c5ac1ca22bb186b4249cae16e4141`.

## CI / build specifics

This is a public repository. Standard GitHub-hosted runner minutes are not treated as scarce. Use hosted CI whenever compilation, test feedback, or a reproducible user artifact is useful, including research/development candidates.

For this Windows-targeted BepInEx mod, `windows-latest` is an acceptable default when it simplifies or better matches the build. Preserve exact source/artifact identity for handed builds.

## Long-lived sources of truth

- `AGENTS.md`
- `docs/VERIFIED_GAME_DATA.md`
- `README.md`
- later, `docs/TEST_BUILD_LOG.md` for numbered production/test binaries

## Shared Graveyard Keeper research

Cross-project Graveyard Keeper 1.407 host/runtime research is centralized in `NikichMods/GraveyardKeeperResearch`.

Before starting a fresh investigation into vanilla/game-engine/UI/NGUI/data/lifecycle behavior:

1. read this repository's own canonical verified-data / architecture docs first;
2. consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and the linked shared knowledge documents;
3. search accepted local/shared test evidence and relevant history if the result has not yet been promoted;
4. perform new static/runtime research or a probe only if the question remains open.

Project-specific mechanics, product/UX decisions, release state, and build acceptance remain canonical in this repository. Reusable host/runtime facts that can serve multiple Graveyard Keeper mods should be promoted back into the shared research repository after acceptance rather than left only in chat, commit history, or a test log.

