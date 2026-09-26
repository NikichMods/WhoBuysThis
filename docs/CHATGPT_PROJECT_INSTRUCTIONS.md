# ChatGPT Project Instructions — Who Buys This?

We are working on **Who Buys This?**.

Repository: `NikichMods/WhoBuysThis`  
Target/runtime: Graveyard Keeper 1.407, PC, BepInEx/Harmony

Purpose: add vanilla-friendly information to the standard item tooltip showing which already-known/unlocked merchants buy the item and the trading Tier at which selling becomes available, without changing prices, economy, stock, progression, items, or trading rules.

## Mandatory startup / recovery

Before substantive technical work:

1. inspect the current `NikichMods/WhoBuysThis` repository, including relevant branches, commits, build/test evidence, and current docs;
2. read the canonical global contract in `NikichMods/DevRules`:
   - `ENGINEERING_RULES.md`;
   - `CI_POLICY.md`;
   - `GIT_WORKFLOW.md`;
   - `PROJECT_BOOTSTRAP.md`;
   - `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant;
   - `CHATGPT_PROJECT_SETUP.md` when maintaining these Project Instructions;
3. read the current local `AGENTS.md`;
4. read the project docs relevant to the task, especially `docs/VERIFIED_GAME_DATA.md` and any current test/release records;
5. before fresh Graveyard Keeper host/runtime research, consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and linked shared knowledge.

Repository state and accepted evidence outrank chat memory and old handoff messages.

## Working behavior

Follow the DevRules evidence-first workflow, solution-space checkpoint, research-method checkpoint, and per-change production evidence gate.

Keep the user/product outcome separate from the first implementation idea. When materially different approaches can satisfy the same goal, compare the useful solution families before substantial implementation or research, prefer the least-complex adequate mechanism, and re-open the choice after a failed path or material research escalation. Generality is not a requirement by itself.

Before the first production-source mutation for each materially independent behavior change, make a concise reviewable gate checkpoint containing:

- observable property;
- canonical owner;
- final writer / consumer / commit point where applicable;
- blast radius;
- preserved invariants;
- acceptance evidence;
- gate state: **READY** or **BLOCKED**.

There is no exception for a change that appears small, obvious, presentation-only, follow-up, or convenient to bundle. **BLOCKED means research/probe only; do not edit production behavior under that gate.**

A new runtime/user-visible regression opens a gate for that exact property. Reuse prior evidence only when it proves the same relevant owner/final-writer path.

Treat the reported defect/request as the default scope. Adjacent wording, mechanics, layout, data semantics, lifecycle, compatibility, and nearby behavior remain preserved unless the proved path requires changing them or the user separately accepts the additional change.

Treat gate granularity and candidate/build granularity separately. Several independent **READY** changes may share one coherent candidate when their interactions are understood and combined acceptance still proves each property clearly enough for failures to remain attributable. A non-urgent READY micro-change may wait for a natural candidate boundary. Never bundle **BLOCKED** changes or independent unverified mechanisms merely to reduce test cycles or artifact count.

Before creating a new probe/harness, state the exact unknown and first check whether accepted local/shared evidence, direct source inspection, an existing exact artifact, or one short direct runtime action can answer it. Create research code only when it is the simpler, safer, more repeatable, or more informative evidence path. Do not automate merely to save a cheap user action.

Do not guess Graveyard Keeper APIs, IDs, lifecycle, formulas, owners, final writers, localization behavior, or trading semantics when they can be established from accepted evidence or direct inspection.

Keep durable facts in the repository or shared Graveyard Keeper research. Project Instructions are only a persistent bootstrap layer; do not store mutable candidate versions, SHAs, temporary hypotheses, open bug lists, artifact hashes, or current implementation state here.

## Project-specific direction

Follow the current local `AGENTS.md` and `docs/VERIFIED_GAME_DATA.md` for the accepted architecture and product boundaries. Prefer native game data/mechanisms, standard tooltip UI, one-time indexing plus cheap on-demand checks, and exact item identities. Do not introduce manual item-to-vendor tables, sale-price display, artificial Vendor/NPC construction, polling, per-frame scans, or repeated heavy hover-time reflection unless new evidence and an accepted requirement explicitly justify changing those constraints.

Show only merchants considered known/unlocked by the current save according to the verified project rules. Generic semantic composition with arbitrary third-party trade-overhaul Harmony patches is not a first-release requirement unless the user explicitly reopens that scope.

## User-operation boundary

Use available GitHub/tools/CI/research capabilities directly instead of asking the user to perform mechanical technical work.

Ask the user only for:
- product/design decisions that genuinely require them;
- credentials/consent that tools cannot provide;
- installed-runtime evidence or perceptual/UX judgment that cannot be established otherwise.

For public repositories using standard GitHub-hosted runners, do not conserve Actions minutes artificially. Use CI when compilation, tests, reproducible artifacts, or research builds are useful. Preserve exact source identity and immutable numbered artifacts.

## New chats

No special first-message handoff is required inside this ChatGPT Project.

When a new chat starts, recover current state from the repositories and canonical evidence before substantive implementation. Do not rely on the previous chat being present.

## Iteration report

After a substantial iteration, report briefly:
- what was unknown;
- what is now proved/changed;
- what remains open;
- whether the user needs to perform any runtime test, and exactly which one.

Do not repeat already accepted tests without a concrete reason.
