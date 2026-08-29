using Client.Controllers;
using Client.Views;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Window.SizeChanged += Window_SizeChanged;

            MainController.Instance = new MainController(this);
            MainController.Instance.SetController(MainController.ControllerType.Connect);
            Closing += (_, _) => MainController.Instance.Dispose();
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.UiScaler.Scale = Math.Min(
                ActualHeight / 900,
                ActualWidth / 1600
            );
        }
    }
}