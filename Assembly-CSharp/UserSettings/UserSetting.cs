using System;
using System.Collections.Generic;
using UnityEngine;

namespace UserSettings;

public static class UserSetting<T>
{
	private readonly struct SettingChangeListener : IEquatable<SettingChangeListener>
	{
		public readonly Action<T> Event;

		public readonly ushort TypeHash;

		public readonly ushort NumericalValue;

		public bool Equals(SettingChangeListener other)
		{
			if (this.Event == other.Event && this.TypeHash == other.TypeHash)
			{
				return this.NumericalValue == other.NumericalValue;
			}
			return false;
		}

		public SettingChangeListener(Enum key, Action<T> listenerEvent)
		{
			this.TypeHash = SettingsKeyGenerator.GetStableTypeHash(key.GetType());
			this.NumericalValue = ((IConvertible)key).ToUInt16((IFormatProvider)null);
			this.Event = listenerEvent;
		}

		public SettingChangeListener(ushort typeHash, ushort numVal, Action<T> listenerEvent)
		{
			this.TypeHash = typeHash;
			this.NumericalValue = numVal;
			this.Event = listenerEvent;
		}
	}

	private static readonly List<SettingChangeListener> Listeners = new List<SettingChangeListener>();

	private static readonly Dictionary<uint, T> DefaultValues = new Dictionary<uint, T>();

	private static readonly object[] TypeHandlers = new object[4]
	{
		new BoolReaderWriter(),
		new FloatReaderWriter(),
		new IntReaderWriter(),
		new StringReaderWriter()
	};

	private static IPrefsReaderWriter<T> GetHandler()
	{
		Type typeFromHandle = typeof(T);
		object[] typeHandlers = UserSetting<T>.TypeHandlers;
		for (int i = 0; i < typeHandlers.Length; i++)
		{
			if (typeHandlers[i] is IPrefsReaderWriter<T> result)
			{
				return result;
			}
		}
		throw new InvalidOperationException($"Type {typeFromHandle} is not defined as valid user settings value.");
	}

	public static T Get<TEnum>(TEnum key) where TEnum : Enum, IConvertible
	{
		ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum));
		ushort val = key.ToUInt16(null);
		return UserSetting<T>.Get(stableTypeHash, val);
	}

	public static T Get<TEnum>(TEnum key, T defaultValue, bool setAsDefault = false) where TEnum : Enum, IConvertible
	{
		ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum));
		ushort val = key.ToUInt16(null);
		return UserSetting<T>.Get(stableTypeHash, val, defaultValue, setAsDefault);
	}

	public static T Get(ushort typeHash, ushort val)
	{
		return UserSetting<T>.Get(typeHash, val, UserSetting<T>.GetDefaultValue(typeHash, val));
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
		UserSetting<T>.Set(SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum)), key.ToUInt16(null), value);
	}

	public static void Set(ushort typeHash, ushort val, T value)
	{
		UserSetting<T>.GetHandler().Save(SettingsKeyGenerator.TypeValueToKey(typeHash, val), value);
		int count = UserSetting<T>.Listeners.Count;
		for (int i = 0; i < count; i++)
		{
			SettingChangeListener settingChangeListener = UserSetting<T>.Listeners[i];
			if (settingChangeListener.TypeHash == typeHash && settingChangeListener.NumericalValue == val)
			{
				try
				{
					settingChangeListener.Event?.Invoke(value);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}

	private static void AddListener(SettingChangeListener newListener)
	{
		foreach (SettingChangeListener listener in UserSetting<T>.Listeners)
		{
			if (listener.Equals(newListener))
			{
				return;
			}
		}
		UserSetting<T>.Listeners.Add(newListener);
	}

	public static void AddListener<TEnum>(TEnum key, Action<T> listenerEvent) where TEnum : Enum, IConvertible
	{
		UserSetting<T>.AddListener(new SettingChangeListener(key, listenerEvent));
	}

	public static void AddListener(ushort typeHash, ushort numValue, Action<T> listenerEvent)
	{
		UserSetting<T>.AddListener(new SettingChangeListener(typeHash, numValue, listenerEvent));
	}

	public static void RemoveListener<TEnum>(TEnum key, Action<T> listenerEvent) where TEnum : Enum, IConvertible
	{
		SettingChangeListener settingChangeListener = new SettingChangeListener(key, listenerEvent);
		int count = UserSetting<T>.Listeners.Count;
		for (int i = 0; i < count; i++)
		{
			if (settingChangeListener.Equals(UserSetting<T>.Listeners[i]))
			{
				UserSetting<T>.Listeners.RemoveAt(i);
				break;
			}
		}
	}

	public static void RemoveListener(Action<T> listenerEvent)
	{
		UserSetting<T>.Listeners.RemoveAll((SettingChangeListener x) => x.Event == listenerEvent);
	}

	public static void SetDefaultValue(ushort type, ushort val, T defaultValue)
	{
		UserSetting<T>.DefaultValues[UserSetting<T>.GetDefValueHash(type, val)] = defaultValue;
	}

	public static void SetDefaultValue<TEnum>(TEnum key, T defaultValue) where TEnum : Enum, IConvertible
	{
		ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(typeof(TEnum));
		ushort val = key.ToUInt16(null);
		UserSetting<T>.SetDefaultValue(stableTypeHash, val, defaultValue);
	}

	private static T GetDefaultValue(ushort type, ushort val)
	{
		if (!UserSetting<T>.DefaultValues.TryGetValue(UserSetting<T>.GetDefValueHash(type, val), out var value))
		{
			value = default(T);
		}
		return value;
	}

	private static uint GetDefValueHash(ushort type, ushort val)
	{
		return (uint)(type | (val << 16));
	}
}
