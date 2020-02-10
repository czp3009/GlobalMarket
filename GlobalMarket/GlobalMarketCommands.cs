using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace GlobalMarket
{
    [Category("market")]
    // ReSharper disable UnusedMember.Global
    // ReSharper disable once UnusedType.Global
    public class GlobalMarketCommands : CommandModule
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private GlobalMarketPlugin Plugin => (GlobalMarketPlugin) Context.Plugin;
        private GlobalMarketConfig Config => Plugin.Config;

        [Command("reload", "Reload purchase orders in disk")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload() => Plugin.Reload();

        [Command("help", "Show help")]
        [Permission(MyPromoteLevel.None)]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void Help()
        {
            Respond("Available commands:",
                "!market help - Show help message",
                "!market inventory - Show all items in inventory and cargo",
                "!market sell <itemName> <amount> <price> - Sell items to market",
                "!market buy <orderNumber> - Buy items from market",
                "!market search [itemName] - Search items in market",
                "!market longsearch [itemName] - Search items in market with dialog",
                "!market my - Show my items in market",
                "!market longmy - Show my items in market with dialog"
            );
        }

        [Command("inventory", "Show all items in inventory and cargo")]
        [Permission(MyPromoteLevel.None)]
        public void Inventory()
        {
            if (!CheckIsPlayer(out _)) return;

            IEnumerable<string> Format(MyInventoryBase inventory) => inventory.GetItems()
                .GroupBy(
                    it => it.Content,
                    it => it.Amount,
                    (physicalObject, amounts) => (
                        physicalObject, amount: amounts.Aggregate(MyFixedPoint.Zero, (acc, x) => acc + x)
                    ),
                    new MyObjectBuilderPhysicalObjectComparer()
                )
                .Select(it => $"{it.physicalObject.TypeId}/{it.physicalObject.SubtypeName}({it.amount})");

            if (TryGetPlayerInventory(out var playerInventory))
            {
                Respond("Items in your inventory:", Format(playerInventory));
            }

            if (TryGetAimedHavePermissionCargoInventory(out var aimedInventory))
            {
                Respond("Items in CargoContainer:", Format(aimedInventory));
            }
        }

        [Command("sell", "Sell items to market")]
        [Permission(MyPromoteLevel.None)]
        public void Sell(string itemName, int amount, long price)
        {
            if (!CheckIsPlayer(out var player)) return;

            if (amount < 1)
            {
                Respond("Amount at least be 1");
                return;
            }

            if (price < 1)
            {
                Respond("Price at least be 1");
                return;
            }

            var playerInventory = GetPlayerInventory();
            var playerInventoryItems =
                playerInventory?.GetItems() ?? Enumerable.Empty<MyPhysicalInventoryItem>().ToList();
            var cargoInventory = GetAimedHavePermissionCargoInventory();
            var cargoInventoryItems =
                cargoInventory?.GetItems() ?? Enumerable.Empty<MyPhysicalInventoryItem>().ToList();
            if (playerInventoryItems.Count == 0 && cargoInventoryItems.Count == 0)
            {
                Respond("Inventory is empty");
                return;
            }

            MyDefinitionId definitionId;
            MyPhysicalInventoryItem[] playerInventoryMatchedItems;
            MyPhysicalInventoryItem[] cargoInventoryMatchedItems;

            bool NoItemMatched()
            {
                if (playerInventoryMatchedItems.Length != 0 || cargoInventoryMatchedItems.Length != 0) return false;
                Respond("No such item in inventory");
                return true;
            }

            if (itemName.Contains('/'))
            {
                if (!MyDefinitionId.TryParse(itemName, out definitionId))
                {
                    Respond("Invalid itemType");
                    return;
                }

                playerInventoryMatchedItems = playerInventoryItems
                    .Where(it => it.Content.GetObjectId() == definitionId).ToArray();
                cargoInventoryMatchedItems = cargoInventoryItems
                    .Where(it => it.Content.GetObjectId() == definitionId).ToArray();
                if (NoItemMatched()) return;
            }
            else
            {
                playerInventoryMatchedItems = playerInventoryItems
                    .Where(it => it.Content.SubtypeName == itemName).ToArray();
                cargoInventoryMatchedItems = cargoInventoryItems
                    .Where(it => it.Content.SubtypeName == itemName).ToArray();
                if (NoItemMatched()) return;

                var distinctPhysicalObjects = playerInventoryMatchedItems.Concat(cargoInventoryMatchedItems)
                    .Select(it => it.Content)
                    .Distinct(new MyObjectBuilderPhysicalObjectComparer()).ToArray();
                if (distinctPhysicalObjects.Length > 1)
                {
                    Respond("Multi items matched, please use fully qualified name:",
                        distinctPhysicalObjects.Select(it => $"{it.TypeId}/{it.SubtypeName}")
                    );
                    return;
                }

                definitionId = distinctPhysicalObjects[0].GetObjectId();
            }

            var playerInventoryHave = playerInventoryMatchedItems
                .Select(it => it.Amount)
                .Aggregate(MyFixedPoint.Zero, (acc, x) => acc + x);
            var cargoInventoryHave = cargoInventoryMatchedItems
                .Select(it => it.Amount)
                .Aggregate(MyFixedPoint.Zero, (acc, x) => acc + x);
            var need = (MyFixedPoint) amount;
            //totalHave may overflow
            if (playerInventoryHave < need && cargoInventoryHave < need &&
                playerInventoryHave + cargoInventoryHave < need)
            {
                Respond($"No enough {definitionId}");
                return;
            }

            if (Config.OrderCountLimitPerPlayer != 0 &&
                Plugin.PurchaseOrders.Values.Count(it => it.SellerIdentityId == player.Identity.IdentityId) >
                Config.OrderCountLimitPerPlayer)
            {
                Respond($"You can only create {Config.OrderCountLimitPerPlayer} purchase orders");
                return;
            }

            //sell items in player's inventory first
            if (playerInventoryHave > need)
            {
                playerInventory?.RemoveItemsOfType(need, definitionId);
            }
            else
            {
                playerInventory?.RemoveItemsOfType(playerInventoryHave, definitionId);
                cargoInventory?.RemoveItemsOfType(need - playerInventoryHave, definitionId);
            }

            var orderNumber =
                Plugin.AddNewPurchaseOrder(new PurchaseOrder(player.IdentityId, definitionId, need, price));
            var s = $"{definitionId}({amount}) to market for ${price}, order number: {orderNumber}";
            if (Config.BroadcastOnSell) SendMessage($"Player '{player.DisplayName}' sold {s}");
            else Respond($"Selling {s}");
        }

        [Command("buy", "Buy items from market")]
        [Permission(MyPromoteLevel.None)]
        public void Buy(string orderNumber)
        {
            if (!CheckIsPlayer(out var player)) return;

            if (!Plugin.PurchaseOrders.TryGetValue(orderNumber, out var purchaseOrder))
            {
                Respond("No such purchase order");
                return;
            }

            var sellerIdentityId = purchaseOrder.SellerIdentityId;
            var isSelfOrder = sellerIdentityId == player.IdentityId;
            if (!Config.CanRepurchaseOwnOrder && isSelfOrder)
            {
                Respond("Can't repurchase your own order");
                return;
            }

            var sellerNotExist = !MySession.Static.Players.HasIdentity(sellerIdentityId);
            if (sellerNotExist)
            {
                Log.Error($"Seller {sellerIdentityId} of order <{orderNumber}> not exists!");
            }
            else if (!Config.CanRepurchaseFactionOrder && !isSelfOrder &&
                     MySession.Static.Factions.GetPlayerFaction(sellerIdentityId)?.IsMember(player.IdentityId) == true)
            {
                Respond("Can't repurchase your faction order");
                return;
            }

            if (!player.TryGetBalanceInfo(out var balance) || balance < purchaseOrder.Price)
            {
                Respond($"No enough money, need {purchaseOrder.Price} but you only have {balance}");
                return;
            }

            if (!MyDefinitionId.TryParse(purchaseOrder.ItemType, purchaseOrder.ItemSubType, out var definitionId) ||
                !MyDefinitionManager.Static.TryGetPhysicalItemDefinition(definitionId, out _))
            {
                Respond(
                    $"The order <{orderNumber}> contains invalid DefinitionId, may cause by mod changing or data corruption, please contact GM"
                );
                return;
            }

            var playerInventory = GetPlayerInventory();
            var canAddToPlayerInventory = playerInventory?.ComputeAmountThatFits(definitionId) ?? MyFixedPoint.Zero;
            var cargoInventory = GetAimedHavePermissionCargoInventory();
            var canAddToCargoInventory = cargoInventory?.ComputeAmountThatFits(definitionId) ?? MyFixedPoint.Zero;

            //totalCanAdd may overflow
            if (canAddToCargoInventory < purchaseOrder.Amount &&
                canAddToPlayerInventory < purchaseOrder.Amount &&
                canAddToPlayerInventory + canAddToCargoInventory < purchaseOrder.Amount)
            {
                Respond("No enough inventory space");
                return;
            }

            MyFixedPoint addToPlayerInventory;
            MyFixedPoint addToCargoInventory;
            if (canAddToCargoInventory > purchaseOrder.Amount)
            {
                addToCargoInventory = purchaseOrder.Amount;
                addToPlayerInventory = MyFixedPoint.Zero;
            }
            else
            {
                addToCargoInventory = canAddToCargoInventory;
                addToPlayerInventory = purchaseOrder.Amount - canAddToCargoInventory;
            }

            if (!Plugin.TryFinishOrder(orderNumber, out _))
            {
                Respond("No such purchase order");
                return;
            }

            player.RequestChangeBalance(-purchaseOrder.Price);
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject(definitionId);
            playerInventory?.AddItems(addToPlayerInventory, objectBuilder);
            cargoInventory?.AddItems(addToCargoInventory, objectBuilder);
            Respond($"You bought {definitionId} ({purchaseOrder.Amount}) for ${purchaseOrder.Price}");

            //if seller not exist
            if (sellerNotExist) return;

            var tax = (long) (Config.IllegalTaxRateDouble * purchaseOrder.Price);
            MyBankingSystem.RequestBalanceChange(sellerIdentityId, purchaseOrder.Price - tax);

            var s = $"<{orderNumber}> purchased by '{player.DisplayName}' for ${purchaseOrder.Price}(tax ${tax})";
            if (Config.BroadcastOnBuy)
            {
                SendMessage($"The order {s}");
            }
            else if (Config.NotifySellerOnBuy &&
                     MySession.Static.Players.TryGetPlayerId(sellerIdentityId, out var sellerPlayerId) &&
                     MySession.Static.Players.IsPlayerOnline(ref sellerPlayerId))
            {
                SendMessage(
                    $"Your order {s}",
                    sellerPlayerId.SteamId
                );
            }
        }

        private (string, IEnumerable<string>) SearchInternal(string itemName = null)
        {
            IEnumerable<KeyValuePair<string, PurchaseOrder>> enumerable = Plugin.PurchaseOrders;
            if (itemName != null)
            {
                enumerable = enumerable.Where(it =>
                    it.Value.ItemFullName.Contains(itemName, StringComparison.OrdinalIgnoreCase));
            }

            return (
                "Matched items in market:",
                enumerable.Select(it =>
                {
                    var (orderNumber, purchaseOrder) = it;
                    var sellerName =
                        MySession.Static.Players.TryGetIdentity(purchaseOrder.SellerIdentityId)?.DisplayName ??
                        "Unknown";
                    return
                        $"<{orderNumber}> '{sellerName}' {purchaseOrder.ItemFullName} ({purchaseOrder.Amount}) ${purchaseOrder.Price}";
                })
            );
        }

        [Command("search", "Search items in market")]
        [Permission(MyPromoteLevel.None)]
        public void Search(string itemName = null)
        {
            var (a, b) = SearchInternal(itemName);
            Respond(a, b);
        }

        // ReSharper disable once StringLiteralTypo
        [Command("longsearch", "Search items in market with dialog")]
        [Permission(MyPromoteLevel.None)]
        public void LongSearch(string itemName = null)
        {
            if (!CheckIsPlayer(out var player)) return;

            var (a, b) = SearchInternal(itemName);
            SendDialog(a, null, b, player.SteamUserId);
        }

        private (string, IEnumerable<string>) MyInternal(IMyPlayer player) =>
        (
            "My items in market:",
            Plugin.PurchaseOrders.Where(it => it.Value.SellerIdentityId == player.IdentityId)
                .Select(it => $"<{it.Key}> {it.Value.ItemFullName} ({it.Value.Amount}) ${it.Value.Price}")
        );

        [Command("my", "Show my items in market")]
        [Permission(MyPromoteLevel.None)]
        public void My()
        {
            if (!CheckIsPlayer(out var player)) return;

            var (a, b) = MyInternal(player);
            Respond(a, b);
        }

        // ReSharper disable once StringLiteralTypo
        [Command("longmy", "Show my items in market with dialog")]
        [Permission(MyPromoteLevel.None)]
        public void LongMy()
        {
            if (!CheckIsPlayer(out var player)) return;

            var (a, b) = MyInternal(player);
            SendDialog(a, null, b, player.SteamUserId);
        }

        private bool CheckIsPlayer(out IMyPlayer player)
        {
            if (Context.Player == null)
            {
                Context.Respond("In-game player only");
                player = null;
                return false;
            }

            player = Context.Player;
            return true;
        }

        private void Respond(string s) => Context.Respond(s);

        private void Respond(params string[] strings) =>
            Context.Respond(string.Join(Environment.NewLine, strings));

        private void Respond(string s, IEnumerable<string> enumerable) =>
            Context.Respond(string.Join(Environment.NewLine, enumerable.Prepend(s)));

        private bool TryGetPlayerInventory(out MyInventory inventory)
        {
            if (Context.Player.Character is MyCharacter character)
            {
                inventory = character.GetInventory();
                return true;
            }

            inventory = null;
            return false;
        }

        private MyInventory GetPlayerInventory()
        {
            TryGetPlayerInventory(out var inventory);
            return inventory;
        }

        private bool TryGetAimedCargo(out MyCubeBlock cubeBlock)
        {
            if (Context.Player.Character is MyCharacter character && character.AimedGrid != 0)
            {
                var aimedBlock = (MyEntities.GetEntityById(character.AimedGrid) as MyCubeGrid)
                    ?.GetCubeBlock(character.AimedBlock);
                if (aimedBlock?.BlockDefinition is MyCargoContainerDefinition)
                {
                    cubeBlock = aimedBlock.FatBlock;
                    return true;
                }
            }

            cubeBlock = null;
            return false;
        }

        private bool TryGetAimedHavePermissionCargoInventory(out MyInventory inventory)
        {
            inventory = null;
            if (!TryGetAimedCargo(out var cubeBlock)) return false;
            if (cubeBlock.OwnerId == 0)
            {
                inventory = cubeBlock.GetInventory();
                return true;
            }

            var relation = cubeBlock.GetUserRelationToOwner(Context.Player.IdentityId);
            if (relation != MyRelationsBetweenPlayerAndBlock.Owner &&
                relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
                return false;
            inventory = cubeBlock.GetInventory();
            return true;
        }

        private MyInventory GetAimedHavePermissionCargoInventory()
        {
            TryGetAimedHavePermissionCargoInventory(out var inventory);
            return inventory;
        }

        private void SendMessage(string message, ulong targetSteamId = 0)
        {
            Context.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>()
                ?.SendMessageAsOther(null, message, null, targetSteamId);
        }

        private static void SendDialog(string title, string subtitle, IEnumerable<string> enumerable, ulong target) =>
            ModCommunication.SendMessageTo(
                new DialogMessage(title, subtitle, string.Join(Environment.NewLine, enumerable)),
                target
            );
    }
}