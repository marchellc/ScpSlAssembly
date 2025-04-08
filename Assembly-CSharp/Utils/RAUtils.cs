using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Utils
{
	public static class RAUtils
	{
		public static string FormatArguments(ArraySegment<string> args, int index)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			foreach (string text in args.Segment(index))
			{
				stringBuilder.Append(text);
				stringBuilder.Append(" ");
			}
			string text2 = stringBuilder.ToString();
			StringBuilderPool.Shared.Return(stringBuilder);
			return text2;
		}

		public static List<ReferenceHub> ProcessPlayerIdOrNamesList(ArraySegment<string> args, int startindex, out string[] newargs, bool keepEmptyEntries = false)
		{
			List<ReferenceHub> list3;
			try
			{
				string text = RAUtils.FormatArguments(args, startindex);
				List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent();
				if (text.StartsWith("@", StringComparison.Ordinal))
				{
					foreach (object obj in new Regex("@\"(.*?)\".|@[^\\s.]+\\.").Matches(text))
					{
						Match match = (Match)obj;
						text = RAUtils.ReplaceFirst(text, match.Value, "");
						string name = match.Value.Substring(1).Replace("\"", "").Replace(".", "");
						List<ReferenceHub> list2 = ReferenceHub.AllHubs.Where((ReferenceHub ply) => ply.nicknameSync.MyNick.Equals(name)).ToList<ReferenceHub>();
						if (list2.Count == 1 && !list.Contains(list2[0]))
						{
							list.Add(list2[0]);
						}
					}
					newargs = text.Split(new char[] { ' ' }, keepEmptyEntries ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries);
					list3 = list;
				}
				else
				{
					if (args.At(startindex).Length > 0)
					{
						if (char.IsDigit(args.At(startindex)[0]))
						{
							string[] array = args.At(startindex).Split('.', StringSplitOptions.None);
							for (int i = 0; i < array.Length; i++)
							{
								int num;
								ReferenceHub referenceHub;
								if (int.TryParse(array[i], out num) && ReferenceHub.TryGetHub(num, out referenceHub) && !list.Contains(referenceHub))
								{
									list.Add(referenceHub);
								}
							}
						}
						else if (char.IsLetter(args.At(startindex)[0]))
						{
							foreach (string text2 in args.At(startindex).Split('.', StringSplitOptions.None))
							{
								foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
								{
									if (referenceHub2.nicknameSync.MyNick.Equals(text2) && !list.Contains(referenceHub2))
									{
										list.Add(referenceHub2);
									}
								}
							}
						}
					}
					newargs = ((args.Count > 1) ? RAUtils.FormatArguments(args, startindex + 1).Split(new char[] { ' ' }, keepEmptyEntries ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries) : null);
					list3 = list;
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				newargs = null;
				list3 = null;
			}
			return list3;
		}

		private static string ReplaceFirst(string str, string search, string replace)
		{
			int num = str.IndexOf(search, StringComparison.Ordinal);
			if (num >= 0)
			{
				return str.Substring(0, num) + replace + str.Substring(num + search.Length);
			}
			return str;
		}

		private const string PlayerNameRegex = "@\"(.*?)\".|@[^\\s.]+\\.";

		public static readonly Regex IsDigit = new Regex("^(\\d*\\.?\\d*)$", RegexOptions.None);
	}
}
