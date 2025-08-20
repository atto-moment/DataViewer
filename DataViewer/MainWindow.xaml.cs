using System.Windows;
using System.Windows.Navigation;

namespace DataViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        double aspectRatio = 1920.0 / 1040.0;

        public MainWindow()
        {
            InitializeComponent();
            NavigationService.Navigate(Constant.NOMAL_VIEW);
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = e.NewSize.Width / aspectRatio;
        }
    }
}
