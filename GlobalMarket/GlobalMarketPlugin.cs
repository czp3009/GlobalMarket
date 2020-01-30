using NLog;
using Torch;
using Torch.API;

namespace GlobalMarket
{
    public class GlobalMarketPlugin : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
        }
    }
}