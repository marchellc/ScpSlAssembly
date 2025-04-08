using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096HandsViewmodel : ScpViewmodelBase
	{
		public override float CamFOV
		{
			get
			{
				return this._fieldOfView;
			}
		}

		protected override void Start()
		{
			base.Start();
			Scp096Role scp096Role = base.Role as Scp096Role;
			this._fpc = scp096Role.FpcModule;
			this._stateController = scp096Role.StateController;
			scp096Role.SubroutineModule.TryGetSubroutine<Scp096AttackAbility>(out this._attackAbility);
			this._stateController.OnRageUpdate += this.OnRageUpdate;
			this._stateController.OnAbilityUpdate += this.OnAbilityUpdate;
			this._attackAbility.OnHitReceived += this.OnHitReceived;
			this._attackAbility.OnAttackTriggered += this.OnAttackTriggered;
			this.OnRageUpdate(this._stateController.RageState);
			this.UpdateLayerWeight(1f);
			this.UpdateAnimations();
			if (base.Owner.isLocalPlayer)
			{
				return;
			}
			if (this._stateController.RageState == Scp096RageState.Enraged)
			{
				this.SkipAnimations(15f, 5);
			}
			else
			{
				this.SkipAnimations(this._stateController.LastRageUpdate, 3);
			}
			this.UpdateWalk(true);
			if (this._stateController.AbilityState == Scp096AbilityState.None)
			{
				return;
			}
			this.OnAbilityUpdate(this._stateController.AbilityState);
			this.SkipAnimations(this._stateController.LastAbilityUpdate, 3);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			this._stateController.OnRageUpdate -= this.OnRageUpdate;
			this._stateController.OnAbilityUpdate -= this.OnAbilityUpdate;
			this._attackAbility.OnHitReceived -= this.OnHitReceived;
			this._attackAbility.OnAttackTriggered -= this.OnAttackTriggered;
		}

		private void OnAbilityUpdate(Scp096AbilityState newState)
		{
			if (newState == Scp096AbilityState.PryingGate)
			{
				base.Anim.SetTrigger(Scp096HandsViewmodel.HashPryGate);
			}
		}

		private void OnRageUpdate(Scp096RageState newState)
		{
			switch (newState)
			{
			case Scp096RageState.Docile:
				this._useEnragedLayer = false;
				return;
			case Scp096RageState.Distressed:
				base.Anim.SetTrigger(Scp096HandsViewmodel.HashEnterRage);
				this._useEnragedLayer = true;
				return;
			case Scp096RageState.Enraged:
				this._useEnragedLayer = true;
				return;
			case Scp096RageState.Calming:
				base.Anim.SetTrigger(Scp096HandsViewmodel.HashExitRage);
				this._useEnragedLayer = false;
				return;
			default:
				return;
			}
		}

		private void OnAttackTriggered()
		{
			if (!base.Owner.isLocalPlayer)
			{
				return;
			}
			base.Anim.SetBool(Scp096HandsViewmodel.HashLeftAttack, !this._attackAbility.LeftAttack);
			base.Anim.SetTrigger(Scp096HandsViewmodel.HashAttackTrigger);
		}

		private void OnHitReceived(Scp096HitResult hit)
		{
			if (base.Owner.isLocalPlayer)
			{
				return;
			}
			base.Anim.SetBool(Scp096HandsViewmodel.HashLeftAttack, this._attackAbility.LeftAttack);
			base.Anim.SetTrigger(Scp096HandsViewmodel.HashAttackTrigger);
		}

		private void UpdateLayerWeight(float maxDelta)
		{
			float layerWeight = base.Anim.GetLayerWeight(1);
			base.Anim.SetLayerWeight(1, Mathf.MoveTowards(layerWeight, (float)(this._useEnragedLayer ? 1 : 0), maxDelta));
		}

		private void UpdateWalk(bool instant)
		{
			float num;
			if (this._stateController.RageState == Scp096RageState.Enraged)
			{
				num = (float)((this._stateController.AbilityState == Scp096AbilityState.Charging) ? 1 : 0);
			}
			else
			{
				num = base.Owner.GetVelocity().magnitude / this._fpc.WalkSpeed;
			}
			if (instant)
			{
				base.Anim.SetFloat(Scp096HandsViewmodel.HashWalk, num);
				return;
			}
			base.Anim.SetFloat(Scp096HandsViewmodel.HashWalk, num, this._dampTime, Time.deltaTime);
		}

		protected override void UpdateAnimations()
		{
			this.UpdateLayerWeight(Time.deltaTime * this._weightAdjustSpeed);
			base.Anim.SetBool(Scp096HandsViewmodel.HashTryNotToCry, this._stateController.AbilityState == Scp096AbilityState.TryingNotToCry);
			this.UpdateWalk(false);
		}

		[SerializeField]
		private float _fieldOfView;

		[SerializeField]
		private float _dampTime;

		[SerializeField]
		private float _weightAdjustSpeed;

		private bool _useEnragedLayer;

		private FirstPersonMovementModule _fpc;

		private Scp096AttackAbility _attackAbility;

		private Scp096StateController _stateController;

		private const int EnrageLayer = 1;

		private static readonly int HashWalk = Animator.StringToHash("Walk");

		private static readonly int HashExitRage = Animator.StringToHash("RageExit");

		private static readonly int HashEnterRage = Animator.StringToHash("RageEnter");

		private static readonly int HashPryGate = Animator.StringToHash("PryGate");

		private static readonly int HashLeftAttack = Animator.StringToHash("LeftAttack");

		private static readonly int HashAttackTrigger = Animator.StringToHash("Attack");

		private static readonly int HashTryNotToCry = Animator.StringToHash("TryNotToCry");
	}
}
