using System;

namespace UserSettings
{
	public class CachedUserSetting<TCachedValue>
	{
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
			ushort stableTypeHash = SettingsKeyGenerator.GetStableTypeHash(trackedEnum.GetType(), false);
			ushort num = ((IConvertible)trackedEnum).ToUInt16(null);
			this.Setup(stableTypeHash, num);
		}

		public CachedUserSetting(ushort trackedEnumType, ushort trackedEnumKey)
		{
			this.Setup(trackedEnumType, trackedEnumKey);
		}

		private void Setup(ushort trackedEnumType, ushort trackedEnumKey)
		{
			this._enumType = trackedEnumType;
			this._enumKey = trackedEnumKey;
			UserSetting<TCachedValue>.AddListener(trackedEnumType, trackedEnumKey, new Action<TCachedValue>(this.UpdateValue));
		}

		public void Destroy()
		{
			UserSetting<TCachedValue>.RemoveListener(new Action<TCachedValue>(this.UpdateValue));
		}

		private void UpdateValue(TCachedValue val)
		{
			this._cachedValue = val;
			this._wasEverSet = true;
		}

		private TCachedValue _cachedValue;

		private ushort _enumType;

		private ushort _enumKey;

		private bool _wasEverSet;
	}
}
