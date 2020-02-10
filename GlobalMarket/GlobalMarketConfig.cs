using Torch;

namespace GlobalMarket
{
    public class GlobalMarketConfig : ViewModel
    {
        private uint _orderCountLimitPerPlayer;
        private ushort _taxRate = 10;
        private bool _canRepurchaseOwnOrder = true;
        private bool _canRepurchaseFactionOrder = true;
        private bool _broadcastOnSell = true;
        private bool _broadcastOnBuy = true;
        private bool _notifySellerOnBuy = true;

        public uint OrderCountLimitPerPlayer
        {
            get => _orderCountLimitPerPlayer;
            set => SetValue(ref _orderCountLimitPerPlayer, value);
        }

        public ushort TaxRate
        {
            get => _taxRate;
            set => SetValue(ref _taxRate, value);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public ushort IllegalTaxRate => _taxRate <= 100 ? _taxRate : (ushort) 100;

        public double IllegalTaxRateDouble => IllegalTaxRate / 100D;

        public bool CanRepurchaseOwnOrder
        {
            get => _canRepurchaseOwnOrder;
            set => SetValue(ref _canRepurchaseOwnOrder, value);
        }

        public bool CanRepurchaseFactionOrder
        {
            get => _canRepurchaseFactionOrder;
            set => SetValue(ref _canRepurchaseFactionOrder, value);
        }

        public bool BroadcastOnSell
        {
            get => _broadcastOnSell;
            set => SetValue(ref _broadcastOnSell, value);
        }

        public bool BroadcastOnBuy
        {
            get => _broadcastOnBuy;
            set => SetValue(ref _broadcastOnBuy, value);
        }

        public bool NotifySellerOnBuy
        {
            get => _notifySellerOnBuy;
            set => SetValue(ref _notifySellerOnBuy, value);
        }
    }
}