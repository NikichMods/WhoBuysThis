# Who Buys This?

A vanilla-friendly informational QoL mod for Graveyard Keeper 1.407.

Planned behavior: extend the standard item tooltip with the merchant(s) that buy the item and the trading Tier at which the sale becomes available.

Current status: architecture research. No production mod or release yet.

Initial scope does not change prices, economy, merchant stock, trading tiers, items, or progression, and it does not show sale-price estimates.

The implementation should derive buyer information from Graveyard Keeper's native trading data and mechanisms whenever possible instead of maintaining a manual item-to-vendor table.

See `docs/VERIFIED_GAME_DATA.md` for the current evidence and remaining runtime questions.
