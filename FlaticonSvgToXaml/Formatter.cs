using System;
using System.Globalization;

namespace FlaticonSvgToXaml
{
    public class GeometryFormatter : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            // Check whether this is an appropriate callback             
            if (!this.Equals(formatProvider))
                return null;

            // Set default format specifier             
            if (string.IsNullOrEmpty(format))
                format = "F6";

            if (arg is System.Windows.Media.PathFigure)
            {
                return (arg as System.Windows.Media.PathFigure).ToString(this);
            }
            else if (arg is System.Windows.Point)
            {
                return string.Format(this, "{0} {1}", ((System.Windows.Point)arg).X, ((System.Windows.Point)arg).Y);
            }
            else if (arg is double)
            {
                return ((double)arg).ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (arg is char)
            {
                if (((char)arg) == ';') return " ";
            }

            return arg.ToString();
        }
    }
}
