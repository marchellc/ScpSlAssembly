using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class SubsequentShotsInaccuracyModule : ModuleBase, IInaccuracyProviderModule
	{
		public float Inaccuracy { get; private set; }

		private void Update()
		{
			if (this._shotsCounter == null)
			{
				return;
			}
			this.Inaccuracy = this._optionalModifier.Process(this._inaccuracyNextFrame);
			this._shotsCounter.Update();
			SubsequentShotsInaccuracyModule.InaccuracySettings orAdd = SubsequentShotsInaccuracyModule.SettingsByCategory.GetOrAdd(base.Firearm.FirearmCategory, () => SubsequentShotsInaccuracyModule.DefaultSettings);
			this._inaccuracyNextFrame = Mathf.InverseLerp((float)orAdd.Offset, (float)orAdd.ShotsToReach, (float)this._shotsCounter.SubsequentShots) * orAdd.TargetDeviation;
		}

		private void OnDestroy()
		{
			SubsequentShotsCounter shotsCounter = this._shotsCounter;
			if (shotsCounter == null)
			{
				return;
			}
			shotsCounter.Destruct();
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			this._shotsCounter = new SubsequentShotsCounter(base.Firearm, 1f, 0.1f, 0.4f);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static SubsequentShotsInaccuracyModule()
		{
			Dictionary<FirearmCategory, SubsequentShotsInaccuracyModule.InaccuracySettings> dictionary = new Dictionary<FirearmCategory, SubsequentShotsInaccuracyModule.InaccuracySettings>();
			dictionary[FirearmCategory.Pistol] = new SubsequentShotsInaccuracyModule.InaccuracySettings(0.9f, 3, 0);
			dictionary[FirearmCategory.Revolver] = new SubsequentShotsInaccuracyModule.InaccuracySettings(0.9f, 1, 0);
			dictionary[FirearmCategory.SubmachineGun] = new SubsequentShotsInaccuracyModule.InaccuracySettings(0.56f, 15, 5);
			dictionary[FirearmCategory.LightMachineGun] = new SubsequentShotsInaccuracyModule.InaccuracySettings(0.56f, 12, 3);
			dictionary[FirearmCategory.Rifle] = new SubsequentShotsInaccuracyModule.InaccuracySettings(0.68f, 8, 2);
			SubsequentShotsInaccuracyModule.SettingsByCategory = dictionary;
		}

		private static readonly SubsequentShotsInaccuracyModule.InaccuracySettings DefaultSettings = new SubsequentShotsInaccuracyModule.InaccuracySettings(1f, 10, 1);

		private static readonly Dictionary<FirearmCategory, SubsequentShotsInaccuracyModule.InaccuracySettings> SettingsByCategory;

		private SubsequentShotsCounter _shotsCounter;

		private float _inaccuracyNextFrame;

		[SerializeField]
		private StatModifier _optionalModifier;

		private class InaccuracySettings
		{
			public InaccuracySettings(float targetDeviation, int shotsToReach, int offset)
			{
				this.TargetDeviation = targetDeviation;
				this.ShotsToReach = shotsToReach;
				this.Offset = offset;
			}

			public float TargetDeviation;

			public int ShotsToReach;

			public int Offset;
		}
	}
}
