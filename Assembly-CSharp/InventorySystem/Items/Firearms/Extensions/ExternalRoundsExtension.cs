using System;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ExternalRoundsExtension : MixedExtension
{
	[SerializeField]
	private GameObject[] _rounds;

	[SerializeField]
	private bool _countChamberedRounds;

	[SerializeField]
	private bool _countMagazineRounds;

	private Func<DisplayAmmoValues> _fetcher;

	private int? _prevValue;

	private void Update()
	{
		DisplayAmmoValues displayAmmoValues = this._fetcher?.Invoke() ?? default(DisplayAmmoValues);
		int num = 0;
		if (this._countChamberedRounds)
		{
			num += displayAmmoValues.Chambered;
		}
		if (this._countMagazineRounds)
		{
			num += displayAmmoValues.Magazines;
		}
		if (!this._prevValue.HasValue || num != this._prevValue)
		{
			this._prevValue = num;
			for (int i = 0; i < this._rounds.Length; i++)
			{
				this._rounds[i].SetActive(i < num);
			}
		}
	}

	public override void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		base.InitViewmodel(viewmodel);
		this._fetcher = () => IDisplayableAmmoProviderModule.GetCombinedDisplayAmmo(viewmodel.ParentFirearm);
	}

	public override void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		base.SetupWorldmodel(worldmodel);
		this._fetcher = () => IDisplayableAmmoProviderModule.GetCombinedDisplayAmmo(worldmodel.Identifier);
	}
}
