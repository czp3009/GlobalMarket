using System;
using VRage;
using VRage.Game;

namespace GlobalMarket
{
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
    public class PurchaseOrder
    {
        public long SellerIdentityId { get; set; }
        public string ItemType { get; set; }
        public string ItemSubType { get; set; }
        public string ItemFullName => $"{ItemType}/{ItemSubType}";
        public MyFixedPoint Amount { get; set; }
        public long Price { get; set; }
        public DateTime CreateTime { get; set; }

        // ReSharper disable once UnusedMember.Global
        public PurchaseOrder()
        {
        }

        public PurchaseOrder(
            long sellerIdentityId,
            MyDefinitionId definitionId,
            MyFixedPoint amount,
            long price,
            DateTime? createTime = null
        )
        {
            if (definitionId.TypeId.IsNull || definitionId.SubtypeName == "")
            {
                throw new ArgumentException("Invalid DefinitionId");
            }

            SellerIdentityId = sellerIdentityId;
            //can't serialize DefinitionId
            ItemType = definitionId.TypeId.ToString();
            ItemSubType = definitionId.SubtypeName;
            Amount = amount;
            Price = price;
            CreateTime = createTime ?? DateTime.UtcNow;
        }
    }
}