# Test Build Log

## 0.1.0 — candidate

- Status: handed candidate / awaiting runtime acceptance
- Purpose: first production implementation of buyer + tier information in the standard item tooltip
- Source commit: `6ef8220be3bdab0189587fd6bfca24afbea1a8de`
- Frozen source ref: `candidate/0.1.0`
- Development branch at build time: `feature/tooltip-buyers`
- GitHub Actions run: `35614753683`
- Artifact ID: `10645477011`
- Artifact name: `WhoBuysThis-0.1.0-6ef8220be3bdab0189587fd6bfca24afbea1a8de`
- Artifact ZIP digest: `sha256:95ed5970f93ecf92cf1e8b8e01d5448f91e2adb87f8ab969b38f3ca6730af398`
- DLL: `WhoBuysThis.dll`
- DLL SHA-256: `5f8ee9345bd70621d458d878e65e797b7176acd08a7c2ed4d146860804e815cc`
- Build: Windows GitHub-hosted runner, Release/net48, successful
- Production source does not embed or redistribute Graveyard Keeper assemblies; game integration is bound once at runtime through cached reflection/Harmony metadata.

### Intended behavior

- Build one structural exact-item buyer index after `MainGame.OnGameStartedPlaying()`.
- Append buyer information through the standard `ItemDefinition.GetTooltipData()` result.
- Show ordinary merchants only when present in native `KnownNPCList`.
- Re-evaluate conditional product types only for conditional candidates (currently Tress paints).
- Treat staged Game of Crone vendor proxies as one conceptual merchant and use active proxy presence as their unlock/progression signal.
- Do not instantiate Vendor/NPC objects, poll, run per-frame work, modify save/economy, or calculate prices.

### Runtime acceptance checks

1. Plugin loads with no Harmony/binding/index-build error.
2. Known ordinary merchant item shows buyer and Roman-numeral tier in the standard tooltip.
3. Multi-buyer item lists all currently known buyers without duplicates.
4. Tier-II/III items show the correct required tier.
5. Unknown ordinary merchants are not leaked.
6. Current Game of Crone staged vendor, when applicable, is shown once rather than as separate `_1/_2/_3` definitions.
7. Items with no visible known/unlocked buyer get no added tooltip section.
8. No gameplay/economy/save behavior changes and no noticeable tooltip hitching.

### Result

Awaiting user runtime test.
