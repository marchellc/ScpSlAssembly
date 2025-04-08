using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NorthwoodLib.Pools;

public static class PlayerPrefsSl
{
	public static event Action<string, string> SettingChanged;

	public static event Action<string> SettingRemoved;

	public static event Action SettingsRefreshed;

	static PlayerPrefsSl()
	{
		PlayerPrefsSl.Refresh();
	}

	private static string Prefix(string key, PlayerPrefsSl.DataType type)
	{
		byte b = (byte)type;
		return b.ToString("00") + key;
	}

	public static void Refresh()
	{
		PlayerPrefsSl._registry.Clear();
		if (!File.Exists(PlayerPrefsSl._path))
		{
			File.Create(PlayerPrefsSl._path).Close();
			return;
		}
		using (StreamReader streamReader = new StreamReader(PlayerPrefsSl._path))
		{
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				if (text.Contains("::-%(|::"))
				{
					int num = text.IndexOf("::-%(|::", StringComparison.Ordinal);
					PlayerPrefsSl._registry.Add(text.Substring(0, num), text.Substring(num + "::-%(|::".Length));
				}
			}
		}
		Action settingsRefreshed = PlayerPrefsSl.SettingsRefreshed;
		if (settingsRefreshed == null)
		{
			return;
		}
		settingsRefreshed();
	}

	private static string Serialize()
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		foreach (KeyValuePair<string, string> keyValuePair in PlayerPrefsSl._registry)
		{
			stringBuilder.Append(keyValuePair.Key);
			stringBuilder.Append("::-%(|::");
			stringBuilder.Append(keyValuePair.Value);
			stringBuilder.AppendLine();
		}
		string text = stringBuilder.ToString();
		StringBuilderPool.Shared.Return(stringBuilder);
		return text;
	}

	private static void Save()
	{
		File.WriteAllText(PlayerPrefsSl._path, PlayerPrefsSl.Serialize(), PlayerPrefsSl.Encoding);
	}

	public static bool HasKey(string key, PlayerPrefsSl.DataType type)
	{
		return PlayerPrefsSl._registry.ContainsKey(PlayerPrefsSl.Prefix(key, type));
	}

	public static void DeleteKey(string key, PlayerPrefsSl.DataType type)
	{
		PlayerPrefsSl._registry.Remove(PlayerPrefsSl.Prefix(key, type));
		PlayerPrefsSl.Save();
		Action<string> settingRemoved = PlayerPrefsSl.SettingRemoved;
		if (settingRemoved == null)
		{
			return;
		}
		settingRemoved(key);
	}

	public static bool TryGetKey(string key, PlayerPrefsSl.DataType type, out string value)
	{
		return PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, type), out value);
	}

	public static void SetKey(string key, PlayerPrefsSl.DataType type, string value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, type), value);
	}

	public static void DeleteAll()
	{
		PlayerPrefsSl._registry.Clear();
		File.WriteAllText(PlayerPrefsSl._path, "", PlayerPrefsSl.Encoding);
	}

	private static void WriteString(string key, string value)
	{
		PlayerPrefsSl._registry[key] = value;
		PlayerPrefsSl.Save();
		Action<string, string> settingChanged = PlayerPrefsSl.SettingChanged;
		if (settingChanged == null)
		{
			return;
		}
		settingChanged(key, value);
	}

	public static void Set(string key, bool value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Bool), value ? "true" : "false");
	}

	public static void Set(string key, byte value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Byte), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, sbyte value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Sbyte), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, char value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Char), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, decimal value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Decimal), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, double value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Double), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, float value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Float), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, int value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Int), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, uint value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Uint), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, long value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Long), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, ulong value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Ulong), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, short value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Short), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, ushort value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Ushort), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, string value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.String), value);
	}

	public static void Set(string key, bool[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.BoolArray), string.Join<bool>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<bool> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.BoolArray), string.Join<bool>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, byte[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ByteArray), string.Join<byte>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<byte> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ByteArray), string.Join<byte>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, sbyte[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.SbyteArray), string.Join<sbyte>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<sbyte> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.SbyteArray), string.Join<sbyte>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, char[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.CharArray), string.Join<char>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<char> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.CharArray), string.Join<char>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, decimal[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DecimalArray), string.Join<decimal>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<decimal> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DecimalArray), string.Join<decimal>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, double[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DoubleArray), string.Join<double>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<double> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DoubleArray), string.Join<double>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, float[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.FloatArray), string.Join<float>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<float> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.FloatArray), string.Join<float>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, int[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.IntArray), string.Join<int>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<int> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.IntArray), string.Join<int>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, uint[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UintArray), string.Join<uint>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<uint> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UintArray), string.Join<uint>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, long[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.LongArray), string.Join<long>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<long> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.LongArray), string.Join<long>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, ulong[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UlongArray), string.Join<ulong>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<ulong> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UlongArray), string.Join<ulong>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, short[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ShortArray), string.Join<short>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<short> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ShortArray), string.Join<short>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, ushort[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UshortArray), string.Join<ushort>(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<ushort> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UshortArray), string.Join<ushort>(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, string[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.StringArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<string> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.StringArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static bool Get(string key, bool defaultValue)
	{
		string text;
		bool flag;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Bool), out text) || !bool.TryParse(text, out flag))
		{
			return defaultValue;
		}
		return flag;
	}

	public static byte Get(string key, byte defaultValue)
	{
		string text;
		byte b;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Byte), out text) || !byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out b))
		{
			return defaultValue;
		}
		return b;
	}

	public static sbyte Get(string key, sbyte defaultValue)
	{
		string text;
		sbyte b;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Sbyte), out text) || !sbyte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out b))
		{
			return defaultValue;
		}
		return b;
	}

	public static char Get(string key, char defaultValue)
	{
		string text;
		char c;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Char), out text) || !char.TryParse(text, out c))
		{
			return defaultValue;
		}
		return c;
	}

	public static decimal Get(string key, decimal defaultValue)
	{
		string text;
		decimal num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Decimal), out text) || !decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static double Get(string key, double defaultValue)
	{
		string text;
		double num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Double), out text) || !double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static float Get(string key, float defaultValue)
	{
		string text;
		float num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Float), out text) || !float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static int Get(string key, int defaultValue)
	{
		string text;
		int num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Int), out text) || !int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static uint Get(string key, uint defaultValue)
	{
		string text;
		uint num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Uint), out text) || !uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static long Get(string key, long defaultValue)
	{
		string text;
		long num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Long), out text) || !long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static ulong Get(string key, ulong defaultValue)
	{
		string text;
		ulong num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Ulong), out text) || !ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static short Get(string key, short defaultValue)
	{
		string text;
		short num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Short), out text) || !short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static ushort Get(string key, ushort defaultValue)
	{
		string text;
		ushort num;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.Int), out text) || !ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return defaultValue;
		}
		return num;
	}

	public static string Get(string key, string defaultValue)
	{
		string text;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.String), out text))
		{
			return defaultValue;
		}
		return text;
	}

	public static bool[] Get(string key, bool[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.BoolArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			bool[] array2 = new bool[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!bool.TryParse(array[i], out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static byte[] Get(string key, byte[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ByteArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			byte[] array2 = new byte[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!byte.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static sbyte[] Get(string key, sbyte[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.SbyteArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			sbyte[] array2 = new sbyte[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!sbyte.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static char[] Get(string key, char[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.CharArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			char[] array2 = new char[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!char.TryParse(array[i], out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static decimal[] Get(string key, decimal[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DecimalArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			decimal[] array2 = new decimal[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!decimal.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static double[] Get(string key, double[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.DoubleArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			double[] array2 = new double[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!double.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static float[] Get(string key, float[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.FloatArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			float[] array2 = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!float.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static int[] Get(string key, int[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.IntArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			int[] array2 = new int[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!int.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static uint[] Get(string key, uint[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UintArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			uint[] array2 = new uint[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!uint.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static long[] Get(string key, long[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.LongArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			long[] array2 = new long[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!long.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static ulong[] Get(string key, ulong[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UlongArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			ulong[] array2 = new ulong[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!ulong.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static short[] Get(string key, short[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.ShortArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			short[] array2 = new short[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!short.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static ushort[] Get(string key, ushort[] defaultValue)
	{
		string text;
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.UshortArray), out text))
		{
			string[] array = text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
			ushort[] array2 = new ushort[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!ushort.TryParse(array[i], NumberStyles.Any, CultureInfo.InvariantCulture, out array2[i]))
				{
					return defaultValue;
				}
			}
			return array2;
		}
		return defaultValue;
	}

	public static string[] Get(string key, string[] defaultValue)
	{
		string text;
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, PlayerPrefsSl.DataType.StringArray), out text))
		{
			return defaultValue;
		}
		return text.Split(new string[] { ";;`'.+=;;" }, StringSplitOptions.None);
	}

	private const string ArraySeparator = ";;`'.+=;;";

	private const string KeySeparator = "::-%(|::";

	private static readonly Dictionary<string, string> _registry = new Dictionary<string, string>();

	private static readonly string _path = FileManager.GetAppFolder(true, false, "") + "registry.txt";

	private static readonly UTF8Encoding Encoding = new UTF8Encoding(false);

	public enum DataType : byte
	{
		Bool,
		Byte,
		Sbyte,
		Char,
		Decimal,
		Double,
		Float,
		Int,
		Uint,
		Long,
		Ulong,
		Short,
		Ushort,
		String,
		BoolArray,
		ByteArray,
		SbyteArray,
		CharArray,
		DecimalArray,
		DoubleArray,
		FloatArray,
		IntArray,
		UintArray,
		LongArray,
		UlongArray,
		ShortArray,
		UshortArray,
		StringArray
	}
}
