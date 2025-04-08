using System;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class ExternalRoundsExtension : MixedExtension
	{
		private void Update()
		{
			Func<DisplayAmmoValues> fetcher = this._fetcher;
			DisplayAmmoValues displayAmmoValues = ((fetcher != null) ? fetcher() : default(DisplayAmmoValues));
			int num = 0;
			if (this._countChamberedRounds)
			{
				num += displayAmmoValues.Chambered;
			}
			if (this._countMagazineRounds)
			{
				num += displayAmmoValues.Magazines;
			}
			if (this._prevValue != null)
			{
				int num2 = num;
				int? prevValue = this._prevValue;
				if ((num2 == prevValue.GetValueOrDefault()) & (prevValue != null))
				{
					return;
				}
			}
			this._prevValue = new int?(num);
			for (int i = 0; i < this._rounds.Length; i++)
			{
				this._rounds[i].SetActive(i < num);
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

		[SerializeField]
		private GameObject[] _rounds;

		[SerializeField]
		private bool _countChamberedRounds;

		[SerializeField]
		private bool _countMagazineRounds;

		private Func<DisplayAmmoValues> _fetcher;

		private int? _prevValue;
	}
}
