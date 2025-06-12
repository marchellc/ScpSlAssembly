using System;

namespace UserSettings;

public class CachedUserSetting<TCachedValue>
{
	private TCachedValue _cachedValue;

	private ushort _enumType;

	private ushort _enumKey;

	private bool _wasEverSet;

	public TCachedValue Value
	{
		get
		{
			if (!this._wasEverSet)
			{
				this.UpdateValue(UserSetting<TCachedValue>.Get(this._enumType, this._enumKey));
			}
			return this._cachedValue;
		}
		set
		{
			UserSetting<TCachedValue>.Set(this._enumType, this._enumKey, value);
		}
	}

	public CachedUserSetting(Enum trackedEnum)
	{
		ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(trackedEnum.GetType());
		ushort trackedEnumKey = ((IConvertible)trackedEnum).ToUInt16((IFormatProvider)null);
		this.Setup(stableTypeHash, trackedEnumKey);
	}

	public CachedUserSetting(ushort trackedEnumType, ushort trackedEnumKey)
	{
		this.Setup(trackedEnumType, trackedEnumKey);
	}

	private void Setup(ushort trackedEnumType, ushort trackedEnumKey)
	{
		this._enumType = trackedEnumType;
		this._enumKey = trackedEnumKey;
		UserSetting<TCachedValue>.AddListener(trackedEnumType, trackedEnumKey, UpdateValue);
	}

	public void Destroy()
	{
		UserSetting<TCachedValue>.RemoveListener(UpdateValue);
	}

	private void UpdateValue(TCachedValue val)
	{
		this._cachedValue = val;
		this._wasEverSet = true;
	}
}
