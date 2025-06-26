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

namespace DataViewer
{
    /// <summary>
    /// TurnViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class TurnViewer : Page
    {
        public TurnViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Change a viewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeViewer(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            Page page;

            if (clickedButton.Name == "NomalView")
            {
                page = new Viewer();
                NavigationService.Navigate(page);
            }
        }
    }
}
