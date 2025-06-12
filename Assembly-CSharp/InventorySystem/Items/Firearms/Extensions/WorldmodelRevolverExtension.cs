using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class WorldmodelRevolverExtension : MonoBehaviour, IWorldmodelExtension, IDestroyExtensionReceiver
{
	[Serializable]
	public class RoundsSet
	{
		[SerializeField]
		private AttachmentLink _attachment;

		[SerializeField]
		private GameObject[] _liveRounds;

		[SerializeField]
		private GameObject[] _dischargedRounds;

		private bool _worldmodelMode;

		private bool _attachmentActive;

		private uint? _filter;

		public void Init(Firearm fa)
		{
			this._attachment.InitCache(fa);
			this._worldmodelMode = false;
		}

		public void Init(FirearmWorldmodel worldmodel)
		{
			this._worldmodelMode = true;
			if (!this._filter.HasValue)
			{
				this._attachment.TryGetFilter(worldmodel.Identifier.TypeId, out var filter);
				this._filter = filter;
			}
			this._attachmentActive = (worldmodel.AttachmentCode & this._filter.Value) != 0;
		}

		public void UpdateAmount(int amt, int offset)
		{
			if (this.Setup())
			{
				for (int i = 0; i < amt; i++)
				{
					this.GetSafe(this._liveRounds, i + offset).SetActive(value: true);
				}
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
				int index = i + offset;
				switch (chambersArrayForSerial[i].ContextState)
				{
				case CylinderAmmoModule.ChamberState.Live:
					this.GetSafe(this._liveRounds, index).SetActive(value: true);
					break;
				case CylinderAmmoModule.ChamberState.Discharged:
					this.GetSafe(this._dischargedRounds, index).SetActive(value: true);
					break;
				}
			}
		}

		private bool Setup()
		{
			GameObject[] liveRounds = this._liveRounds;
			for (int i = 0; i < liveRounds.Length; i++)
			{
				liveRounds[i].SetActive(value: false);
			}
			liveRounds = this._dischargedRounds;
			for (int i = 0; i < liveRounds.Length; i++)
			{
				liveRounds[i].SetActive(value: false);
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
	}

	[SerializeField]
	private RoundsSet[] _roundsSets;

	[SerializeField]
	private int _chambersOffset;

	[SerializeField]
	private BipolarTransform _hammer;

	private ushort _serial;

	private bool _eventsAssigned;

	public void OnDestroyExtension()
	{
		if (this._eventsAssigned)
		{
			DoubleActionModule.OnCockedChanged -= OnCockedChanged;
			CylinderAmmoModule.OnChambersModified -= UpdateChambers;
		}
	}

	public void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		RoundsSet[] roundsSets = this._roundsSets;
		for (int i = 0; i < roundsSets.Length; i++)
		{
			roundsSets[i].Init(worldmodel);
		}
		this._serial = worldmodel.Identifier.SerialNumber;
		this._hammer.Polarity = DoubleActionModule.GetCocked(this._serial);
		if (!this._eventsAssigned)
		{
			this._eventsAssigned = true;
			DoubleActionModule.OnCockedChanged += OnCockedChanged;
			CylinderAmmoModule.OnChambersModified += UpdateChambers;
			this.UpdateChambers(worldmodel.Identifier.SerialNumber);
		}
	}

	private void UpdateChambers(ushort serial)
	{
		RoundsSet[] roundsSets = this._roundsSets;
		for (int i = 0; i < roundsSets.Length; i++)
		{
			roundsSets[i].UpdateAmount(serial, this._chambersOffset);
		}
	}

	private void OnCockedChanged(ushort serial, bool cocked)
	{
		if (serial == this._serial)
		{
			this._hammer.Polarity = cocked;
		}
	}
}
