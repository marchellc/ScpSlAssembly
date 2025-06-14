using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NorthwoodLib.Pools;

public static class PlayerPrefsSl
{
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

	private const string ArraySeparator = ";;`'.+=;;";

	private const string KeySeparator = "::-%(|::";

	private static readonly Dictionary<string, string> _registry;

	private static readonly string _path;

	private static readonly UTF8Encoding Encoding;

	public static event Action<string, string> SettingChanged;

	public static event Action<string> SettingRemoved;

	public static event Action SettingsRefreshed;

	static PlayerPrefsSl()
	{
		PlayerPrefsSl._registry = new Dictionary<string, string>();
		PlayerPrefsSl.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		PlayerPrefsSl._path = FileManager.GetAppFolder() + "registry.txt";
		PlayerPrefsSl.Refresh();
	}

	private static string Prefix(string key, DataType type)
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
		PlayerPrefsSl.SettingsRefreshed?.Invoke();
	}

	private static string Serialize()
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		foreach (KeyValuePair<string, string> item in PlayerPrefsSl._registry)
		{
			stringBuilder.Append(item.Key);
			stringBuilder.Append("::-%(|::");
			stringBuilder.Append(item.Value);
			stringBuilder.AppendLine();
		}
		string result = stringBuilder.ToString();
		StringBuilderPool.Shared.Return(stringBuilder);
		return result;
	}

	private static void Save()
	{
		File.WriteAllText(PlayerPrefsSl._path, PlayerPrefsSl.Serialize(), PlayerPrefsSl.Encoding);
	}

	public static bool HasKey(string key, DataType type)
	{
		return PlayerPrefsSl._registry.ContainsKey(PlayerPrefsSl.Prefix(key, type));
	}

	public static void DeleteKey(string key, DataType type)
	{
		PlayerPrefsSl._registry.Remove(PlayerPrefsSl.Prefix(key, type));
		PlayerPrefsSl.Save();
		PlayerPrefsSl.SettingRemoved?.Invoke(key);
	}

	public static bool TryGetKey(string key, DataType type, out string value)
	{
		return PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, type), out value);
	}

	public static void SetKey(string key, DataType type, string value)
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
		PlayerPrefsSl.SettingChanged?.Invoke(key, value);
	}

	public static void Set(string key, bool value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Bool), value ? "true" : "false");
	}

	public static void Set(string key, byte value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Byte), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, sbyte value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Sbyte), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, char value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Char), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, decimal value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Decimal), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, double value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Double), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, float value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Float), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, int value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Int), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, uint value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Uint), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, long value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Long), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, ulong value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Ulong), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, short value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Short), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, ushort value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.Ushort), value.ToString(CultureInfo.InvariantCulture));
	}

	public static void Set(string key, string value)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.String), value);
	}

	public static void Set(string key, bool[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.BoolArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<bool> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.BoolArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, byte[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.ByteArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<byte> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.ByteArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, sbyte[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.SbyteArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<sbyte> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.SbyteArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, char[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.CharArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<char> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.CharArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, decimal[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.DecimalArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<decimal> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.DecimalArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, double[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.DoubleArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<double> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.DoubleArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, float[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.FloatArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<float> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.FloatArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, int[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.IntArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<int> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.IntArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, uint[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UintArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<uint> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UintArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, long[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.LongArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<long> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.LongArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, ulong[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UlongArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<ulong> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UlongArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, short[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.ShortArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<short> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.ShortArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, ushort[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UshortArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<ushort> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.UshortArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static void Set(string key, string[] array)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.StringArray), string.Join(";;`'.+=;;", array));
	}

	public static void Set(string key, IEnumerable<string> ienumerable)
	{
		PlayerPrefsSl.WriteString(PlayerPrefsSl.Prefix(key, DataType.StringArray), string.Join(";;`'.+=;;", ienumerable));
	}

	public static bool Get(string key, bool defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Bool), out var value) || !bool.TryParse(value, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static byte Get(string key, byte defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Byte), out var value) || !byte.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static sbyte Get(string key, sbyte defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Sbyte), out var value) || !sbyte.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static char Get(string key, char defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Char), out var value) || !char.TryParse(value, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static decimal Get(string key, decimal defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Decimal), out var value) || !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static double Get(string key, double defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Double), out var value) || !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static float Get(string key, float defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Float), out var value) || !float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static int Get(string key, int defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Int), out var value) || !int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static uint Get(string key, uint defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Uint), out var value) || !uint.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static long Get(string key, long defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Long), out var value) || !long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static ulong Get(string key, ulong defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Ulong), out var value) || !ulong.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static short Get(string key, short defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Short), out var value) || !short.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static ushort Get(string key, ushort defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.Int), out var value) || !ushort.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static string Get(string key, string defaultValue)
	{
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.String), out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static bool[] Get(string key, bool[] defaultValue)
	{
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.BoolArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.ByteArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.SbyteArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.CharArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.DecimalArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.DoubleArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.FloatArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.IntArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.UintArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.LongArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.UlongArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.ShortArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.UshortArray), out var value))
		{
			string[] array = value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
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
		if (!PlayerPrefsSl._registry.TryGetValue(PlayerPrefsSl.Prefix(key, DataType.StringArray), out var value))
		{
			return defaultValue;
		}
		return value.Split(new string[1] { ";;`'.+=;;" }, StringSplitOptions.None);
	}
}
