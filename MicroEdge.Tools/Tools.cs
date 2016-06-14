using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MicroEdge
{
	#region Enumerations

	public enum ColumnAlignment
	{
		Left = 0,
		Right = 1,
		Center = 2
	}

	#endregion Enumerations

	/// <summary>
	/// This holds tools that are not application specific and are not specific to any class. Class specific tools
	/// should be in the related class and application specific tools should be in the AppTools class.
	/// </summary>
	public static class Tools
	{
		#region Constants

		private const string UsPattern = "M{0}d{0}yyyy";
		private const string NonUsPattern = "d{0}M{0}yyyy";
		private const string UsPatternFormal = "MM{0}dd{0}yyyy";
		private const string NonUsPatternFormal = "dd{0}MM{0}yyyy";
		static readonly string[] MstrWords = { "baggage", "banister", "bargain", "benefit", "bermuda", "between", "beverage", "breakfast", "capture", "carefree", "carpenter", "character", "diagram", "difference", "emergency", "enterprise", "entrance", "estimate", "exercise", "franchise", "frequency", "hardship", "hardware", "harvest", "hatchback", "heatwave", "immediate", "inspect", "instance", "mistake", "nickname", "preserve", "prevent", "primary", "primitive", "privacy", "pudding", "purchase", "readiness", "reappear", "remember", "reserve", "residence", "respect", "shudder", "significant", "treatment", "understand", "veteran", "yesterday" };

		#endregion Constants

		#region Fields

		private static string _defaultBroswer;

	    private static readonly string[] ParsePatterns =
		{
			CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
			string.Format(UsPattern, @"/"),
			string.Format(UsPattern, CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator),
			string.Format(NonUsPattern, @"/"),
			string.Format(NonUsPattern, CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator),
			string.Format(UsPatternFormal, @"/"),
			string.Format(UsPatternFormal, CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator),
			string.Format(NonUsPatternFormal, @"/"),
			string.Format(NonUsPatternFormal, CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator)
		};

		#endregion Fields

		#region Constructor

		static Tools()
		{
			PrimaryCurrencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
			OtherCurrencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
		}

		#endregion

		#region Propeties

		public static string PrimaryCurrencySymbol
		{
			get;
			set;
		}

		public static string OtherCurrencySymbol
		{
			get;
			set;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Modifies a SQL query so it can be used as a sub-select, essentially
		/// simplifying it down to select a single field.
		/// </summary>
		/// <param name="query">Query to work from.</param>
		/// <param name="isDistinct">True if this should be a distinct sub-select.</param>
		/// <param name="selectFields"></param>
		/// <returns>The final query for use as a sub-select.</returns>
		public static string GetQueryAsSubselect(string query, bool isDistinct, params string[] selectFields)
		{
			// We want just the one select field, and no order-by,
			//	so just extract the from and where and add a select on the front.

			// Extract the from and where.
			string fromAndWhere = ExtractFromAndWhere(query);

			StringBuilder finalSubSelect = new StringBuilder("SELECT ");
			if (isDistinct)
			{
				finalSubSelect.Append(" DISTINCT ");
			}

			// Add the fields to select (with a space on the end, in cause we need a space before the FROM)
			string fieldListString = string.Join(", ", selectFields);
			finalSubSelect.Append(fieldListString).Append(" ");

			// Add on the from and where
			finalSubSelect.Append(fromAndWhere);

			return finalSubSelect.ToString();
		}

		/// <summary>
		/// Substring the FROM and WHERE clause out of the baseQuery
		/// </summary>
		/// <param name="baseQuery">The old query to Substring from</param>
		/// <returns>the Substring'ed whereandFrom </returns>
		public static string ExtractFromAndWhere(string baseQuery)
		{
			int fromIndex = baseQuery.IndexOf("FROM", StringComparison.InvariantCultureIgnoreCase);
			int orderByIndex = baseQuery.IndexOf("ORDER", StringComparison.InvariantCultureIgnoreCase);

			// Make sure to only use orderByIndex if it was found after FROM
			string baseWhereAndFrom;
			if (orderByIndex > fromIndex)
			{
				baseWhereAndFrom = baseQuery.Substring(fromIndex, orderByIndex - fromIndex);
			}
			else
			{
				baseWhereAndFrom = baseQuery.Substring(fromIndex, baseQuery.Length - fromIndex);
			}

			return baseWhereAndFrom;
		}

		/// <summary>
		/// Extracts the where clause.
		/// </summary>
		/// <param name="baseQuery">The base query.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <returns>The index of the beginning of the where clause</returns>
		public static int ExtractWhereClause(string baseQuery, out string whereClause)
		{
			whereClause = string.Empty;
			int whereIndex = baseQuery.IndexOf("WHERE", StringComparison.InvariantCultureIgnoreCase);

			if (whereIndex > 0)
			{
				whereClause = baseQuery.Substring(whereIndex);
			}

			return whereIndex;
		}

		/// <summary>
		/// Return true if the value of the two objects are equal. This will consider the values of the objects
		/// even if they are of different types. Eg. This will allow the comparison of any number type to any
		/// other number type.
		/// </summary>
		/// <param name="value1">
		/// Value to be compared to value2.
		/// </param>
		/// <param name="value2">
		/// Value to be compared to value1.
		/// </param>
		/// <returns>
		/// True if the two are equal.
		/// </returns>
		public new static bool Equals(object value1, object value2)
		{
			//First determine if either are null.
			if (value1 == null || value2 == null)
			{
				//If so, then they must both be null to be equal.
				if (value1 == null && value2 == null)
					return true;

				return false;
			}

			//If value1 is a numeric type, convert it to a decimal.
			Type value1Type = value1.GetType();

			if (value1Type.IsEnum)
				value1 = ToDecimal(value1);
			else
			{
				switch (value1Type.Name)
				{
					case "Int32":
					case "Int16":
					case "Int64":
					case "Decimal":
					case "Byte":
					case "SByte":
					case "UInt32":
					case "UInt16":
					case "UInt64":
						value1 = ToDecimal(value1);
						break;

					case "Double":
					case "Single":
						value1 = ToDouble(value1);
						break;
				}
			}

			//Do the same for value2.
			Type value2Type = value2.GetType();
			if (value2Type.IsEnum)
				value2 = ToDecimal(value2);
			else
			{
				switch (value2Type.Name)
				{
					case "Int32":
					case "Int16":
					case "Int64":
					case "Decimal":
					case "Byte":
					case "SByte":
					case "UInt32":
					case "UInt16":
					case "UInt64":
						value2 = ToDecimal(value2);
						break;

					case "Double":
					case "Single":
						value2 = ToDouble(value2);
						break;
				}
			}

			//If the values are of the same type, compare. If not, they are not equal.
			if (value1.GetType() == value2.GetType())
				return value1.Equals(value2);
			
			return false;
		}

		/// <summary>
		/// Opens a website in the default browser.
		/// </summary>
		/// <param name="url">The url of the website to go to.</param>
		public static void LaunchUrl(string url)
		{
			if (!string.IsNullOrEmpty(url))
			{
				IoTools.LaunchProcess(GetDefaultBrowser(), false, false, string.Format("\"{0}\"", url));
			}
		}

		/// <summary>
		/// Gets the path of the default browser for the current widows user.
		/// </summary>
		/// <returns>The string path to the browser application.</returns>
		private static string GetDefaultBrowser()
		{
			if (string.IsNullOrEmpty(_defaultBroswer))
			{
				string browser = string.Empty;
				Microsoft.Win32.RegistryKey key = null;
				try
				{
					key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

					//trim off quotes
					if (key != null)
					{
						browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
						if (!browser.EndsWith("exe"))
						{
							//get rid of everything after the ".exe"
							browser = browser.Substring(0, browser.LastIndexOf(".exe", StringComparison.Ordinal) + 4);
						}
					}
				}
				finally
				{
					if (key != null)
                        key.Close();
				}

				_defaultBroswer = browser;
			}

			return _defaultBroswer;
		}

		/// <summary>
		/// Takes in a plain text string, generates a hash-encrypted string and 
		/// then converts that to a string of hex values represented the characters 
		/// in the encrypted strign
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string HashValue(string value)
		{
			UnicodeEncoding ue = new UnicodeEncoding();
			Byte[] hashArray = (new MD5CryptoServiceProvider()).ComputeHash(ue.GetBytes(value));
			return StringToHex(BitConverter.ToString(hashArray));
		}

		/// <summary>
		/// Takes a string of hex numbers, converts each to decimal and then 
		/// retuns a string of the associated ascii characters
		/// </summary>
		/// <param name="hexString"></param>
		public static string HexToString(string hexString)
		{
			StringBuilder decimalString = new StringBuilder();
			for (int i = 0; i < hexString.Length; i += 2)
			{
				byte charCode = Convert.ToByte(hexString.Substring(i, 2), 16);
				decimalString.Append(Encoding.Default.GetString(new[] { charCode }));
			}

			return decimalString.ToString();
		}

		/// <summary>
		/// Takes a string of characters, converts each to it's ASCII numeric equivalent 
		/// and retuns a string of the hex version of those integers
		/// </summary>
		public static string StringToHex(string stringToHex)
		{
			StringBuilder hexString = new StringBuilder();
			for (int i = 0; i < stringToHex.Length; i++)
			{
				int ascValue = AsciiValue(stringToHex[i]);
				hexString.Append(ascValue.ToString("X2"));
			}

			return hexString.ToString();
		}

		/// <summary>
		/// Convert any value to a boolean without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a boolean.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a boolean.
		/// </param>
		/// <returns>
		/// true or false.
		/// </returns>
		public static bool ToBoolean(object o, bool defaultValue)
		{
			try
			{
				if (o == null || o == DBNull.Value)
				{
					return defaultValue;
				}

				string boolString = o.ToString();

				//First try the most common forms of True and False.
				if (boolString == "0" || boolString.Equals("false", StringComparison.OrdinalIgnoreCase) || boolString == "")
					return false;
				
				if (boolString == "1" || boolString == "-1" || boolString.Equals("true", StringComparison.OrdinalIgnoreCase))
					return true;

				//Try converting directly to boolean from object o.
				return Convert.ToBoolean(o);
			}
			catch
			{
				//If all else fails, return the default.
				return defaultValue;
			}
		}
		public static bool ToBoolean(object o)
		{
			return ToBoolean(o, false);
		}

		/// <summary>
		/// Convert any value to a nullable boolean without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a nullable boolean.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a nullable boolean.
		/// </param>
		/// <returns>
		/// null, true or false.
		/// </returns>
		public static bool? ToNullableBoolean(object o, bool? defaultValue)
		{
			try
			{
				//DBNull will also be converted to null.
				if (o == null || o == DBNull.Value)
					return null;

				string boolString = o.ToString();

				//First try the most common forms of True and False.
				if (boolString == "0" || boolString.Equals("false", StringComparison.OrdinalIgnoreCase) || boolString == "")
					return false;

				if (boolString == "1" || boolString == "-1" || boolString.Equals("true", StringComparison.OrdinalIgnoreCase))
					return true;

				//Try converting directly to boolean from object o.
				return Convert.ToBoolean(o);
			}
			catch
			{
				//If all else fails, return the default.
				return defaultValue;
			}
		}
		public static bool? ToNullableBoolean(object o)
		{
			return ToNullableBoolean(o, null);
		}

		/// <summary>
		/// Convert any value to a string without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a string.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value is null.
		/// </param>
		/// <returns>
		/// The value represented as a string. If null, the default value will be returned.
		/// </returns>
		public static string ToString(object o, string defaultValue)
		{
			try
			{
				if (o == null || o == DBNull.Value)
					return defaultValue;
				
				return o.ToString();
			}
			catch
			{
				//If all else fails, return the default.
				return defaultValue;
			}
		}
		public static string ToString(object o)
		{
			return ToString(o, "");
		}

		/// <summary>
		/// Convert any value to string without generating an error.
		/// Blocks empty strings, returning default instead.
		/// </summary>
		/// <param name="o">
		/// Value to convert to string.
		/// </param>
		/// <param name="defaultValue">
		/// The value returned if indicated value is null or empty
		/// </param>
		/// <returns>
		/// The value represented as a string. If null or empty, the default will be returned.
		/// </returns>
		public static string ToNonEmptyString(object o, string defaultValue)
		{
			try
			{
				if (o == null || string.IsNullOrEmpty(o.ToString()))
					return defaultValue;
				
				return o.ToString();
			}
			catch
			{
				//If all else fails, return the default.
				return defaultValue;
			}
		}
		public static string ToNonEmptyString(object o)
		{
			return ToNonEmptyString(o, "0");
		}

		/// <summary>
		/// Convert any value to an Int64 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an Int64.
		/// </param>
		/// <returns>
		/// The value as an Int64, or 0 if the value cannot be converted.
		/// </returns>
		public static Int64 ToInt64(object o)
		{
			return ToInt64(o, 0);
		}

		/// <summary>
		/// Convert any value to an Int64 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an Int64.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an Int64.
		/// </param>
		/// <returns>
		/// The value as an Int64, or the default if the value cannot be converted.
		/// </returns>
		public static Int64 ToInt64(object o, Int64 defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

			    if (!(o is string))
                    return Convert.ToInt64(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                long result;
                return long.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an Int64 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an Int64.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an Int64.
		/// </param>
		/// <returns>
		/// The value as an Int64, or the default if the value cannot be converted.
		/// </returns>
		public static long? ToInt64(object o, Int64? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
				{
					return defaultValue;
				}

			    if (!(o is string))
                    return Convert.ToInt64(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                long result;
                return long.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an Int64 without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an Int64.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an Int64.
		/// </param>
		/// <returns>
		/// The value as an Int64, or the default if the value cannot be converted.
		/// </returns>
		public static object ToInt64(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToInt64(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                long result;
                return long.TryParse((string) o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result)
					? result : defaultValue;
			}
			catch
			{
					//If conversion fails, return default.
					return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an int32 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int32.
		/// </param>
		/// <returns>
		/// The value as an int, or 0 if the value cannot be converted.
		/// </returns>
		public static int ToInt32(object o)
		{
			return ToInt32(o, 0);
		}


		/// <summary>
		/// Convert any value to an int32 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int32.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static int ToInt32(object o, int defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToInt32(o);

				int result;
				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				return int.TryParse((string) o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result)
						? result : defaultValue;
			}
			catch
			{
					//If conversion fails, return false.
					return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an int32 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int32.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static int? ToInt32(object o, int? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToInt32(o);

				int result;
				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				return int.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an int32 without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int32.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static object ToInt32(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

			    if (!(o is string))
                    return Convert.ToInt32(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                int result;
                return int.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
					//If conversion fails, return default.
					return defaultValue;
			}
		}

		/// <summary>
		/// ToShort - redirects to ToInt16
		/// </summary>
		/// <param name="o">The value to convert to an Int16</param>
		/// <returns>The value as a short (int16) datatype</returns>
		public static short ToShort(object o)
		{
			return ToInt16(o);
		}

		/// <summary>
		/// Convert any value to an int16 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int16.
		/// </param>
		/// <returns>
		/// The value as an int, or 0 if the value cannot be converted.
		/// </returns>
		public static short ToInt16(object o)
		{
			return ToInt16(o, 0);
		}


		/// <summary>
		/// Convert any value to an int16 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int16.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static short ToInt16(object o, short defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

			    if (!(o is string))
                    return Convert.ToInt16(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                short result;
                return short.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
					//If conversion fails, return false.
					return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an int16 without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int16.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static short? ToInt16(object o, short? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

			    if (!(o is string))
                    return Convert.ToInt16(o);

			    short result;
			    //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
			    //avoid using an exception to handle non-numeric strings, which hurts performance
			    return short.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to an int16 without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to an int16.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to an int.
		/// </param>
		/// <returns>
		/// The value as an int, or the default if the value cannot be converted.
		/// </returns>
		public static object ToInt16(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
						return defaultValue;

				if (!(o is string))
					return Convert.ToInt16(o);

				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				short result;
				return short.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a byte without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a byte.
		/// </param>
		/// <returns>
		/// The value as a byte, or 0 if the value cannot be converted.
		/// </returns>
		public static byte ToByte(object o)
		{
			return ToByte(o, 0);
		}


		/// <summary>
		/// Convert any value to a byte without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a byte.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a byte.
		/// </param>
		/// <returns>
		/// The value as a byte, or the default if the value cannot be converted.
		/// </returns>
		public static byte ToByte(object o, byte defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;
				
				if (!(o is string))
					return Convert.ToByte(o);

				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				byte result;
				return byte.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a byte without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a byte.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a byte.
		/// </param>
		/// <returns>
		/// The value as a byte, or the default if the value cannot be converted.
		/// </returns>
		public static byte? ToByte(object o, byte? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToByte(o);

				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				byte result;
				return byte.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return false.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a byte without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a byte.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a byte.
		/// </param>
		/// <returns>
		/// The value as a byte, or the default if the value cannot be converted.
		/// </returns>
		public static object ToByte(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToByte(o);

				//Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
				//avoid using an exception to handle non-numeric strings, which hurts performance
				byte result;
				return byte.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a decimal without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a decimal.
		/// </param>
		/// <returns>
		/// The value as a decimal, or 0 if the value cannot be converted.
		/// </returns>
		public static decimal ToDecimal(object o)
		{
			return ToDecimal(o, 0);
		}


		/// <summary>
		/// Convert any value to a decimal without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a decimal.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a decimal.
		/// </param>
		/// <returns>
		/// The value as a decimal, or the default if the value cannot be converted.
		/// </returns>
		public static decimal ToDecimal(object o, decimal defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
				{
					return defaultValue;
				}
	
				if (!(o is string)) 
					return Convert.ToDecimal(o);

				//If this is a string and not null, remove any dollar signs and 
				//convert surrounding parentheses to a minus sign.
				string value = o.ToString().Trim();
				value = value.Replace(PrimaryCurrencySymbol, "");
				value = value.Replace(OtherCurrencySymbol, "");
				if (string.IsNullOrEmpty(value))
					return defaultValue;

				if (value.Substring(0, 1) == "(" && value.Substring(value.Length - 1, 1) == ")")
				{
					value = "-" + value.Substring(1, value.Length - 2);
					if (string.IsNullOrEmpty(value)) 
						return defaultValue;
				}
				o = value;


				return Convert.ToDecimal(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a decimal without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a decimal.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a decimal.
		/// </param>
		/// <returns>
		/// The value as a decimal, or the default if the value cannot be converted.
		/// </returns>
		public static decimal? ToDecimal(object o, decimal? defaultValue)
		{
			try
			{
				//If this is a string, remove any dollar signs and convert surrounding parentheses to
				//a minus sign.
				if (o is string)
				{
					string value = o.ToString().Trim();
					value = value.Replace("$", "");
					if (!string.IsNullOrEmpty(value))
					{
						if (value.Substring(0, 1) == "(" && value.Substring(value.Length - 1, 1) == ")")
							value = "-" + value.Substring(1, value.Length - 2);
					}
					o = value;
				}

				if (o == null || o.ToString() == "")
					return defaultValue;
				
				return Convert.ToDecimal(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a decimal without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a decimal.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a decimal.
		/// </param>
		/// <returns>
		/// The value as a decimal, or the default if the value cannot be converted.
		/// </returns>
		public static object ToDecimal(object o, object defaultValue)
		{
			try
			{
				//If this is a string, remove any dollar signs and convert surrounding parentheses to
				//a minus sign.
				if (o is string)
				{
					string value = o.ToString().Trim();
					value = value.Replace(PrimaryCurrencySymbol, "");
					value = value.Replace(OtherCurrencySymbol, "");
					if (value.Substring(0, 1) == "(" && value.Substring(value.Length - 1, 1) == ")")
						value = "-" + value.Substring(1, value.Length - 2);
					o = value;
				}

				if (o == null || o.ToString() == "")
					return defaultValue;
				
				return Convert.ToDecimal(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a single without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a single.
		/// </param>
		/// <returns>
		/// The value as a single, or 0 if the value cannot be converted.
		/// </returns>
		public static float ToSingle(object o)
		{
			return ToSingle(o, 0);
		}


		/// <summary>
		/// Convert any value to a single without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a single.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a single.
		/// </param>
		/// <returns>
		/// The value as a single, or the default if the value cannot be converted.
		/// </returns>
		public static float ToSingle(object o, float defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToSingle(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                float result;
				return float.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a single without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a single.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a single.
		/// </param>
		/// <returns>
		/// The value as a single, or the default if the value cannot be converted.
		/// </returns>
		public static float? ToSingle(object o, float? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToSingle(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                float result;
				return float.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a single without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a single.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a single.
		/// </param>
		/// <returns>
		/// The value as a single, or the default if the value cannot be converted.
		/// </returns>
		public static object ToSingle(object o, object defaultValue)
		{
			try
			{
					if (o == null || o.ToString() == "")
						return defaultValue;

				if (!(o is string))
					return Convert.ToSingle(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                float result;
				return float.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
					//If conversion fails, return default.
					return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a double without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a double.
		/// </param>
		/// <returns>
		/// The value as a double, or 0 if the value cannot be converted.
		/// </returns>
		public static double ToDouble(object o)
		{
			return ToDouble(o, 0);
		}


		/// <summary>
		/// Convert any value to a double without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a double.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a double.
		/// </param>
		/// <returns>
		/// The value as a double, or the default if the value cannot be converted.
		/// </returns>
		public static double ToDouble(object o, double defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToDouble(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                double result;
				return double.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a double without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a double.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a double.
		/// </param>
		/// <returns>
		/// The value as a double, or the default if the value cannot be converted.
		/// </returns>
		public static double? ToDouble(object o, double? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;
				if (!(o is string))
					return Convert.ToDouble(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                double result;
				return double.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}


		/// <summary>
		/// Convert any value to a double without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a double.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a double.
		/// </param>
		/// <returns>
		/// The value as a double, or the default if the value cannot be converted.
		/// </returns>
		public static object ToDouble(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;

				if (!(o is string))
					return Convert.ToDouble(o);

                //Use TryParse if this is a string so that we may specify the NumberStyles enumeration.  Will also 
                //avoid using an exception to handle non-numeric strings, which hurts performance
                double result;
				return double.TryParse((string)o, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result) ? result : defaultValue;
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert an integer to a specific enumerated value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="number">
		/// The number to convert.
		/// </param>
		/// <returns>
		/// The associated enumerated type value.
		/// </returns>
		public static T ToEnum<T>(int number)
		{
			try
			{
				return (T)Enum.ToObject(typeof(T), number);
			}
			catch (System.Exception ex)
			{
				throw new SystemException(string.Format("Error occurred while converting {0} to enumerated type {1}.", number, typeof(T).Name), ex);
			}
		}

		/// <summary>
		/// Convert an integer to a specific enumerated value. If unable to convert, return the default value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="number">
		/// The number to convert.
		/// </param>
		/// <param name="defaultValue">
		/// The default value to return if the value cannot be converted to the enumerated type.
		/// </param>
		/// <returns>
		/// The associated enumerated type value.
		/// </returns>
		public static T ToEnum<T>(int number, T defaultValue)
		{
			try
			{
				return (T)Enum.ToObject(typeof(T), number);
			}
			catch 
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert an integer to a specific enumerated value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="number">
		/// The number to convert.
		/// </param>
		/// <returns>
		/// The associated enumerated type value.
		/// </returns>
		public static T ToEnum<T>(object number)
		{
			return ToEnum<T>(ToInt32(number));
		}

		/// <summary>
		/// Convert an integer to a specific enumerated value. If unable to convert, return the default value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="number">
		/// The number to convert.
		/// </param>
		/// <param name="defaultValue">
		/// The default value to return if the value cannot be converted to the enumerated type.
		/// </param>
		/// <returns>
		/// The associated enumerated type value.
		/// </returns>
		public static T ToEnum<T>(object number, T defaultValue)
		{
			return ToEnum(ToInt32(number), defaultValue);
		}

		/// <summary>
		/// Convert a string to a specific enumerated value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="name">
		/// The string to convert. This must be the name of the enuemration to return.
		/// </param>
		/// <returns>
		/// The enumerated constant of the indicated enumeration with the indicated name.
		/// </returns>
		public static T ToEnum<T>(string name)
		{
			try
			{
					return (T)Enum.Parse(typeof(T), name);
			}
			catch (System.Exception ex)
			{
					throw new SystemException(string.Format("Error occurred while converting '{0}' to enumerated type {1}.", name, typeof(T).Name), ex);
			}
		}

		/// <summary>
		/// Convert a string to a specific enumerated value. If unable to convert, return the default value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="name">
		/// The string to convert. This must be the name of the enuemration to return.
		/// </param>
		/// <param name="defaultValue">
		/// The default value to return if the value cannot be converted to the enumerated type.
		/// </param>
		/// <returns>
		/// The enumerated constant of the indicated enumeration with the indicated name.
		/// </returns>
		public static T ToEnum<T>(string name, T defaultValue)
		{
			try
			{
				return (T)Enum.Parse(typeof(T), name);
			}
			catch 
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert one enumerated type to another. 
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="constant">
		/// The enumeration to convert. This assumes that the enumeration being converted has the same name as a constant of the enumeration converted to.
		/// </param>
		/// <returns>
		/// The enumerated constant of the indicated enumeration with the same name as the indicated enumeration constant.
		/// </returns>
		public static T ToEnum<T>(Enum constant)
		{
			try
			{
				if (!Enum.IsDefined(typeof(T), constant.ToString()))
					return default(T);
				
				return (T)Enum.Parse(typeof(T), constant.ToString());
			}
			catch (System.Exception ex)
			{
				throw new SystemException(string.Format("Error occurred while converting '{0}' to enumerated type {1}.", constant, typeof(T).Name), ex);
			}
		}

		/// <summary>
		/// Convert one enumerated type to another. If unable to convert, return the default value.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the enumeration to which to convert the integer.
		/// </typeparam>
		/// <param name="constant">
		/// The enumeration to convert. This assumes that the enumeration being converted has the same name as a constant of the enumeration converted to.
		/// </param>
		/// <param name="defaultValue">
		/// The default value to return if the value cannot be converted to the enumerated type.
		/// </param>
		/// <returns>
		/// The enumerated constant of the indicated enumeration with the same name as the indicated enumeration constant.
		/// </returns>
		public static T ToEnum<T>(Enum constant, T defaultValue)
		{
			try
			{
				if (!Enum.IsDefined(typeof(T), constant.ToString()))
					return defaultValue;

				return (T)Enum.Parse(typeof(T), constant.ToString());
			}
			catch 
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert any value to a DateTime without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a DateTime.
		/// </param>
		/// <returns>
		/// The value as a DateTime, or null if the value cannot be converted.
		/// </returns>
		public static DateTime? ToDateTime(object o)
		{
			return ToDateTime(o, null);
		}


		/// <summary>
		/// Convert any value to a DateTime without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a DateTime.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a DateTime.
		/// </param>
		/// <returns>
		/// The value as a DateTime, or the default if the value cannot be converted.
		/// </returns>
		public static DateTime? ToDateTime(object o, DateTime? defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;
	
				return Convert.ToDateTime(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert any value to a DateTime without generating an error.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a DateTime.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a DateTime.
		/// </param>
		/// <returns>
		/// The value as a DateTime, or the default if the value cannot be converted.
		/// </returns>
		public static DateTime ToDateTime(object o, DateTime defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;
				return Convert.ToDateTime(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}

		/// <summary>
		/// Convert any value to a DateTime without generating an error and returning an object value.
		/// </summary>
		/// <param name="o">
		/// The value to convert to a DateTime.
		/// </param>
		/// <param name="defaultValue">
		/// The value to return if the indicated value cannot be converted to a DateTime.
		/// </param>
		/// <returns>
		/// The value as a DateTime, or the default if the value cannot be converted.
		/// </returns>
		public static object ToDateTime(object o, object defaultValue)
		{
			try
			{
				if (o == null || o.ToString() == "")
					return defaultValue;
				return Convert.ToDateTime(o);
			}
			catch
			{
				//If conversion fails, return default.
				return defaultValue;
			}
		}

		/// <summary>
		/// Determine if the indicated array contains the indicated item.
		/// </summary>
		/// <param name="array">
		/// The array to search for item.
		/// </param>
		/// <param name="item">
		/// The item to search for in the array.
		/// </param>
		/// <returns>
		/// True if the array contains the item. False if not.
		/// </returns>
		public static bool ArrayContains(object[] array, object item)
		{
		foreach (object element in array)
			if (element.Equals(item)) return true;

		return false;
		}

		/// <summary>
		/// Returns the Ascii value of the indicated character, using the system's 
		/// default encoding (equivalent of the VB6 Asc function)
		/// </summary>
		/// <param name="character"></param>
		/// <returns></returns>
		public static int AsciiValue(char character)
		{
			return Encoding.Default.GetBytes(new[] { character })[0];
		}

		/// <summary>
		/// Invokes the appropriate "To" function for the indicated value type for conversion.
		/// </summary>
		/// <param name="value">
		/// Original value object
		/// </param>
		/// <param name="valueType">
		/// The Type we want to convert to
		/// </param>
		/// <returns>
		/// Object of type we were supposed to convert to
		/// </returns>
		public static object ConvertType(object value, Type valueType)
		{
			switch (valueType.Name.ToUpper())
			{
				case "STRING":
					return ToString(value);

				case "INT16":
					return ToInt16(value);

				case "INT32":
					return ToInt32(value);

				case "BOOLEAN":
					return ToBoolean(value);

				case "DECIMAL":
					return ToDecimal(value);

				case "SINGLE":
					return ToSingle(value);

				case "DOUBLE":
					return ToDouble(value);

				case "BYTE":
					return ToByte(value);

				default:
					throw new SystemException("Unexpected type.");
			}
		}

		/// <summary>
		/// Creates a delimited string of values, with a special delimiter to be
		/// used between the next to last and last values
		/// </summary>
		/// <param name="delimiter">
		/// Delimiter to use between all but the last two values
		/// </param>
		/// <param name="finalDelimiter">
		/// Delimiter to use between last two values
		/// </param>
		/// <param name="items">
		/// Set of string values to concatenate
		/// </param>
		/// <returns></returns>
		public static string Join(string delimiter, string finalDelimiter, IEnumerable<string> items)
		{
			if (items == null)
				return "";

			IList<string> enumerable = items as IList<string> ?? items.ToList();
			int count = enumerable.Count();
			if (count == 0)
				return "";

			if (count < 3)
				return string.Join(finalDelimiter, enumerable);

			string[] itemList = enumerable.ToArray();
			return string.Concat(string.Join(delimiter, itemList, 0, count - 1), finalDelimiter, itemList.Last());
		}

		/// <summary>
		/// This will return a string consisting of strString1 repeated
		/// intNumRepeat times.
		/// </summary>
		/// <param name="repeat">
		/// Number of times to repeat the string.
		/// </param>
		/// <param name="str">
		/// String to repeat.
		/// </param>
		/// <returns>
		/// A string consisting of strString1 repeated intNumRepeat times.
		/// </returns>
		public static string StringReplicate(int repeat, string str)
		{
			string retStr = "";

			for (int i = 0; i < repeat; i++)
			{
					retStr += str;
			}

			return retStr;
		}

		/// <summary>
		/// Returns a trimmed string version of the object 
		/// </summary>
		/// <param name="obj">The object to call ToString on</param>
		/// <returns></returns>
		public static string ToStringTrim(this object obj)
		{
		    return obj == null 
                ? null 
                : obj.ToString().Trim();
		}

	    /// <summary>
		/// Determines if targetDate is between (inclusively) startDate and endDate
		/// </summary>
		/// <param name="targetDate"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <returns>True if target is within range; False otherwise.</returns>
		public static bool DateWithinRange(DateTime targetDate, DateTime startDate, DateTime endDate)
		{
			if (DateTime.Compare(startDate, targetDate) > 0 || DateTime.Compare(targetDate, endDate) > 0)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Delete the file with the indicated path. Unlike File.Delete, this won't generate an error if the file doesn't exist.
		/// </summary>
		/// <param name="path">
		/// The path of the file to delete.
		/// </param>
		public static void DeleteFile(string path)
		{
			if (File.Exists(path))
				File.Delete(path);
		}

		/// <summary>
		/// Splits a delimited string and returns the string of the indicated 0-indexed position.
		/// If not found, returns "".
		/// </summary>
		/// <param name="delimitedString">String to be searched for the field.</param>
		/// <param name="delimiter">Delimiter used in delimitedString.</param>
		/// <param name="fieldPosition">Position of field we want.</param>
		/// <returns></returns>
		public static string Field(string delimitedString, char delimiter, int fieldPosition)
		{
			if (string.IsNullOrEmpty(delimitedString))
				return "";

			string[] parts = delimitedString.Split(delimiter);

			if (parts.IsValidIndex(fieldPosition))
				return parts[fieldPosition]; 

			return ""; 
		}

		/// <summary>
		/// Get a unique file name in the indicated directory.
		/// </summary>
		/// <param name="path">
		/// The path of the directory in which to generate the unique file name.
		/// </param>
		/// <param name="fileName">
		/// The name of the suggested file to test for uniqueness. If a file with this name exists, integer extensions will be added to the file
		/// until a unique one is found.
		/// </param>
		/// <param name="extension">
		/// The extension of the file name to generate.
		/// </param>
		/// <returns>
		/// A unique file name in the indicated directory.
		/// </returns>
		public static string GetUniqueFileName(string path, string fileName, string extension)
		{
			//If  filename is blank, simply generate a GUID for the filename.
			//We don't need to insure this filename doesn't already exist since GUID's are always unique.
			if (fileName == "")
				return Path.Combine(path, string.Concat(Guid.NewGuid().ToString(), ".", extension));
			
			string tempName = Path.Combine(path, string.Concat(fileName, ".", extension));
			int i = 0;
			while (File.Exists(tempName) && i < 1000)
			{
				i++;
				tempName = Path.Combine(path, string.Concat(fileName, i.ToString("00#"), ".", extension));                    
			}
			return tempName;
		}

		/// <summary>
		/// Locates the order of a substring in a delimited string
		/// </summary>
		/// <param name="string1">String we are searching</param>
		/// <param name="string2">String we are searching for</param>
		/// <param name="delim">Delimiter used in string1</param>
		/// <param name="caseSensitive">Whether match is case-sensitive</param>
		/// <param name="trim">Whether to trim the elements of white space</param>
		/// <returns>The index of the string desired in the string we are searching.</returns>
		public static int Locate(string string1, string string2, string delim, bool caseSensitive, bool trim)
		{
			int lenDelim = delim.Length;

			if (lenDelim == 0)
				return -1;

			if (string.IsNullOrEmpty(string1))
				return -1;

			// Split the string we are searching in by the delimiter. (don't remove empty, it will ruin indices)
			string[] elements = string1.Split(new[] { delim }, StringSplitOptions.None);

			// Find the string we are looking for in element.
			for (int i = 0; i < elements.Length; i++)
			{
				string curElement;
				// Trim the element if needed.
				if (trim)
					curElement = elements[i].Trim();
				else
					curElement = elements[i];

				// See if it matches what we are looking for,
				//	ignoring cases if needed.
				if (caseSensitive)
				{
					if (curElement.Equals(string2))
						return i;
				}
				else
				{
					if (curElement.Equals(string2, StringComparison.OrdinalIgnoreCase))
						return i;
				}
			}

			// We didn't find it
			return -1;
		}

		/// <summary>
		/// Calculate the number of months between two dates.
		/// </summary>
		/// <returns>
		/// The number of whole months between the two dates.
		/// </returns>
		public static int MonthsBetweenDates(DateTime date1, DateTime date2)
		{
			DateTime earlyDate = (date1 < date2 ? date1 : date2);
			DateTime laterDate = (date1 >= date2 ? date1 : date2);

			int months = ((laterDate.Year * 12) + laterDate.Month) - ((earlyDate.Year * 12) + earlyDate.Month);

			if (laterDate.Day < earlyDate.Day) months--;
			return months;
		}

		/// <summary>
		/// Clone the indicated object.
		/// </summary>
		/// <param name="obj">
		/// The object to clone.
		/// </param>
		/// <returns>
		/// A copy of the indicated object.
		/// </returns>
		public static object Clone(object obj)
		{
				using (MemoryStream buffer = new MemoryStream())
				{
					 BinaryFormatter formatter = new BinaryFormatter();
					 formatter.Serialize(buffer, obj);
					 buffer.Position = 0;
					 object temp = formatter.Deserialize(buffer);
					 return temp;
				}
		}

		/// <summary>
		/// Clone the indicated object.
		/// </summary>
		/// <param name="obj">
		/// The object to clone.
		/// </param>
		/// <typeparam name="T">
		/// The type of the object to return. This saves you from having to coerce the returned object to the appropriate type.
		/// </typeparam> 
		/// <returns>
		/// A copy of the indicated object.
		/// </returns>
		public static T Clone<T>(T obj)
		{
			using (MemoryStream buffer = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(buffer, obj);
				buffer.Position = 0;
				object temp = formatter.Deserialize(buffer);
				return (T)temp;
			}
		}

		/// <summary>
		/// Creates a password that is not particularly strong.  Should only be used to create a temporary one
		/// (i.e. one that the user will be forced to change the first time they use it)
		/// </summary>
		/// <returns></returns>
		public static string TempPasswordGenerate()
		{
			string tempPassword = "";

			try
			{
				//Get a random word from a fixed list
				Random randomGenerator = new Random();

				//Add some random numbers between 2 - 9, two in front and two in back
				tempPassword = (randomGenerator.Next(8) + 1).ToString() + (randomGenerator.Next(8) + 1).ToString() +
									MstrWords[randomGenerator.Next(MstrWords.Length - 1)] +
									tempPassword + (randomGenerator.Next(8) + 1).ToString() + (randomGenerator.Next(8) + 1).ToString();

			}
			catch (System.Exception)
			{
				tempPassword = "";
			}
			return tempPassword;
		}

		/// <summary>
		/// Gets whether an object is null or an empty string.
		/// </summary>
		/// <param name="obj">
		/// The object to test.
		/// </param>
		/// <returns>
		/// True if the object is null or an empty string, false otherwise.
		/// </returns>
		public static bool IsObjectEmptyOrNull(object obj)
		{
			try
			{
				if (obj == null)
					return true;

				//If this check is impossible, the error thrown will cause return false.
				return string.IsNullOrEmpty((string) obj);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Convert the value to the desired type T.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>
		/// The converted value.
		/// </returns>
		public static object ChangeType<T>(object value)
		{
			Type type = typeof(T);

			if (type.IsNullable() && (value == null || value.Equals(string.Empty)))
				return null;

			//ChangeType doesn't work with nullable types, so get the underlying type if this is a nullable type.
			if (type.IsNullable())
				type = Nullable.GetUnderlyingType(type);

			//Convert the value to the appropriate type. ChangeType doesn't recognize -1 for booleans, so we will use ToBoolean for this.
			//Note that once I get a boolean, I still have to use ChangeType to turn it into type T.
			if (type == typeof(bool))
				value = (T)Convert.ChangeType(ToBoolean(value), type);
			else
				value = (T)Convert.ChangeType(value, type);

			return (T)value;
		}

		#endregion Methods

		#region Extension Methods

		/// <summary>
		/// Executes an action on each item in a enumerable.
		/// </summary>
		/// <typeparam name="T">
		/// Type of item.
		/// </typeparam>
		/// <param name="source">
		/// The source enumerable.
		/// </param>
		/// <param name="modifyAction">
		/// The action to take on each item.
		/// </param>
		/// <param name="startingIndex">
		/// Index of the first item on which to execute the action
		/// </param>
		/// <param name="endingIndex">
		/// Index of the last item on which to execute the action
		/// </param>
		/// <returns>
		/// The original object acted upon. So you can string Each calls together if needed.
		/// </returns>
		public static IList<T> Each<T>(this IList<T> source, Action<T> modifyAction, int startingIndex, int endingIndex)
			where T : class
		{
			if (!source.IsValidIndex(startingIndex) || !source.IsValidIndex(endingIndex))
				throw new IndexOutOfRangeException("Invalid index for accessing source list.");

			for (int i = startingIndex; i <= endingIndex; i++)
			{
				modifyAction(source[i]);
			}

			return source;
		}

		/// <summary>
		/// Executes an action on each item in a enumerable.
		/// </summary>
		/// <typeparam name="T">
		/// Type of item.
		/// </typeparam>
		/// <param name="source">
		/// The source enumerable.
		/// </param>
		/// <param name="modifyAction">
		/// The action to take on each item.
		/// </param>
		/// <returns>
		/// The original object acted upon. So you can string Each calls together if needed.
		/// </returns>
		public static IEnumerable<T> Each<T>(this IEnumerable<T> source, Action<T> modifyAction)
			where T: class
		{
			if (source == null)
				return null;

// ReSharper disable PossibleMultipleEnumeration
			foreach (T item in source)
			{
				modifyAction(item);
			}

			return source;
// ReSharper restore PossibleMultipleEnumeration
		}

		/// <summary>
		/// Checks to see if an index is a valid index to access in an list.
		/// </summary>
		/// <typeparam name="T">Type of list.</typeparam>
		/// <param name="list">List to check for.</param>
		/// <param name="index">Index to validate.</param>
		/// <returns>True if the index is valid for the list.</returns>
		public static bool IsValidIndex<T>(this IList<T> list, int index)
		{
			return (index >= 0 && index < list.Count);
		}

		/// <summary>
		/// Enqueues a range of items in the order they are in.
		/// </summary>
		/// <typeparam name="TItem">Type of the items.</typeparam>
		/// <param name="queue">Queue to add to.</param>
		/// <param name="rangeToAdd">Items to add.</param>
		public static void EnqueueRange<TItem>(this Queue<TItem> queue, IEnumerable<TItem> rangeToAdd)
		{
			foreach (TItem item in rangeToAdd)
			{
				queue.Enqueue(item);
			}
		}

		/// <summary>
		/// Removes the first item satisfying a predicate.
		/// </summary>
		/// <typeparam name="T">
		/// Type of items in the collection.
		/// </typeparam>
		/// <param name="source">
		/// The collection to remove from.
		/// </param>
		/// <param name="predicate">
		/// Predicate to satisfy for removal.
		/// </param>
		/// <returns>
		/// True if an item was removed.
		/// </returns>
		public static bool RemoveFirst<T>(this ICollection<T> source, Predicate<T> predicate)
		{
			// Find first satisfying, and remove it.
			foreach (T item in source)
			{
				if (predicate(item))
				{
					return source.Remove(item);
				}
			}

			// Nothing was removed
			return false;
		}

		/// <summary>
		/// Removes all items satisfying a predicate.
		/// </summary>
		/// <typeparam name="T">
		/// Type of items in the collection.
		/// </typeparam>
		/// <param name="source">
		/// The collection to remove from.
		/// </param>
		/// <param name="predicate">
		/// Predicate to satisfy for removal.
		/// </param>
		/// <returns>
		/// True if any item was removed.
		/// </returns>
		public static bool RemoveAll<T>(this ICollection<T> source, Predicate<T> predicate)
		{
			List<T> remove = new List<T>();

			//Loop through the collection and keep track of those we want to remove.
			foreach (T item in source)
			{
				if (predicate(item))
					remove.Add(item);
			}

			//Now remove all items in the remove list.
			foreach (T item in remove)
				source.Remove(item);

			return remove.Count > 0;
		}

		/// <summary>
		/// Removes all items from a collection
		/// </summary>
		/// <typeparam name="T">
		/// Type of items in the collection.
		/// </typeparam>
		/// <param name="source">
		/// The collection to remove from.
		/// </param>
		/// <returns>
		/// True if any item was removed.
		/// </returns>
		public static bool RemoveAll<T>(this ICollection<T> source)
		{
			List<T> remove = new List<T>();

			//Loop through the collection and get a reference to every item.
			foreach (T item in source)
				remove.Add(item);

			//Now remove all items in the remove list.
			foreach (T item in remove)
				source.Remove(item);

			return remove.Count > 0;
		}

		/// <summary>
		/// Gets the index of the first occurrence of an item satisfying 
		/// a predicate.
		/// </summary>
		/// <typeparam name="T">Type of items in the source.</typeparam>
		/// <param name="source">Source enumerable.</param>
		/// <param name="predicate">Predicate to satisfy.</param>
		/// <returns>
		/// The index of the first satisfactory item.
		/// -1 if not found.
		/// </returns>
		public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			int currentIndex = 0;
			foreach (T item in source)
			{
				if (predicate(item))
				{
					return currentIndex;
				}

				currentIndex++;
			}

			return -1;
		}

		/// <summary>
		/// Determines whether a sequence contains a specified element by using a predicate.
		/// </summary>
		/// <typeparam name="T">The type of elements.</typeparam>
		/// <param name="source">The enumerable to look in.</param>
		/// <param name="predicate">The predicate the desired result must satisfy.</param>
		public static bool Contains<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			foreach (T item in source)
			{
				if (predicate(item))
				{
					return true;
				}
			}

			// Not found.
			return false;
		}

		/// <summary>
		/// Gets whether the string is "0", empty, or null. 
		/// </summary>
		/// <param name="str">string value to test</param>
		/// <returns>Whether the string is "0", empty, or null</returns>
		public static bool IsEmptyOrZero(this string str)
		{
			return (string.IsNullOrEmpty(str) || str == "0");
		}

		/// <summary>
		/// Gets whether the string is empty or null. 
		/// </summary>
		/// <param name="str">string value to test</param>
		/// <returns>Whether the string is empty or null</returns>
		public static bool IsEmptyOrNull(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static DateTime? ToDateTime(this string str)
		{
			if (string.IsNullOrEmpty(str))
				return null;

			try
			{
				return DateTime.ParseExact(str, ParsePatterns, CultureInfo.InvariantCulture, DateTimeStyles.None);
			}
			catch
			{
				return ToDateTime(str, null);
			}
		}

		/// <summary>
		/// Get the start date of the calendar quarter that the date is in.
		/// </summary>
		public static DateTime QuarterStartDate(this DateTime dtm)
		{
			//First get the quarter the date is in.
			int month = dtm.Month;
			int quarter = month / 3;
			if (month % 3 > 0)
					quarter++;

			//Then use this to get the start date of the quarter.
			return new DateTime(dtm.Year, quarter * 3 - 2, 1);
		}

		/// <summary>
		/// Get the end date of the calendar quarter that the date is in.
		/// </summary>
		public static DateTime QuarterEndDate(this DateTime dtm)
		{
			//First get the quarter the date is in.
			int month = dtm.Month;
			int quarter = month / 3;
			if (month % 3 > 0)
					quarter++;

			//Then use this to get the end date of the quarter.
			month = quarter * 3;
			return new DateTime(dtm.Year, month, DateTime.DaysInMonth(dtm.Year, month));
		}

		/// <summary>
		/// This function will adjust the indicated date to a weekday, if
		/// the date falls on a weekend as follows: if the date falls on a
		/// Sunday the following Monday will be returned, if the date
		/// falls on a Saturday, the previous Friday will be returned.
		/// </summary>
		public static DateTime AdjustForWeekend(this DateTime dtm) 
		{
			DateTime newDate;
   
			//Does it fall on a weekend?
			switch (dtm.DayOfWeek)
			{
			   case DayOfWeek.Sunday:
				 newDate = dtm.AddDays(1); //Sunday -> Monday.
				   break;

			   case DayOfWeek.Saturday:
				 newDate = dtm.AddDays(-1); //Saturday -> Friday.
				   break;
	  
			   default:
				   return dtm;
			}

			//if we don't need to adjust, or the adjusted date falls in a different year than the original
			//(don't want to adverseley affect the fiscal year of a payment) then just return the original date 
			if (newDate.Year != dtm.Year)
				return dtm;

			return newDate;
		}

		/// <summary>
		/// Converts a bool to its -1 (true) or 0 (false) string value.
		/// </summary>
		/// <param name="boolean">Value to convert.</param>
		/// <returns>String value.</returns>
		public static string ToFlagString(this bool boolean)
		{
			return ToString(boolean, "-1/0");
		}

		/// <summary>
		/// This allows us to format a boolean value to a string using a format which indicates the string for true and the string for false in the
		/// format [TrueString]/[FalseString].
		/// </summary>
		/// <param name="boolean"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		public static string ToString(this bool boolean, string format)
		{
			string[] temp = format.Split('/');
			if (temp.Length == 2)
			{
				if (boolean)
					return temp[0];
				
				return temp[1];
			}
			
			return "";
		}
	
		/// <summary>
		/// This allows us to round a decimal value using a specified rounding factor.
		/// </summary>
		/// <param name="value">The value to round.</param>
		/// <param name="roundingFactor">
		/// The smallest increment to round to.
		/// </param>
		/// <returns>The rounded result.</returns>
		/// <example>
		/// <code>
		/// // This would round to 3.0
		/// decimal result = RoundTo(3.13, 0.5);
		/// // This would round to 3.5
		/// result = RoundTo(3.30, 0.5);
		/// // This would round to 3.13
		/// result = RoundTo(3.13423, 0.01);
		/// </code>
		/// </example>
		public static decimal RoundTo(this decimal value, decimal roundingFactor)
		{
			// Make sure we aren't trying to round to zero.
			if (roundingFactor.Equals(0))
			{
				return value;
			}

			// To round we divide value by roundingFactor and then round this number to the nearest 
			// whole number by converting to an integer. Then we multiply this result by roundingFactor. 
			// This will give us the multiple of roudingFactor that value is nearest to.
			Int64 multiple = ToInt64(value / roundingFactor);
			return roundingFactor * multiple;
		}

		/// <summary>
		/// This allows us to round down a decimal value using a specified rounding factor.
		/// This NEVER increases a value, only decreases it or leaves it the same.
		/// </summary>
		/// <param name="value">The value to round.</param>
		/// <param name="roundingFactor">
		/// The smallest increment to round to.
		/// </param>
		/// <returns>The rounded result.</returns>
		/// <example>
		/// <code>
		/// // This would round to 3.0
		/// decimal result = RoundTo(3.13, 0.5);
		/// // This would round to 3.0
		/// result = RoundTo(3.30, 0.5);
		/// // This would round to 3.13
		/// result = RoundTo(3.13423, 0.01);
		/// </code>
		/// </example>
		public static decimal RoundDownTo(this decimal value, decimal roundingFactor)
		{
			decimal roundedValue = RoundTo(value, roundingFactor);
			if (roundedValue > value)
			{
				// the Round method rounded up. So, subtract the rounding factor to go 
				//	down one increment.
				return roundedValue - roundingFactor;
			}

			// the Round method rounded down (or not at all), we are good to go
			return roundedValue;
		}

		///// <summary>
		///// Reverse a string.
		///// </summary>
		//public static string Reverse(this string value)
		//{
		//    char[] arr = arr = value.Reverse().ToArray();

		//    return new string(arr);
		//}

		/// <summary>
		/// Determines if a supplied string value is a hex value.
		/// </summary>
		/// <param name="value">
		/// String value to be validated
		/// </param>
		/// <returns>
		/// True if a valid hex value was found.
		/// </returns>
		public static bool IsHex(this string value)
		{
			return Regex.IsMatch(value, @"\A\b[0-9a-fA-F]+\b\Z");
		}

		/// <summary>
		/// Determines if a string contains another string ignoring case.
		/// </summary>
		/// <param name="s">
		/// String to look in.
		/// </param>
		/// <param name="value">String to look for.</param>
		/// <returns>True if s string contains value2</returns>
		/// <remarks>
		/// This is more efficient than doing ToUpper() on strings and using the default contains
		/// because that creates extra string unnecessarily.
		/// </remarks>
		public static bool ContainsInsensitive(this string s, string value)
		{
			return (s.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0);
		}

		/// <summary>
		/// Searches the enumeration for the first item satisfying the predicate,
		/// and returns position in the enumeration
		/// </summary>
		/// <typeparam name="T">The type of items in the enumeration</typeparam>
		/// <param name="enumerable">The enumeration to look in.</param>
		/// <param name="predicate">The predicate to satisfy.</param>
		/// <returns>The position of the first valid item. Or -1 is nothing found.</returns>
		public static int FindPosition<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
		{
			int index = -1;

			foreach (T item in enumerable)
			{
				// Increase the index to match the current position.
				index++;

				// If the item satisfies the predicate, return the position.
				if (predicate(item))
				{
					return index;
				}
			}

			// Haven't found it, return -1;
			return -1;
		}

		/// <summary>
		/// Gets value out of dictionary, adding it to the dictionary if it does
		/// not already exist in the dictionary.
		/// </summary>
		/// <typeparam name="TKey">Type of the keys in the dictionary.</typeparam>
		/// <typeparam name="TValue">Type of the values in the dictionary.</typeparam>
		/// <param name="dictionary">Dictionary to get/add to.</param>
		/// <param name="key">The key to get/add value for.</param>
		/// <param name="valueGetter">
		/// Function used to get a key's value when it is not in the dictionary.
		/// </param>
		/// <returns>The value for the key in the dictionary.</returns>
		public static TValue GetAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueGetter)
		{
			TValue value;
			// Try to get the value out of the dictionary.
			bool found = dictionary.TryGetValue(key, out value);

			if (found)
			{
				// Value was found in the dictionary, return it.
				return value;
			}
			
			// Value doesn't exist in dictionary.
			// Get the value with the getter function.
			value = valueGetter(key);
			// Add the value to the dictionary.
			dictionary[key] = value;
			// Return the value
			return value;
		}

		/// <summary>
		/// Adds a pair to a collection of KeyValuePairs.
		/// </summary>
		/// <typeparam name="TKey">Type of key.</typeparam>
		/// <typeparam name="TValue">Type of value.</typeparam>
		/// <param name="collection">Collection to add to.</param>
		/// <param name="key">Key for pair to add.</param>
		/// <param name="value">Value for pair to add.</param>
		public static void AddPair<TKey, TValue>(this ICollection<KeyValuePair<TKey, TValue>> collection, TKey key, TValue value)
		{
			collection.Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		/// <summary>
		/// Determines if a list/collection is empty.
		/// </summary>
		/// <typeparam name="TItem">Type of items.</typeparam>
		/// <param name="source">Source list/collection to check.</param>
		/// <returns>True if the source is empty.</returns>
		public static bool IsEmpty<TItem>(this ICollection<TItem> source)
		{
			return source.Count == 0;
		}

		/// <summary>
		/// Gets value out of dictionary, adding it to the dictionary if it does
		/// not already exist in the dictionary.
		/// </summary>
		/// <typeparam name="TKey">Type of the keys in the dictionary.</typeparam>
		/// <typeparam name="TValue">Type of the values in the dictionary.</typeparam>
		/// <param name="dictionary">Dictionary to get/add to.</param>
		/// <param name="key">The key to get/add value for.</param>
		/// <param name="valueGetter">
		/// Function used to get a key's value when it is not in the dictionary.
		/// </param>
		/// <returns>The value for the key in the dictionary.</returns>
		public static TValue GetAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueGetter)
		{
			TValue value;
			// Try to get the value out of the dictionary.
			bool found = dictionary.TryGetValue(key, out value);

			if (found)
			{
				// Value was found in the dictionary, return it.
				return value;
			}
			
			// Value doesn't exist in dictionary.
			// Get the value with the getter function.
			value = valueGetter();
			// Add the value to the dictionary.
			dictionary[key] = value;
			// Return the value
			return value;
		}

		/// <summary>
		/// Get the value of the indicated child element of this XElement or an empty string if the child element doesn't exist.
		/// </summary>
		/// <param name="value">
		/// An XElement.
		/// </param>
		/// <param name="name">
		/// The name of a child XElement. 
		/// </param>
		/// <returns>
		/// The value of the indicated child element. If a child element with the indicated name doesn't exist, an empty string is returned.
		/// </returns>
		public static string GetElementValueOrEmptyString(this XElement value, string name)
		{
			if (value == null)
				return string.Empty;

			XElement element = value.Element(name);
			if (element == null)
				return string.Empty;

			return element.Value;
		}

		/// <summary>
		/// Get the value of the indicated child element of this XElement or a specified default string if the child element doesn't exist.
		/// </summary>
		/// <param name="value">
		/// An XElement.
		/// </param>
		/// <param name="name">
		/// The name of a child XElement. 
		/// </param>
		/// <param name="dflt"></param>
		/// <returns>
		/// The value of the indicated child element. If a child element with the indicated name doesn't exist, the specified default string is returned.
		/// </returns>
		public static string GetElementValueOrDefaultString(this XElement value, string name, string dflt)
		{
			XElement element = value.Element(name);
			if (element == null)
				return dflt;
			return element.Value;
		}

		/// <summary>
		/// Get the value of the indicated attribute of this XElement or an empty string if the attribute doesn't exist.
		/// </summary>
		/// <param name="value">
		/// An XElement.
		/// </param>
		/// <param name="name">
		/// The name of an attribute. 
		/// </param>
		/// <returns>
		/// The value of the indicated attribute. If an attribute with the indicated name doesn't exist, an empty string is returned.
		/// </returns>
		public static string GetAttributeValueOrEmptyString(this XElement value, string name)
		{
			XAttribute attribute = value.Attribute(name);
			if (attribute == null)
				return string.Empty;
			return attribute.Value;
		}

		/// <summary>
		/// Get the value of the indicated attribute of this XElement or a specified default string if the attribute doesn't exist.
		/// </summary>
		/// <param name="value">
		/// An XElement.
		/// </param>
		/// <param name="name">
		/// The name of an attribute. 
		/// </param>
		/// <param name="dflt"></param>
		/// <returns>
		/// The value of the indicated attribute. If an attribute with the indicated name doesn't exist, the specified default string is returned.
		/// </returns>
		public static string GetAttributeValueOrDefaultString(this XElement value, string name, string dflt)
		{
			XAttribute attribute = value.Attribute(name);
			if (attribute == null)
				return dflt;
			return attribute.Value;
		}

		public static void AddValueNode(this XElement element, string nodeName, string value)
		{
			string nodeWithValue = string.Concat("<", nodeName, ">", value, "</", nodeName, ">");
			XElement elementToAdd = XElement.Parse(nodeWithValue);
			element.Add(elementToAdd);
		}

		/// <summary>
		/// Checks to see if an enumerated value contains a type. This is useful for those enumerated types marked as Flags.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <returns>
		/// True if the enumerated value includes the indicated type flag.
		/// </returns>
		public static bool Has<T>(this Enum type, T value)
		{
			try
			{
				return (((int)(object)type &
				  (int)(object)value) == (int)(object)value);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// This is an extension method on the Type type that will return true if the type is nullable. Note that this will return false if the type tested
		/// was returned from the GetType method, even if the type is in fact nullable.
		/// </summary>
		/// <param name="type">
		/// The type to test for nullability.
		/// </param>
		/// <returns>
		/// True if the type is nullable.
		/// </returns>
		public static bool IsNullable(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		/// <summary>
		/// Format the decimal as a string using the current locale's currency format but with the indicated currency symbol instead of locale's currency symbol.
		/// </summary>
		/// <param name="value">
		/// The decimal value to format.
		/// </param>
		/// <param name="currencySymbol">
		/// The currency symbol to use instead of the current symbol.
		/// </param>
		/// <returns>
		/// The decimal value formatted as a currency string.
		/// </returns>
		public static string ToCurrencyString(this decimal value, string currencySymbol)
		{
			CultureInfo culture = CultureInfo.CurrentUICulture.Clone() as CultureInfo;
			if (culture == null)
				return value.ToString("c");

			if (currencySymbol != null)
				culture.NumberFormat.CurrencySymbol = currencySymbol;
			return value.ToString("c", culture);
		}
		public static string ToCurrencyString(this decimal value)
		{
			return value.ToCurrencyString(null);
		}

		#endregion Extension Methods
	}
}
