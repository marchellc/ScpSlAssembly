using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class SubsequentShotsInaccuracyModule : ModuleBase, IInaccuracyProviderModule
{
	private class InaccuracySettings
	{
		public float TargetDeviation;

		public int ShotsToReach;

		public int Offset;

		public InaccuracySettings(float targetDeviation, int shotsToReach, int offset)
		{
			this.TargetDeviation = targetDeviation;
			this.ShotsToReach = shotsToReach;
			this.Offset = offset;
		}
	}

	private static readonly InaccuracySettings DefaultSettings = new InaccuracySettings(1f, 10, 1);

	private static readonly Dictionary<FirearmCategory, InaccuracySettings> SettingsByCategory = new Dictionary<FirearmCategory, InaccuracySettings>
	{
		[FirearmCategory.Pistol] = new InaccuracySettings(0.9f, 3, 0),
		[FirearmCategory.Revolver] = new InaccuracySettings(0.9f, 1, 0),
		[FirearmCategory.SubmachineGun] = new InaccuracySettings(0.56f, 15, 5),
		[FirearmCategory.LightMachineGun] = new InaccuracySettings(0.56f, 12, 3),
		[FirearmCategory.Rifle] = new InaccuracySettings(0.68f, 8, 2)
	};

	private SubsequentShotsCounter _shotsCounter;

	private float _inaccuracyNextFrame;

	[SerializeField]
	private StatModifier _optionalModifier;

	public float Inaccuracy { get; private set; }

	private void Update()
	{
		if (this._shotsCounter != null)
		{
			this.Inaccuracy = this._optionalModifier.Process(this._inaccuracyNextFrame);
			this._shotsCounter.Update();
			InaccuracySettings orAdd = SubsequentShotsInaccuracyModule.SettingsByCategory.GetOrAdd(base.Firearm.FirearmCategory, () => SubsequentShotsInaccuracyModule.DefaultSettings);
			this._inaccuracyNextFrame = Mathf.InverseLerp(orAdd.Offset, orAdd.ShotsToReach, this._shotsCounter.SubsequentShots) * orAdd.TargetDeviation;
		}
	}

	private void OnDestroy()
	{
		this._shotsCounter?.Destruct();
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		this._shotsCounter = new SubsequentShotsCounter(base.Firearm);
	}
}
