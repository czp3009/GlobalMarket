# GlobalMarket
A [Torch](https://torchapi.net/) plugin that let players sell and buy items to and from market via command.

https://torchapi.net/plugins/item/025e099c-c046-4d4a-89c4-d770fe485e16

# Command
!market help - Show help message

!market inventory - Show all items in inventory and cargo

!market sell <itemName> <amount> <price> - Sell items to market

!market buy <orderNumber> - Buy items from market

!market search \[itemName\] - Search items in market

!market longsearch \[itemName\] - Search items in market with dialog

!market my - Show my items in market

!market longmy - Show my items in market with dialog

# Note
If player aim at CargoContainer and have permission to access it, item will delivery to CargoContainer first when use "!market buy".

'Aim At' means player must hold a tool and actually point at cargo port(HUD highlight).

"!market sell" will take items from player inventory first, then from CargoContainer.

Item name is not case sensitive.

Tax are borne by the seller.

# License
Apache License 2.0
