using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace GuiBinConverter
{
    // Конвертеры и прочая муть

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(object), typeof(bool))]
    public class ObjectBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }


    [ValueConversion(typeof(string), typeof(bool))]
    public class FileExistToBool : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a bool");

            var path = (string)value;
            return File.Exists(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(string), typeof(bool))]
    public class StrLengthToBool : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a bool");

            return ((string)value).Length > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(object), typeof(DateTime?))]
    public class TickerToNullableMinDateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(DateTime?))
                throw new InvalidOperationException("The target must be a datetime?.");

            if ((value is TradesTickerInfo) == false)
                return DateTime.MinValue;

            return (value as TradesTickerInfo).MinDate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }


    public class MultyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)values[0] == true && (string)values[2] != "" || (bool)values[0] == false && (int)values[1] != -1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MultyConverter2 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string)values[0];
            var prefix = (string)values[1];
            //var versionStr = (string)values[2];

            var ok = File.Exists(path);
            if (!ok)
                return false;

            ok = prefix.Length > 0;
            if (!ok)
                return false;

            //int value;
            //ok = int.TryParse(versionStr, out value);
            //if (!ok || value <= 0)
            //    return false;

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MultyConverter3 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string)values[0];
            var prefix = (string)values[1];
            //var versionStr = (string)values[2];
            //var iBase = values[3] == null ? "" : (string)values[3];
            //var interval = (string)values[4];
            var iBase = values[2] == null ? "" : (string)values[2];
            var interval = (string)values[3];

            var ok = File.Exists(path);
            if (!ok)
                return false;

            ok = prefix.Length > 0;
            if (!ok)
                return false;

            int value;
            //ok = int.TryParse(versionStr, out value);
            //if (!ok || value <= 0)
            //    return false;

            ok = iBase.Length > 0;
            if (!ok)
                return false;

            ok = int.TryParse(interval, out value);
            if (!ok || value <= 0)
                return false;

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TfDecMultyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var timeFrame = values[0];
            int decimals = 0;
            var ok = int.TryParse((string) values[1], out decimals);
            return timeFrame != null && ok && decimals >= 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
}
