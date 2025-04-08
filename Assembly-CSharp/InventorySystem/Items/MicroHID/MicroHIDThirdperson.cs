using System;
using AnimatorLayerManagement;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Thirdperson;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDThirdperson : ThirdpersonItemBase, ILookatModifier
	{
		private ushort Serial
		{
			get
			{
				return base.ItemId.SerialNumber;
			}
		}

		internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
		{
			base.Initialize(subcontroller, id);
			base.SetAnim(AnimState3p.Override0, this._animIdle);
			base.SetAnim(AnimState3p.Override1, this._animWindup);
			base.SetAnim(AnimState3p.Override2, this._animShoot);
			this._prevPhase = MicroHidPhase.Standby;
			this._cycle = CycleSyncModule.GetCycleController(this.Serial);
			this._particles.Init(this.Serial, base.OwnerHub.PlayerCameraReference);
		}

		internal override void OnAnimIK(int layerIndex, float ikScale)
		{
			base.OnAnimIK(layerIndex, ikScale);
			AnimatorLayerManager layerManager = base.TargetModel.LayerManager;
			if (layerIndex != layerManager.GetLayerIndex(this._leftHandIkLayer))
			{
				return;
			}
			Animator animator = base.TargetModel.Animator;
			Vector3 vector;
			Quaternion quaternion;
			this._leftHandIkTarget.GetPositionAndRotation(out vector, out quaternion);
			animator.SetIKPosition(AvatarIKGoal.LeftHand, vector);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, quaternion);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
		}

		public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			ThirdpersonLayerWeight weightForLayer = this._idleLayerProcessor.GetWeightForLayer(this, layer);
			ThirdpersonLayerWeight weightForLayer2 = this._aimLayerProcessor.GetWeightForLayer(this, layer);
			return ThirdpersonLayerWeight.Lerp(weightForLayer, weightForLayer2, this._lastWindupProgress);
		}

		protected override void Update()
		{
			base.Update();
			this._lastWindupProgress = WindupSyncModule.GetProgress(this.Serial);
			base.OverrideBlend = Mathf.Lerp(base.OverrideBlend, 0f, Time.deltaTime * this._blendDecayLerpSpeed);
			this.UpdatePhase();
			this._prevPhase = this._cycle.Phase;
		}

		private void UpdatePhase()
		{
			switch (this._cycle.Phase)
			{
			case MicroHidPhase.WindingUp:
				if (this._prevPhase != MicroHidPhase.WindingUp)
				{
					this._windupElapsed = 0f;
					base.ReplayOverrideBlend(false);
					base.SetAnim(AnimState3p.Override0, this._animIdle);
				}
				base.OverrideBlend = 1f;
				this._windupElapsed += Time.deltaTime;
				return;
			case MicroHidPhase.WindingDown:
				if (this._windupElapsed >= this._minWindupTime)
				{
					base.OverrideBlend = 0f;
					if (this._prevPhase != MicroHidPhase.WindingDown)
					{
						base.SetAnim(AnimState3p.Override0, this._animWinddown);
						base.ReplayOverrideBlend(false);
						return;
					}
				}
				break;
			case MicroHidPhase.WoundUpSustain:
				base.OverrideBlend = 1f;
				return;
			case MicroHidPhase.Firing:
				base.OverrideBlend = 2f;
				break;
			default:
				return;
			}
		}

		public LookatData ProcessLookat(LookatData data)
		{
			float num = 1f - Mathf.Clamp01(this._lastWindupProgress * 3f);
			float num2 = num * num * num;
			data.BodyWeight = Mathf.Lerp(data.BodyWeight + 0.5f, 1f, 1f - num2);
			return data;
		}

		[SerializeField]
		private MicroHIDParticles _particles;

		[SerializeField]
		private LayerProcessorBase _idleLayerProcessor;

		[SerializeField]
		private LayerProcessorBase _aimLayerProcessor;

		[SerializeField]
		private AnimationClip _animIdle;

		[SerializeField]
		private AnimationClip _animWindup;

		[SerializeField]
		private AnimationClip _animWinddown;

		[SerializeField]
		private AnimationClip _animShoot;

		[SerializeField]
		private Transform _leftHandIkTarget;

		[SerializeField]
		private LayerRefId _leftHandIkLayer;

		[SerializeField]
		private float _minWindupTime;

		[SerializeField]
		private float _blendDecayLerpSpeed;

		private float _windupElapsed;

		private MicroHidPhase _prevPhase;

		private CycleController _cycle;

		private float _lastWindupProgress;

		private const float WindingBlend = 1f;

		private const float ShootingBlend = 2f;
	}
}
