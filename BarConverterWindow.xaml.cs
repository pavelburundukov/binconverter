using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;
using TSLab.DataSource;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace GuiBinConverter
{
    /// <summary>
    /// Interaction logic for TickConverterWindow.xaml
    /// </summary>
    public partial class BarConverterWindow : Window
    {
        private readonly ObservableCollection<BaseTickerInfo> _tickers = new ObservableCollection<BaseTickerInfo>();
        private readonly ObservableCollection<DataProvInfo> _dataProviders = new ObservableCollection<DataProvInfo>();
        private readonly ObservableCollection<Interval> _timeframes = new ObservableCollection<Interval>();


        private string GetSelectedBinPath()
        {
            // если выбран провайдер, то ищем в нем иначе проверяем введенную папку на существование
            if (!(bool)chbxFromFolder.IsChecked)
            {
                // Получим инфо по нашему провайдеру.
                var dataInfo = cbxDataProviders.SelectedItem as DataProvInfo;
                if (dataInfo == null)
                    return "";

                if (!Directory.Exists(dataInfo.BarPath))
                    return "";

                return dataInfo.BarPath;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(tbxBinPath.Text);
            if (!Directory.Exists(expandedPath))
                return "";
            
            //cbxDataProviders.SelectedItem
            return expandedPath;
        }


        public BarConverterWindow()
        {
            InitializeComponent();

            cbxTickers.ItemsSource = _tickers;
            cbxDataProviders.ItemsSource = _dataProviders;
            cbxTimeframe.ItemsSource = _timeframes;
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

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

            var dirPath = GetSelectedBinPath();    // если путь нельзя использовать или он неверный, вернет ""
            if (dirPath == "")
            {
                MessageBox.Show("Не получается загрузить тикеры. Неправильный путь.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var tickerInfos = Common.GetBarsList(dirPath);
            tickerInfos.ForEach(_tickers.Add);
        }

        private async void btnSaveBin2Txt_Click(object sender, RoutedEventArgs e)
        {
            // Получим выбранный тикер, если ничего не выбрали то выходим.
            var tickerInfo = cbxTickers.SelectedItem as BarsTickerInfo;
            if (tickerInfo == null)
                return;
            var timeframe = cbxTimeframe.SelectedItem as Interval;
            var decimals = int.Parse(tbxDecimals.Text);

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
            prgbBin2Txt.IsIndeterminate = true;

            // прогрессор, будет апдейтить прогрессбар и строку статуса
            var progress = new Progress<ProgressReport>(report =>
            {
                prgbBin2Txt.Value = (int)report.Percent;
                lblTickProcessStatus.Content = "Обработано: {0}   Затрачено: {1}".Put(report.ProcessedCount, report.TimeUsed);
            });

            // заводим асинхронно конвертацию, если будут ошибки, они хэндлятся штатным образом.
            try
            {
                await Common.BinToTxtBarsAsync(tickerInfo, timeframe, decimals, progress);
            }
            catch (Exception ex)
            {
                Trace.Fail(ex.ToString());
                lblTickProcessStatus.Content = "Error: {0}".Put(ex.Message);
                this.UpdateLayout();
            }

            // вернем назад бинды и прогресс бар
            btn.SetBinding(IsEnabledProperty, binding);
            prgbBin2Txt.Value = 0;
            prgbBin2Txt.IsIndeterminate = false;
            this.UpdateLayout();
        }

        private void cbxDataProviders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _tickers.Clear();
        }

        private void CbxTickers_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cbx = sender as System.Windows.Controls.ComboBox;

            _timeframes.Clear();

            if (cbx.SelectedItem == null) 
                return;

            (cbx.SelectedItem as BarsTickerInfo).TimeFrames.ForEach(_timeframes.Add);
        }

        private void btnRefreshDataProviders_Click(object sender, RoutedEventArgs e)
        {
            _dataProviders.Clear();
            Common.GetDataProviders().ForEach(_dataProviders.Add);
        }



        private void btnBrowseTxt_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = false,
                CheckPathExists = true,
                InitialDirectory = Common.GetTsLabAppDataPath()
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                tbxTxtPath.Text = dialog.FileName;
        }

        private async void btnSaveTxt2Bin_Click(object sender, RoutedEventArgs e)
        {
            // Выключим кнопку записи в файл чтобы сто раз не жмакали
            // так как у нас включен биндинг, нужно его засейвить  потом ресторить.
            var btn = ((Button)sender);
            BindingBase binding = null;
            if (BindingOperations.IsDataBound(btn, IsEnabledProperty))
            {
                binding = BindingOperations.GetBinding(btn, IsEnabledProperty) ??
                          (BindingBase)BindingOperations.GetMultiBinding(btn, IsEnabledProperty);
            }

            btn.IsEnabled = false;

            // Читаем ввод пользователя. Валидация ввода провдится еще в фазе ввода в форму.
            var binPrefix = tbxBinPrefix.Text;
            var filePath = tbxTxtPath.Text;
            var interval = (tbxInterval.Text + cbxIntervalBase.Text).ToInterval();

            // Инициализируем прогресс бар
            prgbTxt2Bin.IsIndeterminate = true;

            // прогрессор, будет апдейтить прогрессбар и строку статуса
            var progress = new Progress<ProgressReport>(report =>
            {
                prgbTxt2Bin.Value = (int)report.Percent;
                lblBarProcessStatus.Content = "Обработано: {0}   Затрачено: {1}".Put(report.ProcessedCount, report.TimeUsed);
            });

            // заводим асинхронно конвертацию, если будут ошибки, они хэндлятся штатным образом.
            try
            {
                await Common.TxtToBinBarsAsync(filePath, binPrefix, interval, progress);
            }
            catch (Exception ex)
            {
                Trace.Fail(ex.ToString());
                lblBarProcessStatus.Content = "Error: {0}".Put(ex.Message);
                this.UpdateLayout();
            }

            btn.SetBinding(IsEnabledProperty, binding);
            prgbTxt2Bin.Value = 0;
            prgbTxt2Bin.IsIndeterminate = false;

        }
    }
}
