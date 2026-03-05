using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DS4WinWPF.DS4Forms.Converters
{
	internal class HexToIntConverter : IValueConverter
	{
		// Convert int -> hex string
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int intValue)
				return intValue.ToString("X"); // uppercase hex (no "0x")
			return "0";
		}

		// Convert hex string -> int
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string s && int.TryParse(s, NumberStyles.HexNumber, culture, out int result))
				return result;

			// Optional: fallback to 0 or throw an exception
			return 0;
		}
	}
}
