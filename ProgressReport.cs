using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuiBinConverter
{
    /// <summary>
    /// Класс для отчета об исполнении асинхронного метода
    /// </summary>
    struct ProgressReport
    {
        public double Percent { get; set; }
        public DateTime Date { get; set; }
        public string FileName { get; set; }
        public int ProcessedCount { get; set; }
        public TimeSpan TimeUsed { get; set; }
        public bool Finished { get; set; }
    }
}
