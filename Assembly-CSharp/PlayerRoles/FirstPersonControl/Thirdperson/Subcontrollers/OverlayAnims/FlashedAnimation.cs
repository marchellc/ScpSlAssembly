using System;
using CustomPlayerEffects;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims
{
	public class FlashedAnimation : OverlayAnimationsBase
	{
		private bool EffectEnabled
		{
			get
			{
				return this._hasEffect && this._effect.IsEnabled;
			}
		}

		public override bool WantsToPlay
		{
			get
			{
				return this._remainingSustain > 0f || this.EffectEnabled;
			}
		}

		public override bool Bypassable
		{
			get
			{
				return !this.EffectEnabled;
			}
		}

		public override AnimationClip Clip
		{
			get
			{
				return base.Controller.FlashedLoopClip;
			}
		}

		public override float GetLayerWeight(AnimItemLayer3p layer)
		{
			return Mathf.Clamp01(this._remainingSustain / 1.7f);
		}

		public override void UpdateActive()
		{
			base.UpdateActive();
			if (this.EffectEnabled)
			{
				this._remainingSustain = 1.7f;
				return;
			}
			this._remainingSustain -= Time.deltaTime;
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

		private const float DecayDuration = 1.7f;

		private bool _hasEffect;

		private float _remainingSustain;

		private Flashed _effect;
	}
}
