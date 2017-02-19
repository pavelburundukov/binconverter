using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSLab.DataSource;

namespace GuiBinConverter
{
    /// <summary>
    /// Сравнивалка сделок на равенство.
    /// </summary>
    class TradeInfoComparer : IEqualityComparer<TradeInfo>
    {
        public bool Equals(TradeInfo x, TradeInfo y)
        {
            // проверка не совершенно полная, но достаточная
            return x.TradeNo == y.TradeNo
                   && x.Price == y.Price
                   && x.Date == y.Date
                   && x.Direction == y.Direction
                   && x.Quantity == y.Quantity
                   && x.OpenInterest == y.OpenInterest;
        }

        public int GetHashCode(TradeInfo trade)
        {
            return (int)trade.TradeNo;
        }
    }
}
