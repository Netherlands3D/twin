﻿using System.Collections.Generic;
using System.Text;
using System;
using System.Text.RegularExpressions;
using System.Globalization;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.ExtensionMethods
{
	public static class StringExtensions
	{
		/// <summary>
		/// Replace the template string and fill in the x and y values
		/// </summary>
		/// <param name="template">The template string</param>
		/// <param name="x">double value x</param>
		/// <param name="y">double value y</param>
		/// <returns>The replaced template string</returns>
		public static string ReplaceXY(this string template, double x, double y)
		{
			StringBuilder sb = new StringBuilder(template);
			sb.Replace("{x}", $"{x}");
			sb.Replace("{y}", $"{y}");
			return sb.ToString();
		}

		/// <summary>
		/// Replace the template properties defined in a KeyValuePair list
		/// </summary>
		/// <param name="template">The template string</param>
		/// <param name="keyvals">KeyValuePair list</param>    
		/// <returns>The replaced template string</returns>
		public static string ReplacePlaceholders(this string template, List<KeyValuePair<string, object>> keyvals)
		{
			foreach (var keyval in keyvals)
			{
				var name = keyval.Key;
				var val = keyval.Value;
				template = template.Replace($"{{{name}}}", $"{val}");
			}

			return template;
		}

		/// <summary>
		/// Get the RD coordinate of a files string
		/// </summary>
		/// <param name="filepath">string filepath</param>
		/// <returns>The Vector3RD coordinate</returns>
		public static Vector3RD GetRDCoordinate(this string filepath)
		{
			var numbers = new Regex(@"(\d{4,6})");

			var matches = numbers.Matches(filepath);
			if (matches.Count == 2)
			{
				return new Vector3RD()
				{
					x = double.Parse(matches[0].Value),
					y = double.Parse(matches[1].Value)
				};
			}
			else
			{
				throw new Exception($"Could not get RD coordinate of string: {filepath}");
			}
		}

		/// <summary>
		/// Retrieves a specific parameter value from an url with a query string
		/// </summary>
		/// <param name="url">An url like "https://mypage.nl?param=value&param2=value2#hashvalue"</param>
		/// <param name="param">The parameter name</param>
		/// <returns>The value of the parameter</returns>
		public static string GetUrlParamValue(this string url, string param)
		{
			var groups = Regex.Match(url, $"[?&]{param}=([^&#]*)").Groups;
			if (groups.Count < 2) return null;
			return groups[1].Value;
		}

		public static string ToInvariant(this double d)
		{
			return d.ToString(CultureInfo.InvariantCulture);
		}
	}
}
