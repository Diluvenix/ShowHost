using Client.Controllers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for ModeratorPanel.xaml
    /// </summary>
    public partial class ModeratorPanel : UserControl
    {
        static readonly Brush smokeWhite = new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf5));
        readonly ResourceDictionary resources = Application.Current.Resources;


        public ModeratorPanel()
        {
            InitializeComponent();
        }

        internal void SetPanels(IController[] controllers)
        {
            foreach (IController controller in controllers)
            {
                PathButton button = new()
                {
                    Data = controller.Path!,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = smokeWhite,
                    FontSize = (double)resources["FontXTiny"],
                    Height = (double)resources["FontXLarge"],
                    Margin = (Thickness)resources["MarginLargeVertical"],
                };
                button.Click += (_, _) =>
                {
                    ContentBorder.Child = controller.View;
                };

                ButtonStackPanel.Children.Add(button);
            }

            ContentBorder.Child = controllers[0].View;
        }
    }
}
