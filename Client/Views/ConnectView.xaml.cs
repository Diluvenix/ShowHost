using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for ConnectView.xaml
    /// </summary>
    public partial class ConnectView : UserControl
    {
        public string ErrorMessage
        {
            set
            {
                ErrorLabel.Content = value;
                ErrorLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xD1, 0x52, 0x62));
                ErrorLabel.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Hidden : Visibility.Visible;
            }
        }
        public string HintMessage
        {
            set
            {
                ErrorLabel.Content = value;
                ErrorLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xC4, 0xC4));
                ErrorLabel.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Hidden : Visibility.Visible;
            }
        }
        public string SuccessMessage
        {
            set
            {
                ErrorLabel.Content = value;
                ErrorLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xF5, 0x91));
                ErrorLabel.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public string ServerAddress => ServerAddressInputBox.Text;
        public int Port => int.TryParse(PortInputBox.Text, out var result) ? result : 0;
        public string Username => UsernameInputBox.Text;

        public string ModKey => ModKeyInputBox.Text;
        public bool IsModChecked => ModCheckBox.IsChecked ?? false;
        public string RecoveryKey => RecoveryKeyInputBox.Text;
        public bool IsRecoveryChecked => RecoveryCheckBox.IsChecked ?? false;


        public ConnectView()
        {
            InitializeComponent();

            ModCheckBox.Click += (_, _) =>
            {
                ModKeyInputBox.IsEnabled = ModCheckBox.IsChecked ?? false;

                RecoveryCheckBox.IsChecked = false;
                RecoveryKeyInputBox.IsEnabled = false;
            };

            RecoveryCheckBox.Click += (_, _) =>
            {
                RecoveryKeyInputBox.IsEnabled = RecoveryCheckBox.IsChecked ?? false;

                ModCheckBox.IsChecked = false;
                ModKeyInputBox.IsEnabled = false;
            };
        }

        public void AddConnectButtonClickEventHandler(RoutedEventHandler eventHandler)
            => ConnectButton.Click += eventHandler;

        public void SetControllsEnabled(bool enabled)
        {
            ServerAddressInputBox.IsEnabled = enabled;
            PortInputBox.IsEnabled = enabled;
            UsernameInputBox.IsEnabled = enabled;
            ConnectButton.IsEnabled = enabled;

            ModCheckBox.IsEnabled = enabled;
            ModKeyInputBox.IsEnabled = enabled && (ModCheckBox.IsChecked ?? false);
            RecoveryCheckBox.IsEnabled = enabled;
            RecoveryKeyInputBox.IsEnabled = enabled && (RecoveryCheckBox.IsChecked ?? false);
        }
    }
}
