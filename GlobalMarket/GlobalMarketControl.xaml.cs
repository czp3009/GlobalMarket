using System.Windows;

namespace GlobalMarket
{
    public partial class GlobalMarketControl
    {
        private readonly GlobalMarketPlugin _plugin;

        private GlobalMarketControl()
        {
            InitializeComponent();
        }

        public GlobalMarketControl(GlobalMarketPlugin plugin) : this()
        {
            _plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            _plugin.SaveConfig();
        }
    }
}