# Test Build Log

## 1.0.0 — release candidate

- Status: build pending
- Purpose: first public stable release candidate
- Parent accepted behavior: 0.1.1
- Change from 0.1.1: version metadata only; production logic and UI code are unchanged
- Development branch: `dev/1.0.0`

### Required smoke test

1. Confirm BepInEx reports `Who Buys This? 1.0.0`.
2. Load the same save and open one item tooltip that has buyer information.
3. Confirm the buyer line still renders normally.
4. Return the fresh `LogOutput.log` only if an error appears; otherwise a screenshot or explicit confirmation is sufficient.

### Result

Awaiting build and smoke test.

---

## 0.1.1 — accepted candidate / superseded by 1.0.0 version-only release candidate

- Status: runtime accepted
- Purpose: presentation-only follow-up to 0.1.0
- Source commit: `5d56e50423a6574b59725ed79229eca69ab2e39f`
- Frozen source ref: `candidate/0.1.1`
- Development branch at build time: `feature/tooltip-buyers`
- GitHub Actions run: `35626138883`
- Artifact ID: `10651932654`
- Artifact name: `WhoBuysThis-0.1.1-5d56e50423a6574b59725ed79229eca69ab2e39f`
- Artifact ZIP digest: `sha256:3e837a4d866cec53e838d1d5836ff032aa0bab3f2b2ccc16f94795ed27d94c72`
- DLL: `WhoBuysThis.dll`
- DLL SHA-256: `9485e019e50240c1ffd8ec6c39c0b8dca587bc6a6e98ece840b0682aaf92ba96`
- Build: Windows GitHub-hosted runner, Release/net48, successful

### Change

- Removed the em dash between merchant name and Tier.
- Merchant/Tier pairs render as e.g. `Мельник II, Фермер I`.
- A Unicode non-breaking space (U+00A0) joins merchant name and Roman-numeral Tier so wrapping moves the pair together.
- Trading/index/filter behavior is unchanged from 0.1.0.

### Runtime evidence

- User confirmed the fixed wrapping in-game: multi-buyer entries keep merchant name and Tier together.
- User confirmed the feature otherwise continued to work correctly.
- This acceptance closes the only presentation defect found in 0.1.0.

---

## 0.1.0 — superseded candidate

- Status: runtime behavior passed; superseded by 0.1.1 for one presentation defect
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

### Runtime evidence

- Plugin loaded normally in Graveyard Keeper 1.407 / BepInEx 5.4.23.5.
- Structural buyer index built successfully at `OnGameStartedPlaying`: 234 indexed items, 267 buyer entries, 24 conceptual vendor entries.
- No Who Buys This? initialization, index-build, or tooltip-rendering error was observed in the returned log.
- User visually confirmed correct buyer/tier output for one-buyer and multi-buyer examples.
- User checked inventory plus technology and cooking UI contexts without observing regressions.
- User checked multiple game languages; native merchant names followed the active language.
- Remaining defect was only the long-line wrap fixed in 0.1.1.

### Intended architecture retained

- One structural exact-item buyer index after `MainGame.OnGameStartedPlaying()`.
- Standard `ItemDefinition.GetTooltipData()` postfix.
- Native `KnownNPCList` filter for ordinary merchants.
- Conditional product-type re-check only for conditional candidates.
- Staged Game of Crone vendor proxies merged into one conceptual merchant.
- No Vendor/NPC construction, polling, per-frame work, economy/save mutation, or price calculation.
