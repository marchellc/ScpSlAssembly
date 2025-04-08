using System;
using System.Collections.Generic;
using UnityEngine;

namespace UserSettings
{
	public static class UserSetting<T>
	{
		private static IPrefsReaderWriter<T> GetHandler()
		{
			Type typeFromHandle = typeof(T);
			object[] typeHandlers = UserSetting<T>.TypeHandlers;
			for (int i = 0; i < typeHandlers.Length; i++)
			{
				IPrefsReaderWriter<T> prefsReaderWriter = typeHandlers[i] as IPrefsReaderWriter<T>;
				if (prefsReaderWriter != null)
				{
					return prefsReaderWriter;
				}
			}
			throw new InvalidOperationException(string.Format("Type {0} is not defined as valid user settings value.", typeFromHandle));
		}

		public static T Get<TEnum>(TEnum key) where TEnum : Enum, IConvertible
		{
			ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum), false);
			ushort num = key.ToUInt16(null);
			return UserSetting<T>.Get(stableTypeHash, num);
		}

		public static T Get<TEnum>(TEnum key, T defaultValue, bool setAsDefault = false) where TEnum : Enum, IConvertible
		{
			ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum), false);
			ushort num = key.ToUInt16(null);
			return UserSetting<T>.Get(stableTypeHash, num, defaultValue, setAsDefault);
		}

		public static T Get(ushort typeHash, ushort val)
		{
			return UserSetting<T>.Get(typeHash, val, UserSetting<T>.GetDefaultValue(typeHash, val), false);
		}

		public static T Get(ushort typeHash, ushort val, T defaultValue, bool setAsDefault = false)
		{
			if (setAsDefault)
			{
				UserSetting<T>.SetDefaultValue(typeHash, val, defaultValue);
			}
			return UserSetting<T>.GetHandler().Load(SettingsKeyGenerator.TypeValueToKey(typeHash, val), defaultValue);
		}

		public static void Set<TEnum>(TEnum key, T value) where TEnum : Enum, IConvertible
		{
			UserSetting<T>.Set(SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum), false), key.ToUInt16(null), value);
		}

		public static void Set(ushort typeHash, ushort val, T value)
		{
			UserSetting<T>.GetHandler().Save(SettingsKeyGenerator.TypeValueToKey(typeHash, val), value);
			int count = UserSetting<T>.Listeners.Count;
			for (int i = 0; i < count; i++)
			{
				UserSetting<T>.SettingChangeListener settingChangeListener = UserSetting<T>.Listeners[i];
				if (settingChangeListener.TypeHash == typeHash && settingChangeListener.NumericalValue == val)
				{
					try
					{
						Action<T> @event = settingChangeListener.Event;
						if (@event != null)
						{
							@event(value);
						}
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				}
			}
		}

		private static void AddListener(UserSetting<T>.SettingChangeListener newListener)
		{
			foreach (UserSetting<T>.SettingChangeListener settingChangeListener in UserSetting<T>.Listeners)
			{
				if (settingChangeListener.Equals(newListener))
				{
					return;
				}
			}
			UserSetting<T>.Listeners.Add(newListener);
		}

		public static void AddListener<TEnum>(TEnum key, Action<T> listenerEvent) where TEnum : Enum, IConvertible
		{
			UserSetting<T>.AddListener(new UserSetting<T>.SettingChangeListener(key, listenerEvent));
		}

		public static void AddListener(ushort typeHash, ushort numValue, Action<T> listenerEvent)
		{
			UserSetting<T>.AddListener(new UserSetting<T>.SettingChangeListener(typeHash, numValue, listenerEvent));
		}

		public static void RemoveListener<TEnum>(TEnum key, Action<T> listenerEvent) where TEnum : Enum, IConvertible
		{
			UserSetting<T>.SettingChangeListener settingChangeListener = new UserSetting<T>.SettingChangeListener(key, listenerEvent);
			int count = UserSetting<T>.Listeners.Count;
			for (int i = 0; i < count; i++)
			{
				if (settingChangeListener.Equals(UserSetting<T>.Listeners[i]))
				{
					UserSetting<T>.Listeners.RemoveAt(i);
					return;
				}
			}
		}

		public static void RemoveListener(Action<T> listenerEvent)
		{
			UserSetting<T>.Listeners.RemoveAll((UserSetting<T>.SettingChangeListener x) => x.Event == listenerEvent);
		}

		public static void SetDefaultValue(ushort type, ushort val, T defaultValue)
		{
			UserSetting<T>.DefaultValues[UserSetting<T>.GetDefValueHash(type, val)] = defaultValue;
		}

		public static void SetDefaultValue<TEnum>(TEnum key, T defaultValue) where TEnum : Enum, IConvertible
		{
			ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum), false);
			ushort num = key.ToUInt16(null);
			UserSetting<T>.SetDefaultValue(stableTypeHash, num, defaultValue);
		}

		private static T GetDefaultValue(ushort type, ushort val)
		{
			T t;
			if (!UserSetting<T>.DefaultValues.TryGetValue(UserSetting<T>.GetDefValueHash(type, val), out t))
			{
				t = default(T);
			}
			return t;
		}

		private static uint GetDefValueHash(ushort type, ushort val)
		{
			return (uint)((int)type | ((int)val << 16));
		}

		private static readonly List<UserSetting<T>.SettingChangeListener> Listeners = new List<UserSetting<T>.SettingChangeListener>();

		private static readonly Dictionary<uint, T> DefaultValues = new Dictionary<uint, T>();

		private static readonly object[] TypeHandlers = new object[]
		{
			new BoolReaderWriter(),
			new FloatReaderWriter(),
			new IntReaderWriter(),
			new StringReaderWriter()
		};

		private readonly struct SettingChangeListener : IEquatable<UserSetting<T>.SettingChangeListener>
		{
			public bool Equals(UserSetting<T>.SettingChangeListener other)
			{
				return this.Event == other.Event && this.TypeHash == other.TypeHash && this.NumericalValue == other.NumericalValue;
			}

			public SettingChangeListener(Enum key, Action<T> listenerEvent)
			{
				this.TypeHash = SettingsKeyGenerator.GetStableTypeHash(key.GetType(), false);
				this.NumericalValue = ((IConvertible)key).ToUInt16(null);
				this.Event = listenerEvent;
			}

			public SettingChangeListener(ushort typeHash, ushort numVal, Action<T> listenerEvent)
			{
				this.TypeHash = typeHash;
				this.NumericalValue = numVal;
				this.Event = listenerEvent;
			}

			public readonly Action<T> Event;

			public readonly ushort TypeHash;

			public readonly ushort NumericalValue;
		}
	}
}
