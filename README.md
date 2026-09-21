# Who Buys This?

A vanilla-friendly informational QoL mod for Graveyard Keeper 1.407.

Goal: extend the standard item tooltip with the merchant(s) that buy the item and the trading Tier at which the sale becomes available.

Current status: architecture research is complete and the first-release implementation architecture is frozen. Production implementation/test build is next.

Initial scope does not change prices, economy, merchant stock, trading tiers, items, or progression, and it does not show sale-price estimates.

Buyer information is derived from Graveyard Keeper's native trading data rather than a manual item-to-vendor table. The design uses one structural buyer index plus cheap on-demand checks for known merchants, conditional vendor types, and staged Game of Crone vendor proxies.

See `docs/VERIFIED_GAME_DATA.md` for the verified 1.407 data and accepted architecture.
