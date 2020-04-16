using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;

namespace GlobalMarket
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GlobalMarketPlugin : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Persistent<GlobalMarketConfig> _config;
        public GlobalMarketConfig Config => _config.Data;

        private GlobalMarketControl _control;
        public UserControl GetControl() => _control ?? (_control = new GlobalMarketControl(this));

        private string PurchaseOrderFilePath => Path.Combine(StoragePath, $"{Name}PurchaseOrders.xml");
        public ConcurrentDictionary<string, PurchaseOrder> PurchaseOrders;

        private readonly TaskFactory _fifoTaskFactory =
            new TaskFactory(new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
            SetUpPurchaseOrders();
            Log.Info("Loaded");
        }

        private void SetupConfig() =>
            _config = Persistent<GlobalMarketConfig>.Load(Path.Combine(StoragePath, $"{Name}.cfg"));

        //c# can't serialize dictionary to xml, WTF
        private void SetUpPurchaseOrders()
        {
            PurchaseOrders = new ConcurrentDictionary<string, PurchaseOrder>();
            if (File.Exists(PurchaseOrderFilePath))
            {
                using (var textReader = File.OpenText(PurchaseOrderFilePath))
                {
                    var rootElement = XElement.Load(textReader);
                    rootElement.Elements().ForEach(it => PurchaseOrders.TryAdd(
                        it.Name.LocalName.Substring(1),
                        it.Element("PurchaseOrder").FromXElement<PurchaseOrder>()
                    ));
                }
            }
            else
            {
                SavePurchaseOrders();
            }
        }

        public void Reload() => SetUpPurchaseOrders();

        internal void SaveConfig()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (Exception e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal void SavePurchaseOrders()
        {
            try
            {
                var rootElement = new XElement("PurchaseOrders",
                    PurchaseOrders.Select(it => new XElement($"x{it.Key}", it.Value.ToXElement())));
                using (var textWriter = File.CreateText(PurchaseOrderFilePath)) rootElement.Save(textWriter);
            }
            catch (Exception e)
            {
                Log.Error(e, "PurchaseOrders failed to save");
                throw;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal void SavePurchaseOrdersSync() => _fifoTaskFactory.StartNew(SavePurchaseOrders);

        public string AddNewPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            var hash = purchaseOrder.GetHashCode();
            while (true)
            {
                var orderNumber = $"{hash:x}";
                if (PurchaseOrders.TryAdd(orderNumber, purchaseOrder))
                {
                    SavePurchaseOrdersSync();
                    return orderNumber;
                }

                hash++;
            }
        }

        public bool TryFinishOrder(string orderNumber, out PurchaseOrder purchaseOrder)
        {
            if (!PurchaseOrders.TryRemove(orderNumber, out purchaseOrder)) return false;
            SavePurchaseOrdersSync();
            return true;
        }
    }
}