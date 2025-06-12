using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127BadAimVoiceTrigger : Scp127VoiceTriggerBase
{
	[SerializeField]
	private Scp127VoiceLineCollection _voiceLine;

	private int _missedAmmo;

	private Scp127MagazineModule _magazineModule;

	private Scp127ActionModule _actionModule;

	private const float MissThreshold = 0.8f;

	protected override void OnInit()
	{
		base.OnInit();
		base.Firearm.TryGetModules<Scp127MagazineModule, Scp127ActionModule, IHitregModule>(out this._magazineModule, out this._actionModule, out var m);
		m.ServerOnFired += OnFired;
	}

	private void OnDamaged(Firearm scp127)
	{
		if (!(scp127 != base.Firearm))
		{
			this._missedAmmo = 0;
		}
	}

	private void OnFired()
	{
		this._missedAmmo++;
		if (this._actionModule.AmmoStored <= 0)
		{
			float num = 0.8f * (float)this._magazineModule.AmmoMax;
			if (!((float)this._missedAmmo < num))
			{
				this._missedAmmo = 0;
				base.ServerPlayVoiceLineFromCollection(this._voiceLine);
			}
		}
	}

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		Scp127TierManagerModule.ServerOnDamaged += OnDamaged;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		Scp127TierManagerModule.ServerOnDamaged -= OnDamaged;
	}
}
