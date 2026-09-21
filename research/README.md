# Vendor Matrix Research Harness

Research-only diagnostic for **Who Buys This?**. It is not production code and must not be merged into the shipping mod by default.

## Questions this harness closes

- Which `VendorDefinition` rows exist in the player's actual Graveyard Keeper 1.407 runtime.
- Which definitions map to world objects, NPC aliases and the save's `known_npcs` list.
- Whether special/DLC vendors map cleanly to the game's own "met NPC" state.
- Whether any vendor uses state-dependent `additional_types`.
- What `not_buying` tier exceptions actually exist.
- Whether quality variants have different buyer sets.
- Whether the side-effect-free data-derived `CanBuyItem` model matches native `Vendor.CanBuyItem(..., true)` for Vendor instances the game has already created naturally.

## Safety / lifecycle

- Never accesses the lazy `WorldGameObject.vendor` property.
- Never constructs a `Vendor`.
- Never changes vendor tier, money, inventory or save data.
- Empty `known_npcs` / zero known merchants is treated as a valid ready state.
- Structural item/vendor discovery is independent of known-NPC membership.
- After the initial report, the only recurring work is a cheap once-per-second fingerprint of `known_npcs`. A change is debounced for two seconds before one new report is written. No structural rebuild is triggered merely because the list is empty.

## Output

The harness writes to `BepInEx/WhoBuysThisResearch/`:

- `latest-report.txt` — concise summary and anomalies;
- `vendor-catalog.tsv` — every vendor definition plus localization/WGO/known-NPC mapping;
- `buyer-matrix.tsv` — exact item ID -> potential buyer/effective tier rows, with `is_known` as a separate filter dimension.

It also writes one grep-friendly `WHO_BUYS_THIS_DIAGNOSTIC` line to `LogOutput.log` whenever a report is produced.

## Local build/install

Hosted CI is intentionally not used for this research artifact.

On the Windows machine with Graveyard Keeper + BepInEx installed, run `research/Build-And-Install.cmd`. The script locates the Steam installation, compiles against the already installed BepInEx/Unity assemblies using the local .NET Framework C# compiler, and installs `WhoBuysThisResearch.dll` into its own plugin folder.

`research/Remove-Harness.cmd` removes the installed harness.
