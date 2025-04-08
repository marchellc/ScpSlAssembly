using System;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace Decals
{
	public class DecalPoolManager : MonoBehaviour
	{
		private void Awake()
		{
			DecalPoolManager._singleton = this;
			DecalPoolManager._singletonSet = true;
			this._collections.ForEach(delegate(DecalPoolManager.DecalCollection x)
			{
				x.Setup();
			});
		}

		private void OnDestroy()
		{
			DecalPoolManager._singletonSet = false;
			this._collections.ForEach(delegate(DecalPoolManager.DecalCollection x)
			{
				x.UnlinkListeners();
			});
		}

		public static bool TryGet(DecalPoolType poolType, out Decal decal)
		{
			if (DecalPoolManager._singletonSet)
			{
				DecalPoolManager.DecalCollection[] collections = DecalPoolManager._singleton._collections;
				for (int i = 0; i < collections.Length; i++)
				{
					if (collections[i].TryGet(poolType, out decal))
					{
						return true;
					}
				}
			}
			decal = null;
			return false;
		}

		public static void ClientClear(DecalPoolType decalPoolType, int amount)
		{
			DecalPoolManager.DecalCollection[] collections = DecalPoolManager._singleton._collections;
			for (int i = 0; i < collections.Length; i++)
			{
				collections[i].Clear(decalPoolType, amount);
			}
		}

		private static DecalPoolManager _singleton;

		private static bool _singletonSet;

		[SerializeField]
		private DecalPoolManager.DecalCollection[] _collections;

		[Serializable]
		private class DecalCollection
		{
			public void Setup()
			{
				this._pools = new DecalPool[this._templates.Length];
				for (int i = 0; i < this._pools.Length; i++)
				{
					this._pools[i] = new DecalPool(this._templates[i]);
				}
				UserSetting<bool>.AddListener<PerformanceVideoSetting>(this._enabledSetting, new Action<bool>(this.OnSettingToggled));
				UserSetting<float>.AddListener<PerformanceVideoSetting>(this._limitSetting, new Action<float>(this.OnLimitChanged));
				this._decalsEnabled = UserSetting<bool>.Get<PerformanceVideoSetting>(this._enabledSetting);
				this._decalLimit = (int)UserSetting<float>.Get<PerformanceVideoSetting>(this._limitSetting);
			}

			public void UnlinkListeners()
			{
				UserSetting<bool>.RemoveListener<PerformanceVideoSetting>(this._enabledSetting, new Action<bool>(this.OnSettingToggled));
				UserSetting<float>.RemoveListener<PerformanceVideoSetting>(this._limitSetting, new Action<float>(this.OnLimitChanged));
			}

			public bool TryGet(DecalPoolType type, out Decal decal)
			{
				int i = 0;
				int num = 0;
				DecalPool decalPool = null;
				DecalPool decalPool2 = null;
				for (int j = 0; j < this._pools.Length; j++)
				{
					DecalPool decalPool3 = this._pools[j];
					int instances = decalPool3.Instances;
					i += instances;
					if (decalPool3.Type == type)
					{
						decalPool = decalPool3;
					}
					else if (instances > num)
					{
						decalPool2 = decalPool3;
						num = instances;
					}
				}
				if (decalPool == null)
				{
					decal = null;
					return false;
				}
				while (i >= this._decalLimit)
				{
					if (decalPool2 == null || decalPool2.Instances == 0)
					{
						decalPool.DisableLast();
					}
					else
					{
						decalPool2.DisableLast();
					}
					i--;
				}
				decal = decalPool.Get();
				return true;
			}

			internal void Clear(DecalPoolType decalPoolType, int amount)
			{
				foreach (DecalPool decalPool in this._pools)
				{
					if (decalPool.Type == decalPoolType)
					{
						decalPool.SetLimit(Mathf.Max(decalPool.Instances - amount, 0));
					}
				}
			}

			private void OnLimitChanged(float limit)
			{
				this._decalLimit = (int)limit;
				this.RefreshLimits();
			}

			private void OnSettingToggled(bool status)
			{
				this._decalsEnabled = status;
				this.RefreshLimits();
			}

			private void RefreshLimits()
			{
				int limit = (this._decalsEnabled ? this._decalLimit : 0);
				this._pools.ForEach(delegate(DecalPool x)
				{
					x.SetLimit(limit);
				});
			}

			[SerializeField]
			private Decal[] _templates;

			[SerializeField]
			private PerformanceVideoSetting _enabledSetting;

			[SerializeField]
			private PerformanceVideoSetting _limitSetting;

			private DecalPool[] _pools;

			private int _decalLimit;

			private bool _decalsEnabled;
		}
	}
}
