using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSLab.DataSource;

namespace GuiBinConverter
{
    class BaseTickerInfo
    {
        /// <summary>
        /// Имя тикера
        /// </summary>
        public string Ticker { get; private set; }

        /// <summary>
        /// Путь до папки где лежат бины для данного тикера
        /// </summary>
        public string BinPath { get; private set; }

        /// <summary>
        /// список версий бинарных файлов. При обновлении тслаба версии могут быть разные.
        /// </summary>
        public List<int> Versions { get; private set; }


        public BaseTickerInfo(string ticker, string binPath, IEnumerable<int> versions)
        {
            Ticker = ticker;
            BinPath = binPath;
            Versions =  new List<int>(versions);
        }

        public override string ToString()
        {
            var str = this.Ticker;
            this.Versions.ForEach(v => str += " | v{0}".Put(v));
            return str;
        }
    }


    /// <summary>
    /// Информация об одном тикере для тиковых данных
    /// </summary>
    class TradesTickerInfo : BaseTickerInfo
    {
        /// <summary>
        /// список дат файлов которые для тикера есть.
        /// </summary>
        public List<DateTime> Dates { get; private set; }

        public DateTime MinDate 
        {
            get { return Dates.Min(); }
        }

        public DateTime MaxDate
        {
            get { return Dates.Max(); }
        }


        public TradesTickerInfo(string ticker, IEnumerable<DateTime> dates, string path, IEnumerable<int> versions) : base(ticker, path, versions)
        {
            Dates = new List<DateTime>(dates);
        }
    }

    /// <summary>
    /// Информация для одного тикера для баровых данных
    /// </summary>
    class BarsTickerInfo : BaseTickerInfo
    {
        /// <summary>
        /// список таймфреймов для которых есть бины баров данного тикера
        /// </summary>
        public List<Interval> TimeFrames { get; private set; }

        public BarsTickerInfo(string ticker, IEnumerable<Interval> timeframes, string path, IEnumerable<int> versions) : base(ticker, path, versions)
        {
            TimeFrames = new List<Interval>(timeframes);
        }
    }
}
