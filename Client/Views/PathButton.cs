using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Views
{
    internal class PathButton : Button
    {
        public Geometry Data { get => (Geometry)GetValue(DataProperty); set => SetValue(DataProperty, value); }

        public static readonly DependencyProperty DataProperty
            = DependencyProperty.Register("Data", typeof(Geometry), typeof(PathButton), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    }
}
