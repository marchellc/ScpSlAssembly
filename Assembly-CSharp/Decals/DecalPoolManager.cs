using System;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace Decals;

public class DecalPoolManager : MonoBehaviour
{
	[Serializable]
	private class DecalCollection
	{
		[SerializeField]
		private Decal[] _templates;

		[SerializeField]
		private PerformanceVideoSetting _enabledSetting;

		[SerializeField]
		private PerformanceVideoSetting _limitSetting;

		private DecalPool[] _pools;

		private int _decalLimit;

		private bool _decalsEnabled;

		public void Setup()
		{
			this._pools = new DecalPool[this._templates.Length];
			for (int i = 0; i < this._pools.Length; i++)
			{
				this._pools[i] = new DecalPool(this._templates[i]);
			}
			UserSetting<bool>.AddListener(this._enabledSetting, OnSettingToggled);
			UserSetting<float>.AddListener(this._limitSetting, OnLimitChanged);
			this._decalsEnabled = UserSetting<bool>.Get(this._enabledSetting);
			this._decalLimit = (int)UserSetting<float>.Get(this._limitSetting);
		}

		public void UnlinkListeners()
		{
			UserSetting<bool>.RemoveListener(this._enabledSetting, OnSettingToggled);
			UserSetting<float>.RemoveListener(this._limitSetting, OnLimitChanged);
		}

		public bool TryGet(DecalPoolType type, out Decal decal)
		{
			int num = 0;
			int num2 = 0;
			DecalPool decalPool = null;
			DecalPool decalPool2 = null;
			for (int i = 0; i < this._pools.Length; i++)
			{
				DecalPool decalPool3 = this._pools[i];
				int instances = decalPool3.Instances;
				num += instances;
				if (decalPool3.Type == type)
				{
					decalPool = decalPool3;
				}
				else if (instances > num2)
				{
					decalPool2 = decalPool3;
					num2 = instances;
				}
			}
			if (decalPool == null)
			{
				decal = null;
				return false;
			}
			while (num >= this._decalLimit)
			{
				if (decalPool2 == null || decalPool2.Instances == 0)
				{
					decalPool.DisableLast();
				}
				else
				{
					decalPool2.DisableLast();
				}
				num--;
			}
			decal = decalPool.Get();
			return true;
		}

		internal void Clear(DecalPoolType decalPoolType, int amount)
		{
			DecalPool[] pools = this._pools;
			foreach (DecalPool decalPool in pools)
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
	}

	private static DecalPoolManager _singleton;

	private static bool _singletonSet;

	[SerializeField]
	private DecalCollection[] _collections;

	private void Awake()
	{
		DecalPoolManager._singleton = this;
		DecalPoolManager._singletonSet = true;
		this._collections.ForEach(delegate(DecalCollection x)
		{
			x.Setup();
		});
	}

	private void OnDestroy()
	{
		DecalPoolManager._singletonSet = false;
		this._collections.ForEach(delegate(DecalCollection x)
		{
			x.UnlinkListeners();
		});
	}

	public static bool TryGet(DecalPoolType poolType, out Decal decal)
	{
		if (DecalPoolManager._singletonSet)
		{
			DecalCollection[] collections = DecalPoolManager._singleton._collections;
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
		DecalCollection[] collections = DecalPoolManager._singleton._collections;
		for (int i = 0; i < collections.Length; i++)
		{
			collections[i].Clear(decalPoolType, amount);
		}
	}
}
