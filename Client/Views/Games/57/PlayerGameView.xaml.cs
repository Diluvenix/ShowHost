using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for PlayerGameView.xaml
    /// </summary>
    public partial class PlayerGameView : UserControl
    {
        private readonly Label[] labels;

        public PlayerGameView()
        {
            InitializeComponent();

            labels = [
                Label1,
                Label2,
                Label3,
                Label4,
                Label5,
                Label6,
                Label7,
                Label8,
                Label9,
                Label10,
                Label11,
                Label12,
                Label13,
                Label14,
                Label15,
                Label16,
                Label17,
                Label18,
                Label19,
                Label20,
                Label21,
                Label22,
                Label23,
                Label24,
                Label25,
                Label26,
                Label27,
                Label28,
                Label29,
                Label30,
                Label31,
                Label32,
                Label33,
                Label34,
                Label35,
                Label36,
                Label37,
                Label38,
                Label39,
                Label40,
                Label41,
                Label42,
                Label43,
                Label44,
                Label45,
                Label46,
                Label47,
                Label48,
                Label49,
                Label50,
                Label51,
                Label52,
                Label53,
                Label54,
                Label55,
                Label56,
                Label57
            ];
        }

        public void UpdateEntries(string[] entries)
        {
            for (int i = 0; i < Math.Min(entries.Length, 56); i++)
                labels[i].Content = string.Format("{0}. {1}", i + 1, entries[i]);

            if (entries.Length >= 57)
                labels[56].Content = entries[56];
        }
    }
}
