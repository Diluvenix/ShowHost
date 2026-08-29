using System.Windows;
using System.Windows.Controls;

namespace Client.Views
{
    public class InputBox : TextBox
    {
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
        public string Watermark { get => (string)GetValue(WatermarkProperty); set => SetValue(WatermarkProperty, value); }

        public static readonly DependencyProperty CornerRadiusProperty 
            = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(InputBox), new FrameworkPropertyMetadata(default(CornerRadius), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty WatermarkProperty
            = DependencyProperty.Register("Watermark", typeof(string), typeof(InputBox));
    }
}
