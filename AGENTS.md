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

Do not maintain a manual item -> vendor table unless verified native game data is insufficient.

## Mandatory project-specific start-of-work checks

Before substantive implementation:
1. inspect current repository state and this file;
2. inspect `docs/VERIFIED_GAME_DATA.md`;
3. verify unfamiliar Graveyard Keeper internals from current game/decompiled evidence or established open-source mod code before production relies on them;
4. keep research probes separate from production.

## Project-specific evidence contract

Production architecture must not be fixed until evidence establishes:
- the complete vendor set and authoritative trading data;
- the native item-buy eligibility rule;
- Tier requirement ownership;
- localized merchant display-name source;
- tooltip construction seam;
- safe cache-build lifecycle and any invalidation need;
- behavior for DLC/special merchants/quality items;
- compatibility implications of other Harmony patches to vendor eligibility methods.

Record durable verified findings in `docs/VERIFIED_GAME_DATA.md` with evidence/source references. Unknowns stay explicitly unknown.

## Architecture / runtime constraints

Preferred direction, only if verified:
`GameBalance/Vendor native data -> one-time item ID -> buyers/tier index -> cheap tooltip lookup`.

Avoid without proven need:
- per-frame work;
- polling or recurring scans;
- repeated heavy reflection during tooltip creation;
- artificial NPC/vendor creation for queries;
- duplicated trade formulas or large hardcoded mappings;
- broad UI replacement when standard tooltip data can express the result.

Strong candidate seam: `ItemDefinition.GetTooltipData()`; treat it as unverified until current evidence confirms it.

## User-facing behavior requirements

- Vanilla-friendly informational QoL only.
- Use the standard tooltip when practical.
- Do not expose current/predicted sale price in initial scope.
- Product decision remains open: show all potential buyers vs only merchants already discovered/met by the player. Do not silently choose one before presenting the technical trade-offs to the user.

## Git / version / acceptance workflow

- `main` is the stable/documentation baseline.
- Research-only work may use `research/<topic>`; build-bearing runtime work should use a `dev/<version>` or semantic feature branch.
- Do not consume a numbered release/test version for research-only work.
- Runtime behavior reaches stable `main` only after the exact candidate has required build/runtime evidence and explicit user acceptance.
- Numbered handed artifacts are immutable and must retain exact source identity per DevRules.
- Stable distribution, once applicable, uses GitHub Releases.

## Runtime test harness specifics

If static inspection cannot settle native lifecycle/eligibility semantics, use a minimal research-only harness that:
- invokes real native methods/data paths;
- logs only the values needed to distinguish hypotheses;
- does not manufacture the result being tested;
- avoids save-persistent state when possible;
- is not merged into production by default.

## CI / build specifics

No hosted CI is required for research/documentation/bootstrap work. Add/run hosted build automation only when a concrete executable property must be proven, especially before a handed candidate.

## Long-lived sources of truth

- `AGENTS.md`
- `docs/VERIFIED_GAME_DATA.md`
- `README.md`
- later, `docs/TEST_BUILD_LOG.md` once numbered binaries are handed out
