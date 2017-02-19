using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TSLab.DataSource;
using TSLab.Utils;

namespace GuiBinConverter
{
    static class Common
    {
        // шаблоны времени при конвертации
        public static string TxtDateFormat = @"yyyyMMdd";
        public static string TxtTimeFormat = @"HHmmss";
        public static string TxtMSecFormat = @"fff";

        // шаблон цены при конвертации в текст
        public static string TxtFinamDecimalsFormat = @"0.000000";

        //  сокращеные финамовские шаблоны
        public static string TickTxtHeaderFinam = @"<DATE>,<TIME>,<LAST>,<VOL>";
        public static string TickTxtTemplateFinam = "{0:#DATE},{1:#TIME},{2},{3}"
                                                        .Replace("#DATE", TxtDateFormat)
                                                        .Replace("#TIME", TxtTimeFormat);

        public static string BarTxtHeaderFiman = @"<DATE>,<TIME>,<OPEN>,<HIGH>,<LOW>,<CLOSE>,<VOL>";
        public static string BarTxtTemplateFiman = "{0:#DATE},{1:#TIME},{2},{3},{4},{5},{6}"
                                                        .Replace("#DATE", TxtDateFormat)
                                                        .Replace("#TIME", TxtTimeFormat);

        // полные шаблоны
        public static string TickTxtHeaderFull = @"<DATE>,<TIME>,<MSEC>,<TRADENO>,<LAST>,<VOL>,<DIRECTION>,<ASK>,<ASKQTY>,<BID>,<BIDQTY>,<INTEREST>,<STEPPRICE>";
        public static string TickTxtTemplateFull = "{0:#DATE},{1:#TIME},{2:#MSEC},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}"
                                                        .Replace("#DATE", TxtDateFormat)
                                                        .Replace("#TIME", TxtTimeFormat)
                                                        .Replace("#MSEC", TxtMSecFormat);

        public static string BarTxtHeaderFull = @"<DATE>,<TIME>,<OPEN>,<HIGH>,<LOW>,<CLOSE>,<VOL>,<INTEREST>";
        public static string BarTxtTemplateFull = "{0:#DATE},{1:#TIME},{2},{3},{4},{5},{6},{7}"
                                                        .Replace("#DATE", TxtDateFormat)
                                                        .Replace("#TIME", TxtTimeFormat);


        // формат даты используемый в именовании бинарных файлов тикового кэша
        public static string TickBinFileNameDateFormat = @"MM.dd.yyyy";



        /// <summary>
        /// Путь в профиль пользователя где лежат данные программы ТСЛаб.
        /// </summary>
        public static string GetTsLabAppDataPath()
        {
            // собираем путь к папке данных лаба
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var tslabDataPath = Path.Combine(localDataPath, "TSLab", "TSLab12");

            return tslabDataPath;
        }

        /// <summary>
        /// Вытаскиваем все стандартные дата провайдеры тслаба.
        /// Находим их по папкам с кэшэм и барами
        /// </summary>
        public static List<DataProvInfo> GetDataProviders()
        {
            // список паттернов папок которые надо зачищать.
            const string cacheBarsRx = @"(^.+?)cache$";
            const string cacheDataRx = @"(^.+?)CacheData$";
            const string cacheTradesRx = @"(^.+?)CacheTrades$";
            var rxList = new string[] { cacheBarsRx, cacheDataRx, cacheTradesRx };

            var dataProviders = new List<DataProvInfo>();

            // собираем путь к папке данных лаба
            var tslabDataPath = GetTsLabAppDataPath();

            // Проверим ввода папки
            if (!Directory.Exists(tslabDataPath))
            {
                MessageBox.Show("Папки с данными TSLab не обнаружено.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return dataProviders;
            }


            // запросим все подпапки для текущей папки и отработаем их и их содержимое.
            foreach (var dir in Directory.GetDirectories(tslabDataPath))
            {
                // удаляем завершающие символы и забираем  имя папки
                var cleanDir = dir.TrimEnd(new char[] { '\\', ' ' });
                var dirName = Path.GetFileName(cleanDir);

                // будем  перебирать все варианты папок для провайдера. Не все всегда существуют, иногда некоторых нет.
                foreach (var rx in rxList)
                {
                    var match = Regex.Match(dirName, rx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    if (!match.Success)
                        continue;

                    // в группе 1 у нас имя провайдера, если группа не сматчилась значит чето пошло не так.
                    if (match.Groups[1].Success == false)
                    {
                        MessageBox.Show("Не получилось извлечь имя провайдера из {0}.".Put(dirName), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return dataProviders;
                    }

                    // если такой дата провайдер еще не найден, добавляем его в список
                    var dataProvName = match.Groups[1].Value;
                    var tickPath = Path.Combine(tslabDataPath, dataProvName + "CacheTrades");
                    var barPath = Path.Combine(tslabDataPath, dataProvName + "Cache");

                    var ok = dataProviders.Find(info => info.Name == dataProvName);
                    if (ok != null)
                        continue;

                    dataProviders.Add(new DataProvInfo(dataProvName, tickPath, barPath));
                }
            }

            return dataProviders;
        }

        public static List<TradesTickerInfo> GetTicksList(DataProvInfo provInfo)
        {
            return GetTicksList(provInfo.TickPath);
        }

        /// <summary>
        /// По заданному пути читаем все бинарники и возвращает список тикеров  сортированный по имени.
        /// </summary>
        /// <param name="tickPath"></param>
        /// <returns></returns>
        public static List<TradesTickerInfo> GetTicksList(string tickPath)
        {
            const string tickRx = @"^(.+?)\.(\d{2}\.\d{2}\.\d{4})\.bin$";       // групапа 1 это имя, группа 2 это дата

            var tickerInfos = new List<TradesTickerInfo>();

            var files = Directory.GetFiles(tickPath, "*.bin");

            foreach (var path in files)
            {
                // Читаем версию бинарника
                var binVer = BinaryCache<TradeInfo>.ReadVersion(path);

                // Получаем тикер из имени файла, и если тикер новый, добавляем имя тикера в общий список
                // Проверяем версии бинарников, если в двух версиях, то указываем обе версии.
                var name = Path.GetFileName(path) ?? "";
                var match = Regex.Match(name, tickRx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                if (!match.Success)
                    continue;

                #region Проверка наличия группы 1 и 2 в результатах
                // в группе 1 у нас имя тикера, если группа не сматчилась значит чето пошло не так.
                if (match.Groups[1].Success == false)
                {
                    MessageBox.Show("Не получилось извлечь имя провайдера из {0}.".Put(name), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return tickerInfos;
                }

                // в группе 2 у нас дата, если группа не сматчилась значит чето пошло не так.
                if (match.Groups[2].Success == false)
                {
                    MessageBox.Show("Не получилось извлечь дату из {0}.".Put(name), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return tickerInfos;
                } 
                #endregion

                // Ищем в списке тикеров только что прочитанные, если он уже есть, 
                // и версия бинарника уже такая есть, то переходим к след файлу.
                var ticker = match.Groups[1].Value;
                var date = DateTime.ParseExact(match.Groups[2].Value, TickBinFileNameDateFormat, CultureInfo.InvariantCulture);

                // если тикер есть и такая версия файла тоже есть
                var et = tickerInfos.Find(info => info.Ticker == ticker);
                if (et != null && et.Versions.Contains(binVer) && et.Dates.Contains(date))
                    continue;

                // Если тикер уже есть но дата прочитана новая.
                if (et != null && et.Dates.Contains(date) == false)
                    et.Dates.Add(date);

                // Если тикер уже есть но версия файла прочитана новая.
                if (et != null && et.Versions.Contains(binVer) == false)
                    et.Versions.Add(binVer);

                // Если тикера еще нет, добавим новый тикер в список.
                if (et == null)
                    tickerInfos.Add(new TradesTickerInfo(ticker, new DateTime[] {date}, tickPath, new List<int>() { binVer }));
            }

            tickerInfos.Sort((info1, info2) => string.Compare(info1.Ticker, info2.Ticker, StringComparison.InvariantCultureIgnoreCase));
            return tickerInfos;
        }


        #region Работа с тиками

        /// <summary>
        /// Пишет весь тикер в текстовый файл  с учетом временных ограничений. 
        /// Для каждого бин файла вызывается <paramref name="progress"/>
        /// </summary>
        /// <param name="tickerInfo"></param>
        /// <param name="range">Временной диапазон, за который конвертить данные. Если задать null то все данные брать.</param>
        /// <param name="progress">прогрессор. Для отчетности о выполнении работы</param>
        public async static Task BinToTxtTradesAsync(TradesTickerInfo tickerInfo, DateRange range, IProgress<ProgressReport> progress)
        {
            var timer = new Stopwatch();
            timer.Start();

            var totalProcessed = await Task.Factory.StartNew<int>(() =>
            {
                #region Тело метода
                var trdName = "{0}.trd".Put(tickerInfo.Ticker);
                var trdPath = Path.Combine(tickerInfo.BinPath, trdName);
                var streamW = new StreamWriter(trdPath, false);

                streamW.WriteLine(TickTxtHeaderFull);

                // получим список путей до бинарников с учетом ограничений по дате
                var matсhingDates = range == null
                                        ? tickerInfo.Dates
                                        : tickerInfo.Dates.Where(range.Includes);

                var pathList = matсhingDates.Select(date =>
                {
                    var fileName = "{0}.{1}.bin".Put(tickerInfo.Ticker, date.ToString(TickBinFileNameDateFormat));
                    return Path.Combine(tickerInfo.BinPath, fileName);
                }).ToArray();

                // Для каждого бинарного файла тикер, производим процедуру записи в ТХТ файл.
                var cache = new BinaryCache<TradeInfo>();
                var filesProcessed = 0;
                foreach (var path in pathList)
                {
                    var version = BinaryCache<TradeInfo>.ReadVersion(path);
                    if (version == 0)
                        throw new Exception("Не удалось получить версию формата файла.");

                    // пишем в текст
                    var trades = cache.LoadCached(path, version);
                    trades.ForEach(t => streamW.WriteLine(t.ToFullString()));

                    // отчитались
                    filesProcessed++;
                    progress.Report(new ProgressReport()
                    {
                        Percent = (double)filesProcessed * 100 / pathList.Length,
                        TimeUsed = timer.Elapsed,
                        ProcessedCount = filesProcessed
                    });
                }

                // Скидываем на диск и закрываем потоки.
                streamW.Flush();
                streamW.Close();

                return pathList.Length;

                #endregion
            });

            timer.Stop();
            progress.Report(new ProgressReport() { Finished = true, Percent = 100, TimeUsed = timer.Elapsed, ProcessedCount = totalProcessed });
        }

        public async static Task TxtToBinTradesAsync(string filePath, string binPrefix, IProgress<ProgressReport> progress)
        {
            var timer = new Stopwatch();
            timer.Start();

            var totalProcessed = await Task.Factory.StartNew<int>(() =>
            {
                #region Тело метода

                var cache = new BinaryCache<TradeInfo>();
                var sr = new StreamReader(filePath);
                var dirPath = Path.GetDirectoryName(filePath);

                // будет сейвить данные на диск. убрали дублирование кода ниже
                Action<IList<TradeInfo>, DateTime> saveAndClear = (tradeList, dateTime) =>
                {
                    var path = Path.Combine(dirPath, @"{0}.{1}.bin".Put(binPrefix, dateTime.ToString(TickBinFileNameDateFormat)));

                    // устраняем дубли, сортируем по номеру сделок
                    var sorted = tradeList.Distinct(new TradeInfoComparer()).OrderBy(t => t.TradeNo).ToArray();
                    cache.SaveCached(path, sorted);

                    tradeList.Clear();
                };


                // Пропустим первую строчку ибо в ней заголовки
                sr.ReadLine();

                var trades = new List<TradeInfo>();
                var currDate = DateTime.MinValue;
                var processedCount = 0;
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    var tradeInfo = new TradeInfo();
                    tradeInfo.FromFullString(line);

                    if (currDate == DateTime.MinValue)
                        currDate = tradeInfo.Date.Date;

                    // Если взяли сделку со следующего дня, пишем то что есть в файл.
                    if (tradeInfo.Date.Date > currDate)
                    {
                        saveAndClear(trades, currDate);

                        // отчитались
                        processedCount++;
                        progress.Report(new ProgressReport() { ProcessedCount = processedCount, TimeUsed = timer.Elapsed, Date = currDate });

                        //
                        currDate = tradeInfo.Date.Date;
                    }

                    trades.Add(tradeInfo);
                }

                // После завершения работы пишем в файл те сделки которые еще не записаны
                if (trades.Count > 0)
                {
                    saveAndClear(trades, currDate);

                    // отчитались
                    processedCount++;
                    progress.Report(new ProgressReport() { ProcessedCount = processedCount, TimeUsed = timer.Elapsed, Date = currDate });
                }

                sr.Close();
                return processedCount;

                #endregion
            });

            timer.Stop();
            progress.Report(new ProgressReport() { Finished = true, Percent = 100, TimeUsed = timer.Elapsed, ProcessedCount = totalProcessed });


        }

        /// <summary>
        /// Перебирает все бин файлы с тиками и заменяет файлы где нашлись дубли или несортировано на новые версии.
        /// Старые версии сохраняет поставив перед именем префикс "__"
        /// </summary>
        /// <param name="tickerInfo"></param>
        /// <param name="range"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async static Task BinDeDupTradesAdync(TradesTickerInfo tickerInfo, DateRange range, IProgress<ProgressReport> progress)
        {
            var timer = new Stopwatch();
            timer.Start();

            var totalReplaced = await Task.Factory.StartNew<int>(() =>
            {
                #region Тело метода

                // получим список путей до бинарников с учетом ограничений по дате
                var matсhingDates = range == null
                                        ? tickerInfo.Dates
                                        : tickerInfo.Dates.Where(range.Includes);

                var pathList = matсhingDates.Select(date =>
                {
                    var fileName = "{0}.{1}.bin".Put(tickerInfo.Ticker, date.ToString(TickBinFileNameDateFormat));
                    return Path.Combine(tickerInfo.BinPath, fileName);
                }).ToArray();

                // Для каждого бинарного файла тикер, производим проверку и перезапись если нужно сортировать
                var cache = new BinaryCache<TradeInfo>();
                var filesProcessed = 0;
                var filesReplaced = 0;
                foreach (var path in pathList)
                {
                    var version = BinaryCache<TradeInfo>.ReadVersion(path);
                    if (version == 0)
                        throw new Exception("Не удалось получить версию формата файла.");

                    // загружаем тики из бинарного кэша
                    var trades = cache.LoadCached(path, version);

                    // удалим дубли, и если увидим что число сделок упало, значит надо переписать исходный файл
                    var origCount = trades.Count;
                    var sorted = trades.Distinct(new TradeInfoComparer()).OrderBy(t => t.TradeNo).ToList();

                    if (sorted.Count != origCount)
                    {
                        var bakFileName = "__" + Path.GetFileName(path);
                        var bakDirName = Path.GetDirectoryName(path);
                        var bakPath = Path.Combine(bakDirName, bakFileName);
                        File.Move(path, bakPath);

                        cache.SaveCached(path, sorted);
                        filesReplaced++;
                    }

                    // отчитались
                    filesProcessed++;
                    progress.Report(new ProgressReport()
                    {
                        Percent = (double)filesProcessed * 100 / pathList.Length,
                        TimeUsed = timer.Elapsed,
                        ProcessedCount = filesProcessed
                    });
                }

                return filesReplaced;

                #endregion
            });

            timer.Stop();
            progress.Report(new ProgressReport() { Finished = true, Percent = 100, TimeUsed = timer.Elapsed, ProcessedCount = totalReplaced });
        }

        #endregion

        #region Работа с барами

        public async static Task BinToTxtBarsAsync(BarsTickerInfo tickerInfo, Interval timeframe, int decimals, IProgress<ProgressReport> progress)
        {
            var timer = new Stopwatch();
            timer.Start();

            var totalProcessed = await Task.Factory.StartNew<int>(() =>
            {
                #region Тело метода

                var trdName = "{0}.{1}.bar".Put(tickerInfo.Ticker, timeframe);
                var trdPath = Path.Combine(tickerInfo.BinPath, trdName);
                var streamW = new StreamWriter(trdPath, false);

                streamW.WriteLine(BarTxtHeaderFull);

                // получим список путей до бинарников с учетом ограничений по дате
                var mathingItems = tickerInfo.TimeFrames.Where(tf => tf == timeframe);

                var pathList = mathingItems.Select(item =>
                {
                    var fileName = "{0}.{1}.bin".Put(tickerInfo.Ticker, item.ToString());
                    return Path.Combine(tickerInfo.BinPath, fileName);
                }).ToList();

                // Для каждого бинарного файла тикер, производим процедуру записи в ТХТ файл.
                var cache = new BinaryCache<DataBar>();
                var filesProcessed = 0;
                foreach (var path in pathList)
                {
                    var version = BinaryCache<DataBar>.ReadVersion(path);
                    if (version == 0)
                        throw new Exception("Не удалось получить версию формата файла.");

                    var items = cache.LoadCached(path, version);
                    items.ForEach(databar => streamW.WriteLine(databar.ToFullString(decimals)));

                    // отчитались
                    filesProcessed++;
                    progress.Report(new ProgressReport()
                    {
                        Percent = (double)filesProcessed * 100 / pathList.Count,
                        TimeUsed = timer.Elapsed,
                        ProcessedCount = filesProcessed
                    });
                }

                // Скидываем на диск и закрываем потоки.
                streamW.Flush();
                streamW.Close();

                return filesProcessed;

                #endregion
            });

            timer.Stop();
            progress.Report(new ProgressReport() { Finished = true, Percent = 100, TimeUsed = timer.Elapsed, ProcessedCount = totalProcessed });
        }

        public async static Task TxtToBinBarsAsync(string filePath, string binPrefix, Interval interval, IProgress<ProgressReport> progress)
        {
            var timer = new Stopwatch();
            timer.Start();

            var totalProcessed = await Task.Factory.StartNew<int>(() =>
            {
                #region Тело метода

                var cache = new BinaryCache<DataBar>();
                var sr = new StreamReader(filePath);
                var dirPath = Path.GetDirectoryName(filePath);

                var items = new List<DataBar>();

                // Пропустим первую строчку ибо в ней заголовки
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var dataBar = FromFullString(line);
                    items.Add(dataBar);
                }

                // После завершения работы пишем в файл те сделки которые еще не записаны
                if (items.Count > 0)
                {
                    var path = Path.Combine(dirPath, @"{0}.{1}.bin".Put(binPrefix, interval.ToString()));
                    cache.SaveCached(path, items);

                    items.Clear();
                }

                sr.Close();

                return 1;

                #endregion
            });

            timer.Stop();
            progress.Report(new ProgressReport() { Finished = true, Percent = 100, TimeUsed = timer.Elapsed, ProcessedCount = totalProcessed });
        } 
        
        #endregion


        public static string ToFullString(this TradeInfo tradeInfo)
        {
            var ic = CultureInfo.InvariantCulture;
            return TickTxtTemplateFull.Put(tradeInfo.Date, tradeInfo.Date, tradeInfo.Date,
                                           tradeInfo.TradeNo, tradeInfo.Price.ToString(ic), tradeInfo.Quantity.ToString(ic),
                                           tradeInfo.Direction,
                                           tradeInfo.Ask.ToString(ic), tradeInfo.AskQty.ToString(ic),
                                           tradeInfo.Bid.ToString(ic), tradeInfo.BidQty.ToString(ic),
                                           tradeInfo.OpenInterest.ToString(ic),
                                           tradeInfo.StepPrice.ToString(ic));
        }

        public static void FromFullString(this TradeInfo trade, string tradeInfoStr)
        {
            var ic = CultureInfo.InvariantCulture;

            var line = tradeInfoStr;
            var parts = line.Split(new char[] { ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

            // @"<DATE>,<TIME>,<MSEC>,<TRADENO>,<LAST>,<VOL>,<DIRECTION>,<ASK>,<ASKQTY>,<BID>,<BIDQTY>,<INTEREST>,<STEPPRICE>";
            //var trade = new TradeInfo();

            trade.Date = DateTime.ParseExact(parts[0] + parts[1] + parts[2],
                                             TxtDateFormat + TxtTimeFormat + TxtMSecFormat,
                                             ic);
            trade.TradeNo = long.Parse(parts[3], ic);
            trade.Price = float.Parse(parts[4], ic);
            trade.Quantity = float.Parse(parts[5], ic);
            TradeDirection direction;
            var ok = Enum.TryParse(parts[6], true, out direction);
            trade.Direction = ok ? direction : TradeDirection.Unknown;
            trade.Ask = float.Parse(parts[7], ic);
            trade.AskQty = float.Parse(parts[8], ic);
            trade.Bid = float.Parse(parts[9], ic);
            trade.BidQty = float.Parse(parts[10], ic);
            trade.OpenInterest = float.Parse(parts[11], ic);
            trade.StepPrice = float.Parse(parts[12], ic);
        }


        public static string ToFullString(this DataBar dataBar, int decimals)
        {
            if (decimals < 0)
                throw new ArgumentOutOfRangeException("decimals", "Не может быть меньше 0");

            // @"<DATE>,<TIME>,<OPEN>,<HIGH>,<LOW>,<CLOSE>,<VOL>,<INTEREST>";
            var ic = CultureInfo.InvariantCulture;
            return BarTxtTemplateFull.Put(dataBar.Date, dataBar.Date,
                                            Math.Round(dataBar.Open, decimals).ToString(TxtFinamDecimalsFormat, ic),
                                            Math.Round(dataBar.High, decimals).ToString(TxtFinamDecimalsFormat, ic),
                                            Math.Round(dataBar.Low, decimals).ToString(TxtFinamDecimalsFormat, ic),
                                            Math.Round(dataBar.Close, decimals).ToString(TxtFinamDecimalsFormat, ic),
                                            Math.Round(dataBar.Volume, decimals).ToString(TxtFinamDecimalsFormat, ic),
                                            Math.Round(dataBar.Interest, decimals).ToString(TxtFinamDecimalsFormat, ic));
        }

        public static DataBar FromFullString(string dataStr)
        {
            var ic = CultureInfo.InvariantCulture;

            var line = dataStr;
            var parts = line.Split(new char[] { ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            
            // @"<DATE>,<TIME>,<OPEN>,<HIGH>,<LOW>,<CLOSE>,<VOL>,<INTEREST>";
            var date = DateTime.ParseExact(  parts[0] + parts[1],
                                             TxtDateFormat + TxtTimeFormat,
                                             ic);
            var open = double.Parse(parts[2], ic);
            var high = double.Parse(parts[3], ic);
            var low = double.Parse(parts[4], ic);
            var close = double.Parse(parts[5], ic);

            var vol = double.Parse(parts[6], ic);
            var interest = double.Parse(parts[7], ic);

            return new DataBar(date, open, high, low, close, vol, interest);
        }

        private static void SafeInvoke(this Action action)
        {
            if (action != null)
                action();
        }



        public static List<BarsTickerInfo> GetBarsList(string dirPath)
        {
            const string rx = @"^(.+?)\.(\d+?[tsmhd])\.bin$";       // групапа 1 это имя, группа 2 это интервал 3 это база интервала

            var tickerInfos = new List<BarsTickerInfo>();

            var files = Directory.GetFiles(dirPath , "*.bin");

            foreach (var path in files)
            {
                // Читаем версию бинарника
                var binVer = BinaryCache<DataBar>.ReadVersion(path);

                // Получаем тикер из имени файла, и если тикер новый, добавляем имя тикера в общий список
                // Проверяем версии бинарников, если в двух версиях, то указываем обе версии.
                var name = Path.GetFileName(path) ?? "";
                var match = Regex.Match(name, rx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                if (!match.Success)
                    continue;

                #region Проверка наличия группы 1 и 2 в результатах
                // в группе 1 у нас имя тикера, если группа не сматчилась значит чето пошло не так.
                if (match.Groups[1].Success == false)
                {
                    MessageBox.Show("Не получилось извлечь имя провайдера из {0}.".Put(name), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return tickerInfos;
                }

                // в группе 2 у нас таймфрейм, если группа не сматчилась значит чето пошло не так.
                if (match.Groups[2].Success == false)
                {
                    MessageBox.Show("Не получилось извлечь интервал из {0}.".Put(name), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return tickerInfos;
                }

                #endregion

                // Ищем в списке тикеров только что прочитанные, если он уже есть, 
                // и версия бинарника уже такая есть, то переходим к след файлу.
                var ticker = match.Groups[1].Value;
                var intervalStr = match.Groups[2].Value;
                var timeframe = intervalStr.ToInterval();
                
                // если тикер есть и такая версия файла тоже есть
                var et = tickerInfos.Find(info => info.Ticker == ticker);
                if (et != null && et.Versions.Contains(binVer) && et.TimeFrames.Contains(timeframe))
                    continue;

                // Если тикер уже есть но дата прочитана новая.
                if (et != null && et.TimeFrames.Contains(timeframe) == false)
                    et.TimeFrames.Add(timeframe);

                // Если тикер уже есть но версия файла прочитана новая.
                if (et != null && et.Versions.Contains(binVer) == false)
                    et.Versions.Add(binVer);

                // Если тикера еще нет, добавим новый тикер в список.
                if (et == null)
                    tickerInfos.Add(new BarsTickerInfo(ticker, new Interval[] { timeframe }, dirPath, new List<int>() { binVer }));
            }

            tickerInfos.Sort((info1, info2) => string.Compare(info1.Ticker, info2.Ticker, StringComparison.InvariantCultureIgnoreCase));
            return tickerInfos;
        }

        /// <summary>
        /// Пробуем конвертить строку в Interval тслабовский. Если неудачно то null
        /// </summary>
        /// <param name="intervalStr">Строка типо 4m, 1s, 3D. Регистр не важен. Никаких пробелов и прочих разделителей.</param>
        /// <returns>null если все прошло неудачно</returns>
        public static Interval ToInterval(this string intervalStr)
        {
            // на входе строки без пробелов и разделителей 4d, 15m и т.д
            // целое число, потом s,m,h,d и все
            const string rx = @"^(\d+?)([smhd])$";

            var match = Regex.Match(intervalStr, rx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            var iBase = match.Groups[2].Value.ToLowerInvariant();
            var interval = int.Parse(match.Groups[1].Value);

            Interval timeframe = null;
            switch (iBase)
            {
                case "t":
                    return new Interval(interval, DataIntervals.TICK);

                case "s":
                    return new Interval(interval, DataIntervals.SECONDS);

                case "m":
                    return new Interval(interval, DataIntervals.MINUTE);

                case "d":
                    return new Interval(interval, DataIntervals.DAYS);
            }
            
            return null;
        }
    }
}
