using System;
using System.Collections.Generic;
using TSLab.DataSource;

namespace GuiBinConverter
{
    /// <summary>
    /// Вспомогательный класс, содержащий хелпер методы. В том числе и методы расширения.
    /// </summary>
    static class TradeHelper
    {

        #region Общие хелперы

        public static bool IsNull(this object obj)
        {
            return obj == null;
        }

        /// <summary>
        /// Метод упрощающий форматирование строк.
        /// </summary>
        /// <param name="str">Строка для форматирования.</param>
        /// <param name="args">Аргументы для форматирования.</param>
        /// <returns></returns>
        public static string Put(this string str, params object[] args)
        {
            return string.Format(str, args);
        }

        /// <summary>
        /// Сравнивает два значения double. Нужен для сравнения цен. Использует дельту 1Е-10
        /// Если разница между двумя значениями меньше дельты вернет истину.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool IsPriceEqual(this double d1, double d2)
        {
            return Math.Abs(d1 - d2) < 1E-10;
        }

        #endregion

        #region Хелперы времени/даты

        /// <summary>
        /// Возвращает истину если время лежит в заданных границах, ВКЛЮЧИТЕЛЬНО!
        /// </summary>
        /// <param name="time">Время</param>
        /// <param name="minTime">Минимальная граница</param>
        /// <param name="maxTime">Максимальная граница</param>
        /// <returns></returns>
        public static bool InRange(this TimeSpan time, TimeSpan minTime, TimeSpan maxTime)
        {
            return time >= minTime && time <= maxTime;
        }

        #endregion
    }

    public interface IRange<T>
    {
        T Start { get; }
        T End { get; }

        bool Includes(T value);
        bool Includes(IRange<T> range);
    }

    public class DateRange : IRange<DateTime>
    {
        /// <summary>
        /// Начало диапазона. Меньше либо равно концу.
        /// </summary>
        public DateTime Start { get; private set; }
        /// <summary>
        /// Конец диапазона. Больше либо равен началу.
        /// </summary>
        public DateTime End { get; private set; }


        /// <summary>
        /// Создает временной диапазон.
        /// </summary>
        /// <param name="start">Начало. Не больше чем конец, но может быть равным ему.</param>
        /// <param name="end">Конец. Не меньше чем начало, но может быть равным ему.</param>
        public DateRange(DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentOutOfRangeException("start", "Начало не может быть больше чем конец диапазона.");

            Start = start;
            End = end;
        }


        /// <summary>
        /// Проверка вхождения элемента в диапазон. Края включительно!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Includes(DateTime value)
        {
            return (Start <= value) && (value <= End);
        }

        /// <summary>
        /// Проверка вхождения диапазона в другой диапазон. Края включительно!
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool Includes(IRange<DateTime> range)
        {
            return (Start <= range.Start) && (range.End <= End);
        }
    }

    public class IntervalRange : IRange<Interval>
    {
        /// <summary>
        /// Начало диапазона. Меньше либо равно концу.
        /// </summary>
        public Interval Start { get; private set; }
        /// <summary>
        /// Конец диапазона. Больше либо равен началу.
        /// </summary>
        public Interval End { get; private set; }


        /// <summary>
        /// Создает временной диапазон.
        /// </summary>
        /// <param name="start">Начало. Не больше чем конец, но может быть равным ему.</param>
        /// <param name="end">Конец. Не меньше чем начало, но может быть равным ему.</param>
        public IntervalRange(Interval start, Interval end)
        {
            if (start > end)
                throw new ArgumentOutOfRangeException("start", "Начало не может быть больше чем конец диапазона.");

            Start = start;
            End = end;
        }


        /// <summary>
        /// Проверка вхождения элемента в диапазон. Края включительно!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Includes(Interval value)
        {
            return (Start == value || Start < value) && (value < End || value == End);
        }

        /// <summary>
        /// Проверка вхождения диапазона в другой диапазон. Края включительно!
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool Includes(IRange<Interval> range)
        {
            return (Start < range.Start || Start == range.Start) && (range.End < End || range.End < End);
        }
    }
}
