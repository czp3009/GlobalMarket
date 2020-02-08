using Torch;

namespace GlobalMarket
{
    public class GlobalMarketConfig : ViewModel
    {
        private uint _orderCountLimitPerPlayer;
        private ushort _taxRate = 10;

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

        public double TaxRateDouble => _taxRate / 100D;
    }
}