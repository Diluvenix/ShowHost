using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;

namespace Client
{
    public class UiScaler
    {
        private double scale = 1.0;
        public double Scale
        {
            get => scale;
            set {
                if (Math.Abs(scale - value) < 0.0001)
                    return;

                scale = value;
                UpdateScaledValues();
            }
        }

        private void UpdateScaledValues()
        {
            ResourceDictionary resources = Application.Current.Resources;

            resources["FontMicro"] = 1.2 * scale;
            resources["FontXTiny"] = 3.5 * scale;
            resources["FontXSmall"] = 10 * scale;
            resources["FontSmall"] = 12 * scale;
            resources["FontNormal"] = 15 * scale;
            resources["FontBig"] = 20 * scale;
            resources["FontLarge"] = 25 * scale;
            resources["FontXLarge"] = 36 * scale;
            resources["FontHuge"] = 48 * scale;

            resources["RadiusNormal"] = new CornerRadius(5 * scale);
            resources["RadiusLarge"] = new CornerRadius(8 * scale);
            
            resources["CardHeightNormal"] = 70 * scale;

            resources["MarginNormal"] = new Thickness(3 * scale);
            resources["MarginNormalHorizontal"] = new Thickness(3 * scale, 0, 3 * scale, 0);
            resources["MarginLargeVertical"] = new Thickness(0, 3 * scale, 0, 3 * scale);
            resources["MarginNormalVerticalNegative"] = new Thickness(0, -3 * scale, 0, -3 * scale);
            resources["MarginLarge"] = new Thickness(10 * scale);
            resources["MarginLargeHorizontal"] = new Thickness(10 * scale, 0, 10 * scale, 0);
            resources["MarginLargeVertical"] = new Thickness(0, 10 * scale, 0, 10 * scale);
        }
    }
}
