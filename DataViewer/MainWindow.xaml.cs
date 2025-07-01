using System.Windows.Navigation;

namespace DataViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationService.Navigate(Constant.NOMAL_VIEW);
        }
    }
}
