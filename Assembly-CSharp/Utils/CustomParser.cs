using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Utils
{
	public static class CustomParser
	{
		public static bool RemoveUnknownCharacters(string source, out string res)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent(source.Length);
			bool flag = false;
			foreach (char c in source)
			{
				if (CustomParser.KnownCharacters.Contains(c))
				{
					stringBuilder.Append(c);
				}
				else
				{
					flag = true;
				}
			}
			if (flag)
			{
				res = stringBuilder.ToString();
			}
			else
			{
				res = source;
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			return flag;
		}

		public static CustomParser.ParseResult TryParseFloat(string source, out float val)
		{
			CustomParser.ParseResult parseResult = CustomParser.ParseResult.FullSuccess;
			val = 0f;
			if (CustomParser.RemoveUnknownCharacters(source, out source))
			{
				parseResult = CustomParser.ParseResult.PartialSuccess;
			}
			if (string.IsNullOrEmpty(source))
			{
				return CustomParser.ParseResult.Failed;
			}
			source = source.Replace(",", ".");
			if (source.Contains("."))
			{
				string[] array = source.Split('.', StringSplitOptions.None);
				if (array.Length > 2)
				{
					string text = string.Empty;
					for (int i = 0; i < array.Length - 1; i++)
					{
						text += array[i];
					}
					array[0] = text;
					array[1] = array[array.Length - 1];
				}
				if (string.IsNullOrEmpty(array[0]))
				{
					array[0] = "0";
				}
				val = float.Parse(array[0]);
				if (!string.IsNullOrEmpty(array[1]))
				{
					float num;
					if (float.TryParse(array[1], out num))
					{
						val += num / Mathf.Pow(10f, (float)array[1].Length);
					}
					else
					{
						parseResult = CustomParser.ParseResult.PartialSuccess;
					}
				}
				return parseResult;
			}
			if (!float.TryParse(source, out val))
			{
				return CustomParser.ParseResult.Failed;
			}
			return parseResult;
		}

		public static CustomParser.ParseResult TryParseInt(string source, out int val)
		{
			float num;
			CustomParser.ParseResult parseResult = CustomParser.TryParseFloat(source, out num);
			if (parseResult == CustomParser.ParseResult.Failed)
			{
				val = 0;
				return parseResult;
			}
			val = (int)num;
			if (num == (float)((int)num))
			{
				return parseResult;
			}
			return CustomParser.ParseResult.PartialSuccess;
		}

		private static readonly HashSet<char> KnownCharacters = new HashSet<char>("0123456789.,");

		public enum ParseResult : byte
		{
			FullSuccess,
			PartialSuccess,
			Failed
		}
	}
}
