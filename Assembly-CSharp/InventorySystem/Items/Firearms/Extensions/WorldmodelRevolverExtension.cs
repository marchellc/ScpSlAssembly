using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class WorldmodelRevolverExtension : MonoBehaviour, IWorldmodelExtension, IDestroyExtensionReceiver
	{
		public void OnDestroyExtension()
		{
			if (!this._eventsAssigned)
			{
				return;
			}
			DoubleActionModule.OnCockedChanged -= this.OnCockedChanged;
			CylinderAmmoModule.OnChambersModified -= this.UpdateChambers;
		}

		public void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
			for (int i = 0; i < roundsSets.Length; i++)
			{
				roundsSets[i].Init(worldmodel);
			}
			this._serial = worldmodel.Identifier.SerialNumber;
			this._hammer.Polarity = DoubleActionModule.GetCocked(this._serial);
			if (this._eventsAssigned)
			{
				return;
			}
			this._eventsAssigned = true;
			DoubleActionModule.OnCockedChanged += this.OnCockedChanged;
			CylinderAmmoModule.OnChambersModified += this.UpdateChambers;
			this.UpdateChambers(worldmodel.Identifier.SerialNumber);
		}

		private void UpdateChambers(ushort serial)
		{
			WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
			for (int i = 0; i < roundsSets.Length; i++)
			{
				roundsSets[i].UpdateAmount(serial, this._chambersOffset);
			}
		}

		private void OnCockedChanged(ushort serial, bool cocked)
		{
			if (serial != this._serial)
			{
				return;
			}
			this._hammer.Polarity = cocked;
		}

		[SerializeField]
		private WorldmodelRevolverExtension.RoundsSet[] _roundsSets;

		[SerializeField]
		private int _chambersOffset;

		[SerializeField]
		private BipolarTransform _hammer;

		private ushort _serial;

		private bool _eventsAssigned;

		[Serializable]
		public class RoundsSet
		{
			public void Init(Firearm fa)
			{
				this._attachment.InitCache(fa);
				this._worldmodelMode = false;
			}

			public void Init(FirearmWorldmodel worldmodel)
			{
				this._worldmodelMode = true;
				if (this._filter == null)
				{
					uint num;
					this._attachment.TryGetFilter(worldmodel.Identifier.TypeId, out num);
					this._filter = new uint?(num);
				}
				this._attachmentActive = (worldmodel.AttachmentCode & this._filter.Value) > 0U;
			}

			public void UpdateAmount(int amt, int offset)
			{
				if (!this.Setup())
				{
					return;
				}
				for (int i = 0; i < amt; i++)
				{
					this.GetSafe(this._liveRounds, i + offset).SetActive(true);
				}
			}

			public void UpdateAmount(ushort serial, int offset)
			{
				if (!this.Setup())
				{
					return;
				}
				int num = Mathf.Min(this._liveRounds.Length, this._dischargedRounds.Length);
				CylinderAmmoModule.Chamber[] chambersArrayForSerial = CylinderAmmoModule.GetChambersArrayForSerial(serial, num);
				for (int i = 0; i < num; i++)
				{
					int num2 = i + offset;
					CylinderAmmoModule.ChamberState contextState = chambersArrayForSerial[i].ContextState;
					if (contextState != CylinderAmmoModule.ChamberState.Live)
					{
						if (contextState == CylinderAmmoModule.ChamberState.Discharged)
						{
							this.GetSafe(this._dischargedRounds, num2).SetActive(true);
						}
					}
					else
					{
						this.GetSafe(this._liveRounds, num2).SetActive(true);
					}
				}
			}

			private bool Setup()
			{
				GameObject[] array = this._liveRounds;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(false);
				}
				array = this._dischargedRounds;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(false);
				}
				if (!this._worldmodelMode)
				{
					return this._attachment.Instance.IsEnabled;
				}
				return this._attachmentActive;
			}

			private GameObject GetSafe(GameObject[] arr, int index)
			{
				int num = arr.Length;
				return arr[(index % num + num) % num];
			}

			[SerializeField]
			private AttachmentLink _attachment;

			[SerializeField]
			private GameObject[] _liveRounds;

			[SerializeField]
			private GameObject[] _dischargedRounds;

			private bool _worldmodelMode;

			private bool _attachmentActive;

			private uint? _filter;
		}
	}
}
