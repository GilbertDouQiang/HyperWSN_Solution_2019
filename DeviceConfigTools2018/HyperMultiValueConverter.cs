using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Data;
using System.Windows;

namespace DeviceConfigTools2018
{
    public class HyperMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values==null || values.Length<1)
            {
                return null;
            }
            if (values[0]== DependencyProperty.UnsetValue || values[1]== DependencyProperty.UnsetValue)
            {
                return null;
            }
            return String.Format("{0}+{1}", values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            string[] splitv = ((string)value).Split('-');
            return splitv;
        }
    }
}
