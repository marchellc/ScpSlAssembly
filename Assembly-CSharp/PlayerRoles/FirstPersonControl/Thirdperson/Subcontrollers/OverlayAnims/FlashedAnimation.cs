using CustomPlayerEffects;
using InventorySystem.Disarming;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims;

public class FlashedAnimation : OverlayAnimationsBase
{
	private const float DecayDuration = 1.7f;

	private bool _hasEffect;

	private float _remainingSustain;

	private Flashed _effect;

	private bool EffectEnabled
	{
		get
		{
			if (this._hasEffect)
			{
				return this._effect.IsEnabled;
			}
			return false;
		}
	}

	public override bool WantsToPlay
	{
		get
		{
			if (!(this._remainingSustain > 0f))
			{
				return this.EffectEnabled;
			}
			return true;
		}
	}

	public override bool Bypassable => !this.EffectEnabled;

	public override AnimationClip Clip => base.Controller.FlashedLoopClip;

	public override float GetLayerWeight(AnimItemLayer3p layer)
	{
		return Mathf.Clamp01(this._remainingSustain / 1.7f);
	}

	public override void UpdateActive()
	{
		base.UpdateActive();
		if (base.Model.OwnerHub.inventory.IsDisarmed())
		{
			this._remainingSustain = 0f;
		}
		else if (this.EffectEnabled)
		{
			this._remainingSustain = 1.7f;
		}
		else
		{
			this._remainingSustain -= Time.deltaTime;
		}
	}

	public override void OnStopped()
	{
		base.OnStopped();
		this._remainingSustain = 0f;
	}

	public override void OnReassigned()
	{
		base.OnReassigned();
		this._hasEffect = base.Model.OwnerHub.playerEffectsController.TryGetEffect<Flashed>(out this._effect);
	}

	public override void OnReset()
	{
		base.OnReset();
		this._hasEffect = false;
		this._remainingSustain = 0f;
	}
}
