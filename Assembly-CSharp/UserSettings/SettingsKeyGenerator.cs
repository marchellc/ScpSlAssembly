using System;
using System.Collections.Generic;
using System.Text;

namespace UserSettings
{
	public static class SettingsKeyGenerator
	{
		public static string EnumToKey<T>(T val) where T : Enum, IConvertible
		{
			return SettingsKeyGenerator.TypeValueToKey(SettingsKeyGenerator.GetStableTypeHash(typeof(T), false), val.ToUInt16(null));
		}

		public static string TypeValueToKey(ushort typeHash, ushort value)
		{
			SettingsKeyGenerator.UshortToHex(typeHash);
			for (int i = 0; i < 4; i++)
			{
				SettingsKeyGenerator.KeyBuilder[13 + i] = SettingsKeyGenerator.HexNonAlloc32[i];
			}
			SettingsKeyGenerator.UshortToHex(value);
			for (int j = 0; j < 4; j++)
			{
				SettingsKeyGenerator.KeyBuilder[18 + j] = SettingsKeyGenerator.HexNonAlloc32[j];
			}
			return SettingsKeyGenerator.KeyBuilder.ToString();
		}

		public static ushort GetStableTypeHash(Type type, bool preventCaching = false)
		{
			ushort num;
			if (!preventCaching && SettingsKeyGenerator.TypeToHash.TryGetValue(type, out num))
			{
				return num;
			}
			if (!type.IsEnum)
			{
				throw new ArgumentException("To reduce hash collisions, this method can only be used with enums!", "type");
			}
			int num2 = 23;
			foreach (char c in type.Name)
			{
				num2 = num2 * 31 + (int)c;
			}
			ushort num3 = (ushort)(num2 & 65535);
			if (preventCaching)
			{
				return num3;
			}
			Type type2;
			if (SettingsKeyGenerator.HashToType.TryGetValue(num3, out type2))
			{
				throw new InvalidOperationException(string.Concat(new string[] { "Type ", type.Name, " hash-collided with type ", type2.Name, ". Consider renaming one of the enums." }));
			}
			SettingsKeyGenerator.HashToType[num3] = type;
			SettingsKeyGenerator.TypeToHash[type] = num3;
			return num3;
		}

		private static void UshortToHex(ushort val)
		{
			int num = 0;
			for (int i = 0; i < 4; i++)
			{
				SettingsKeyGenerator.HexNonAlloc32[i] = SettingsKeyGenerator.IntToHexArr[(val >> num) & 15];
				num += 4;
			}
		}

		private static readonly Dictionary<Type, ushort> TypeToHash = new Dictionary<Type, ushort>();

		private static readonly Dictionary<ushort, Type> HashToType = new Dictionary<ushort, Type>();

		private static readonly char[] HexNonAlloc32 = new char[4];

		private static readonly char[] IntToHexArr = new char[]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
			'A', 'B', 'C', 'D', 'E', 'F'
		};

		private static readonly StringBuilder KeyBuilder = new StringBuilder("UserSettings_FFFF_FFFF");

		private const int HexLength = 4;

		private const int SbHashStartIndex = 13;

		private const int SbValueStartIndex = 18;
	}
}
