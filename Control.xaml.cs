using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch.Views;

namespace DisableProjectedBlocks
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    public partial class Control : UserControl
    {
        private MainLogic Plugin { get; }

        public Control()
        {
            InitializeComponent();
        }

        public Control(MainLogic plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }
        
        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void EditRemovedBlocks_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new CollectionEditor() {Owner = Window.GetWindow(this)};
            editor.Edit<string>(Plugin.Config.RemoveBlocks, "Removed Blocks - Use pairnames,typeId and/or subtypeId");
        }

    }
}
