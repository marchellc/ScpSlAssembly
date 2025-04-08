using System;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class WorldmodelAutomaticActionExtension : MonoBehaviour, IWorldmodelExtension
	{
		public void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			this._lastSerial = worldmodel.Identifier.SerialNumber;
			this.UpdateAllPolarity();
		}

		private void UpdateAllPolarity()
		{
			int num;
			bool flag;
			bool flag2;
			AutomaticActionModule.DecodeSyncFlags(this._lastSerial, out num, out flag, out flag2);
			this.UpdatePolarity(this._anyChambered, num > 0);
			this.UpdatePolarity(this._boltLocked, flag);
			this.UpdatePolarity(this._cocked, flag2);
		}

		private void UpdatePolarity(BipolarTransform[] arr, bool polarity)
		{
			for (int i = 0; i < arr.Length; i++)
			{
				arr[i].Polarity = polarity;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			AutomaticActionModule.OnSyncDataReceived += delegate(ushort serial)
			{
				FirearmWorldmodel firearmWorldmodel;
				if (!FirearmWorldmodel.Instances.TryGetValue(serial, out firearmWorldmodel))
				{
					return;
				}
				WorldmodelAutomaticActionExtension worldmodelAutomaticActionExtension;
				if (!firearmWorldmodel.TryGetExtension<WorldmodelAutomaticActionExtension>(out worldmodelAutomaticActionExtension))
				{
					return;
				}
				worldmodelAutomaticActionExtension.UpdateAllPolarity();
			};
		}

		private ushort _lastSerial;

		[SerializeField]
		private BipolarTransform[] _anyChambered;

		[SerializeField]
		private BipolarTransform[] _boltLocked;

		[SerializeField]
		private BipolarTransform[] _cocked;
	}
}
