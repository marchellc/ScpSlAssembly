using System;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelRevolverExtension : MonoBehaviour, IViewmodelExtension
{
	[SerializeField]
	private WorldmodelRevolverExtension.RoundsSet[] _roundsSets;

	[SerializeField]
	private int _cockedOffset;

	[SerializeField]
	private int _insertionOffset;

	[SerializeField]
	private AnimatorLayerMask _inspectTriggerOverrides;

	[SerializeField]
	private float _inspectTriggerWeightScale;

	private RevolverClipReloaderModule _clipModule;

	private CylinderAmmoModule _cylinderModule;

	private DoubleActionModule _doubleActionModule;

	private Action<int, float> _setWeightAction;

	private int _prevWithheld;

	private ushort _serial;

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		viewmodel.ParentFirearm.TryGetModules<CylinderAmmoModule, RevolverClipReloaderModule, DoubleActionModule>(out this._cylinderModule, out this._clipModule, out this._doubleActionModule);
		this._clipModule.OnWithheld += OnAmmoWithheld;
		this._clipModule.OnAmmoInserted += OnAmmoInserted;
		this._setWeightAction = viewmodel.AnimatorSetLayerWeight;
		this._serial = viewmodel.ItemId.SerialNumber;
		WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
		for (int i = 0; i < roundsSets.Length; i++)
		{
			roundsSets[i].Init(viewmodel.ParentFirearm);
		}
	}

	private void OnDisable()
	{
		this._prevWithheld = 0;
	}

	private void OnAmmoInserted(int amt)
	{
		WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
		for (int i = 0; i < roundsSets.Length; i++)
		{
			roundsSets[i].UpdateAmount(amt, this._insertionOffset);
		}
		this._prevWithheld = 0;
	}

	private void OnAmmoWithheld()
	{
		int withheldAmmo = this._clipModule.WithheldAmmo;
		if (withheldAmmo > this._prevWithheld)
		{
			WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
			for (int i = 0; i < roundsSets.Length; i++)
			{
				roundsSets[i].UpdateAmount(withheldAmmo, this._insertionOffset);
			}
			this._prevWithheld = withheldAmmo;
		}
	}

	private void LateUpdate()
	{
		this._inspectTriggerOverrides.SetWeight(this._setWeightAction, Mathf.Clamp01(this._doubleActionModule.TriggerPullProgress * this._inspectTriggerWeightScale));
		if (!this._clipModule.IsReloading && !this._clipModule.IsUnloading)
		{
			int offset = (this._doubleActionModule.Cocked ? this._cockedOffset : 0);
			WorldmodelRevolverExtension.RoundsSet[] roundsSets = this._roundsSets;
			for (int i = 0; i < roundsSets.Length; i++)
			{
				roundsSets[i].UpdateAmount(this._serial, offset);
			}
		}
	}
}
