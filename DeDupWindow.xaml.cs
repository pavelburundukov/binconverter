using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace GuiBinConverter
{
    /// <summary>
    /// Interaction logic for DeDupWindow.xaml
    /// </summary>
    public partial class DeDupWindow : Window
    {
        private readonly ObservableCollection<TradesTickerInfo> _tickers = new ObservableCollection<TradesTickerInfo>();
        private readonly ObservableCollection<DataProvInfo> _dataProviders = new ObservableCollection<DataProvInfo>();

        private string GetSelectedBinPath()
        {
            // если выбран провайдер, то ищем в нем иначе проверяем введенную папку на существование
            if (!(bool)chbxFromFolder.IsChecked)
            {
                // Получим инфо по нашему провайдеру.
                var dataInfo = cbxDataProviders.SelectedItem as DataProvInfo;
                if (dataInfo == null)
                    return "";

                if (!Directory.Exists(dataInfo.TickPath))
                    return "";

                return dataInfo.TickPath;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(tbxBinPath.Text);
            if (!Directory.Exists(expandedPath))
                return "";
            
            //cbxDataProviders.SelectedItem
            return expandedPath;
        }


        public DeDupWindow()
        {
            InitializeComponent();

            cbxTickers.ItemsSource = _tickers;
            cbxDataProviders.ItemsSource = _dataProviders;
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var tslabDataPath = Path.Combine(localDataPath, "TSLab", "TSLab12");
        }

        private void btnBrowseBin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog()
                        {
                            SelectedPath = Common.GetTsLabAppDataPath(),
                            ShowNewFolderButton = false
                        };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                tbxBinPath.Text = dialog.SelectedPath;
            //cbxDataProviders.SelectionBoxItemTemplate
        }

        private void btnLoadBinTickers_Click(object sender, RoutedEventArgs e)
        {
            _tickers.Clear();

            var tickPath = GetSelectedBinPath();    // если путь нельзя использовать или он неверный, вернет ""
            if (tickPath == "")
            {
                MessageBox.Show("Не получается загрузить тикеры. Неправильный путь.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var tickerInfos = Common.GetTicksList(tickPath);
            tickerInfos.ForEach(_tickers.Add);
        }

        private async void btnDeDup_Click(object sender, RoutedEventArgs e)
        {
            // Получим выбранный тикер, если ничего не выбрали то выходим.
            var tickerInfo = cbxTickers.SelectedItem as TradesTickerInfo;
            if (tickerInfo == null)
                return;

            // если границы не заданы, то диапазона не будет, будем брать все.
            DateRange dateRange;
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                dateRange = null;
            }
            else
            {
                var from = (DateTime)dpFrom.SelectedDate;
                var to = (DateTime)dpTo.SelectedDate;
                if (from > to)
                {
                    MessageBox.Show("Дата От должна быть всегда не больше даты До.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                dateRange= new DateRange(from, to);
            }
            

            // Выключим кнопку записи в файл чтобы сто раз не жмакали
            // так как у нас включен биндинг, нужно его засейвить  потом ресторить.
            var btn = ((Button) sender);
            BindingBase binding = null;
            if (BindingOperations.IsDataBound(btn, IsEnabledProperty))
            {
                binding = BindingOperations.GetBinding(btn, IsEnabledProperty) ??
                          (BindingBase)BindingOperations.GetMultiBinding(btn, IsEnabledProperty);
            }

            btn.IsEnabled = false;

            // Инициализируем прогресс бар с учетом выбранного диапазона
            prgbBin2Txt.Minimum = 0;
            prgbBin2Txt.Maximum = 100;
            prgbBin2Txt.Value = 0;

            // прогрессор, будет апдейтить прогрессбар и строку статуса
            var progress = new Progress<ProgressReport>(report =>
            {
                prgbBin2Txt.Value = (int) report.Percent;
                if (report.Finished)
                    lblTickProcessStatus.Content = "Заменено: {0}   Затрачено: {1}".Put(report.ProcessedCount, report.TimeUsed);
                else
                    lblTickProcessStatus.Content = "Обработано: {0}   Затрачено: {1}".Put(report.ProcessedCount, report.TimeUsed);
            });

            // заводим асинхронно конвертацию, если будут ошибки, они хэндлятся штатным образом.
            try
            {
                await Common.BinDeDupTradesAdync(tickerInfo, dateRange, progress);
            }
            catch (Exception ex)
            {
                Trace.Fail(ex.ToString());
                lblTickProcessStatus.Content = "Error: {0}".Put(ex.Message);
                this.UpdateLayout();
            }
            
            // восстановим биндинг кнопки, сбросим прогрессбар
            btn.SetBinding(IsEnabledProperty, binding);
            prgbBin2Txt.Value = 0;
            prgbBin2Txt.UpdateLayout();
        }

        private void cbxDataProviders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _tickers.Clear();
        }

        private void CbxTickers_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cbx = sender as System.Windows.Controls.ComboBox;

            dpFrom.DisplayDateStart = null;
            dpFrom.DisplayDateEnd = null;
            dpFrom.SelectedDate = null;
            dpFrom.BlackoutDates.Clear();

            dpTo.DisplayDateStart = null;
            dpTo.DisplayDateEnd = null;
            dpTo.SelectedDate = null;
            dpTo.BlackoutDates.Clear();


            if (cbx.SelectedItem == null) 
                return;


            var tickerInfo = cbx.SelectedItem as TradesTickerInfo;
            var minDate = tickerInfo.MinDate;
            var maxDate = tickerInfo.MaxDate;

            dpFrom.DisplayDateStart = minDate;
            dpFrom.DisplayDateEnd = maxDate;
            dpFrom.SelectedDate = minDate;

            dpTo.DisplayDateStart = minDate;
            dpTo.DisplayDateEnd = maxDate;
            dpTo.SelectedDate = maxDate;

            // промаркируем те дни в которые нет кэша.
            for (DateTime current = minDate; current <= maxDate; current += TimeSpan.FromDays(1))
            {
                if (tickerInfo.Dates.Contains(current))
                    continue;

                dpFrom.BlackoutDates.Add(new CalendarDateRange(current));
                dpTo.BlackoutDates.Add(new CalendarDateRange(current));
            }
        }

        private void btnRefreshDataProviders_Click(object sender, RoutedEventArgs e)
        {
            //var dp = DataProvidersCollection.DataProviders;
            //dp.Clear();
            //Common.GetDataProviders().ForEach(dp.Add);
            _dataProviders.Clear();
            Common.GetDataProviders().ForEach(_dataProviders.Add);
        }
    }
}
