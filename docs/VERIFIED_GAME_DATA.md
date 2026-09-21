# Verified Game Data

Target: Graveyard Keeper 1.407 (PC).

This document is the durable source of truth for game/runtime facts that production code may rely on.

## Evidence status

Research in progress. Do not promote hypotheses to production assumptions without direct evidence.

## Questions to close

- Complete merchant/vendor source.
- Native buy-eligibility rule.
- Tier requirement source and semantics.
- Feasibility of building an item -> buyers/Tier index from native balance/vendor data.
- Compatibility implications of Harmony patches to vendor eligibility methods.
- Localized merchant display-name source.
- Standard tooltip construction seam.
- Cache build lifecycle and invalidation.
- Difference between potential eligibility and current trade availability.
- DLC, special merchants, quality items, and other edge cases.

## Product decision still open

Whether tooltips should show:
1. every potential buyer represented by game data; or
2. only merchants already discovered/met by the current player.

Investigate both technically; do not choose silently.
