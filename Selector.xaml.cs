using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace GuiBinConverter
{
    /// <summary>
    /// Interaction logic for Selector.xaml
    /// </summary>
    public partial class Selector : Window
    {
        public Selector()
        {
            InitializeComponent();
        }

        private void BtnTicks_Click(object sender, RoutedEventArgs e)
        {
            var mw = new TickConverterWindow();
            mw.Show();

            this.Close();
        }

        private void BtnBars_Click(object sender, RoutedEventArgs e)
        {
            var mw = new BarConverterWindow();
            mw.Show();

            this.Close();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void BtnDeDup_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var mw = new DeDupWindow();
            mw.Show();

            this.Close();
        }
    }
}
