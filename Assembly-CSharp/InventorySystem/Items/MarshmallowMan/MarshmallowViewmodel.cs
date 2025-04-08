using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.MarshmallowMan
{
	public class MarshmallowViewmodel : StandardAnimatedViemodel
	{
		private void Awake()
		{
			this._curAttackVariant = global::UnityEngine.Random.Range(0, this._attackVariants);
			MarshmallowItem.OnSwing += this.OnSwing;
			MarshmallowItem.OnHolsterRequested += this.OnHolsterRequested;
		}

		private void OnDestroy()
		{
			MarshmallowItem.OnSwing -= this.OnSwing;
			MarshmallowItem.OnHolsterRequested -= this.OnHolsterRequested;
		}

		private void Update()
		{
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			float num = (base.Hub.IsGrounded() ? base.Hub.GetVelocity().MagnitudeIgnoreY() : 0f);
			this._prevVel = Mathf.MoveTowards(this._prevVel, num, Time.deltaTime * 28.5f);
			this.AnimatorSetFloat(MarshmallowViewmodel.WalkCycleHash, animatedCharacterModel.WalkCycle);
			this.AnimatorSetLayerWeight(2, Mathf.Clamp01(this._prevVel / 7.5f));
		}

		private void OnHolsterRequested(ushort serial)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.AnimatorSetTrigger(MarshmallowViewmodel.HolsterTriggerHash);
		}

		private void OnSwing(ushort serial)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this._curAttackVariant++;
			this.AnimatorSetFloat(MarshmallowViewmodel.AttackVariantHash, (float)(this._curAttackVariant % this._attackVariants));
			this.AnimatorSetTrigger(MarshmallowViewmodel.AttackTriggerHash);
		}

		public override void InitLocal(ItemBase ib)
		{
			base.InitLocal(ib);
			base.GetComponent<MarshmallowAudio>().Setup(ib.ItemSerial, ib.Owner.transform);
		}

		[SerializeField]
		private int _attackVariants;

		private int _curAttackVariant;

		private float _prevVel;

		private static readonly int AttackVariantHash = Animator.StringToHash("AttackVariant");

		private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

		private static readonly int HolsterTriggerHash = Animator.StringToHash("Holster");

		private static readonly int WalkCycleHash = Animator.StringToHash("WalkCycle");

		private const float VelAdjustSpeed = 28.5f;

		private const float WalkMaxVel = 7.5f;

		private const int WalkLayer = 2;
	}
}
