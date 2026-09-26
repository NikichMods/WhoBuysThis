# Test Build Log

## 1.0.1 — stable

- Status: runtime accepted / stable
- Purpose: localize the mod-owned buyer heading for every Graveyard Keeper interface language
- Parent stable release: 1.0.0
- Source commit: `11249ae8f5bf816eaab18987c2162419e057e4cf`
- Frozen source ref: `candidate/1.0.1`
- Development branch: `dev/1.0.1`
- GitHub Actions run: `36237282249`
- Artifact ID: `10904104341`
- Artifact name: `WhoBuysThis-1.0.1-11249ae8f5bf816eaab18987c2162419e057e4cf`
- Artifact ZIP digest: `sha256:8ec4d5de34bfbb1251f60f01f9ffd4b573bac7ce080968131527d40d15a06290`
- DLL: `WhoBuysThis.dll`
- DLL SHA-256: `026948f78b39a865e449e9cf8e820f70dd57e4bf8d2d5731f6045f76c510faf4`
- Build: Windows GitHub-hosted runner, Release/net48, successful

### Change

The only mod-owned localized tooltip heading now covers all 11 interface languages supported by Graveyard Keeper:

- English: `Buyers`
- German: `Käufer`
- Spanish: `Compradores`
- French: `Acheteurs`
- Italian: `Acquirenti`
- Japanese: `買い手`
- Korean: `구매자`
- Polish: `Kupujący`
- Brazilian Portuguese: `Compradores`
- Russian: `Покупают`
- Simplified Chinese: `买家`

Language codes are normalized case-insensitively with `_` / `-` equivalence, so variants such as `pt_BR` / `pt-BR` and `zh_CN` / `zh-CN` resolve identically. Unknown/empty codes fall back to English.

Merchant names remain owned by the game and are still localized on demand through `GJL.L`. Buyer discovery, known/unlocked filtering, Tier calculation, non-breaking-space formatting, and tooltip structure are unchanged.

The standard `BubbleWidgetText.Draw()` path calls `GJL.EnsureLabelHasCorrectFont(...)`, so this candidate deliberately does not add a parallel font map or language-change hook.

### Runtime acceptance

- User visually confirmed that the buyer heading switches correctly and renders normally while cycling the supported game languages.
- Returned runtime log shows `Who Buys This? 1.0.1` loading successfully.
- Buyer index initialized successfully with no Who Buys This? initialization, index-build, or tooltip-rendering exception.
- The same log records live game language loads for all 11 supported locale codes, including `pt-br` and `zh_cn`.
- The observed index diagnostic counts differ from the earlier 1.0.0 test session, but `BuyerIndex.cs` is unchanged in 1.0.1; the counts reflect the loaded native/modded balance/world state and are not evidence of a localization regression.
- Acceptance scope was localization/presentation only; trading behavior was intentionally not re-tested because that behavior is unchanged from stable 1.0.0.

---

## 1.0.0 — stable

- Status: accepted stable release
- Purpose: first public stable release
- Parent accepted behavior: 0.1.1
- Change from 0.1.1: version metadata only; production logic and UI behavior are unchanged
- Exact candidate source: `7470e783015faf59b32c7d51a77685fdbb731d59`
- Frozen source ref: `candidate/1.0.0`
- Development branch: `dev/1.0.0`
- GitHub Actions run: `35629287909`
- Artifact ID: `10654071531`
- Artifact name: `WhoBuysThis-1.0.0-7470e783015faf59b32c7d51a77685fdbb731d59`
- Artifact ZIP digest: `sha256:ec904d051b5c4649af877ef0c4b9a71ae39ce44183900b0f1269247c75170a2e`
- Stable DLL: `WhoBuysThis.dll`
- DLL SHA-256: `6f57726ad8dd81f0e32b7dfb8bf35187be8efade86e00132036c9ade7154e7f0`
- Build: Windows GitHub-hosted runner, Release/net48, successful
- Stable promotion commit: `30f1fea50c1e0585c740a92e82f1043bad99da2f`
- Stable tag: `v1.0.0` -> `30f1fea50c1e0585c740a92e82f1043bad99da2f`
- GitHub Release: `https://github.com/NikichMods/WhoBuysThis/releases/tag/v1.0.0`
- Release publication run: `36236411749`
- Release asset digest reported by GitHub: `sha256:6f57726ad8dd81f0e32b7dfb8bf35187be8efade86e00132036c9ade7154e7f0`
- Publication reused artifact `10654071531`; the publication workflow contained no compile/build step and verified the DLL SHA-256 before release upload.

### Acceptance basis

- Candidate 0.1.1 was runtime accepted after the non-breaking-space presentation fix.
- The production-code delta from accepted 0.1.1 to 1.0.0 changes only `PluginVersion` from `0.1.1` to `1.0.0`; the remaining candidate delta is build/version metadata and documentation.
- The exact 1.0.0 candidate was built successfully from `7470e783015faf59b32c7d51a77685fdbb731d59`.
- On 2026-09-26 the user confirmed that the release was already considered completed and authorized closing the remaining repository promotion/documentation work. A duplicate runtime smoke test was therefore not required.
- Stable promotion reused the exact accepted DLL above. No 1.0.0 rebuild was performed.

---

## 0.1.1 — accepted candidate / superseded by 1.0.0

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
- This acceptance closed the only presentation defect found in 0.1.0.

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
