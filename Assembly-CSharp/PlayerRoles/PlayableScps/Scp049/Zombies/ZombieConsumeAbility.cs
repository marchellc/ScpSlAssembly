using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp0492Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieConsumeAbility : RagdollAbilityBase<ZombieRole>
	{
		protected override float Duration
		{
			get
			{
				return 7f;
			}
		}

		protected override float RangeSqr
		{
			get
			{
				return 3.3f;
			}
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this._attackAbility.Cooldown.IsReady)
			{
				return;
			}
			base.ClientTryStart();
		}

		protected override byte ServerValidateCancel()
		{
			return 1;
		}

		protected override void OnProgressSet()
		{
			base.OnProgressSet();
			if (!base.IsInProgress)
			{
				return;
			}
			ZombieMovementModule zombieMovementModule = base.CastRole.FpcModule as ZombieMovementModule;
			if (zombieMovementModule == null)
			{
				return;
			}
			zombieMovementModule.ForceBloodlustSpeed();
			if (!NetworkServer.active || this._bloodlustAbility.SimulatedStare > 0f)
			{
				return;
			}
			this._bloodlustAbility.SimulatedStare = this.Duration + 5f - 5f;
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			bool isInProgress = base.IsInProgress;
			base.ServerProcessCmd(reader);
			if (base.IsInProgress && !isInProgress)
			{
				Scp0492Events.OnStartedConsumingCorpse(new Scp0492StartedConsumingCorpseEventArgs(base.Owner, base.CurRagdoll));
			}
		}

		protected override byte ServerValidateBegin(BasicRagdoll ragdoll)
		{
			ZombieConsumeAbility.ConsumeError consumeError = ZombieConsumeAbility.ConsumeError.None;
			if (ZombieConsumeAbility.ConsumedRagdolls.Contains(ragdoll))
			{
				consumeError = ZombieConsumeAbility.ConsumeError.AlreadyConsumed;
			}
			else if (!ragdoll.Info.RoleType.IsHuman() || !base.ServerValidateAny())
			{
				consumeError = ZombieConsumeAbility.ConsumeError.TargetNotValid;
			}
			else if (Mathf.Approximately(base.Owner.playerStats.GetModule<HealthStat>().NormalizedValue, 1f))
			{
				consumeError = ZombieConsumeAbility.ConsumeError.FullHealth;
			}
			else
			{
				foreach (ZombieConsumeAbility zombieConsumeAbility in ZombieConsumeAbility.AllAbilities)
				{
					if (zombieConsumeAbility.IsInProgress && zombieConsumeAbility.CurRagdoll == ragdoll)
					{
						consumeError = ZombieConsumeAbility.ConsumeError.BeingConsumed;
						break;
					}
				}
			}
			Scp0492StartingConsumingCorpseEventArgs scp0492StartingConsumingCorpseEventArgs = new Scp0492StartingConsumingCorpseEventArgs(base.Owner, ragdoll, consumeError);
			Scp0492Events.OnStartingConsumingCorpse(scp0492StartingConsumingCorpseEventArgs);
			consumeError = scp0492StartingConsumingCorpseEventArgs.Error;
			return (byte)consumeError;
		}

		protected override bool ServerValidateAny()
		{
			return true;
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<ZombieAttackAbility>(out this._attackAbility);
			base.GetSubroutine<ZombieBloodlustAbility>(out this._bloodlustAbility);
		}

		protected override void Update()
		{
			base.Update();
			this._headRotationDirty = true;
		}

		protected override void ServerComplete()
		{
			Scp0492ConsumingCorpseEventArgs scp0492ConsumingCorpseEventArgs = new Scp0492ConsumingCorpseEventArgs(base.Owner, base.CurRagdoll, 100f);
			Scp0492Events.OnConsumingCorpse(scp0492ConsumingCorpseEventArgs);
			if (!scp0492ConsumingCorpseEventArgs.IsAllowed)
			{
				return;
			}
			if (base.CurRagdoll != null)
			{
				bool flag = ZombieConsumeAbility.ConsumedRagdolls.Contains(base.CurRagdoll);
				if (scp0492ConsumingCorpseEventArgs.AddToConsumedRagdollList)
				{
					ZombieConsumeAbility.ConsumedRagdolls.Add(base.CurRagdoll);
				}
				if (flag && !scp0492ConsumingCorpseEventArgs.HealIfAlreadyConsumed)
				{
					return;
				}
			}
			base.Owner.playerStats.GetModule<HealthStat>().ServerHeal(scp0492ConsumingCorpseEventArgs.HealAmount);
			Scp0492Events.OnConsumedCorpse(new Scp0492ConsumedCorpseEventArgs(base.Owner, base.CurRagdoll));
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			ZombieConsumeAbility.AllAbilities.Add(this);
			this._headTransform = (base.CastRole.FpcModule.CharacterModelInstance as ZombieModel).HeadObject;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			ZombieConsumeAbility.AllAbilities.Remove(this);
		}

		public Vector3 ProcessCamPos(Vector3 original)
		{
			return Vector3.Lerp(original, this._headTransform.position, this._eatAnimPositionFade.Evaluate(base.ProgressStatus));
		}

		public Vector3 ProcessRotation()
		{
			if (this._headRotationDirty)
			{
				this._headRotation = Quaternion.Lerp(base.Owner.PlayerCameraReference.rotation, this._headTransform.rotation, this._eatAnimRotationFade.Evaluate(base.ProgressStatus)).eulerAngles;
				this._headRotationDirty = false;
			}
			return this._headRotation;
		}

		private const float ConsumeHeal = 100f;

		private const float SimulatedBloodlustDuration = 5f;

		private static readonly HashSet<ZombieConsumeAbility> AllAbilities = new HashSet<ZombieConsumeAbility>();

		[SerializeField]
		private AnimationCurve _eatAnimRotationFade;

		[SerializeField]
		private AnimationCurve _eatAnimPositionFade;

		private ZombieAttackAbility _attackAbility;

		private ZombieBloodlustAbility _bloodlustAbility;

		private Transform _headTransform;

		private bool _headRotationDirty;

		private Vector3 _headRotation;

		public static readonly HashSet<BasicRagdoll> ConsumedRagdolls = new HashSet<BasicRagdoll>();

		public enum ConsumeError : byte
		{
			None,
			CannotCancel,
			AlreadyConsumed,
			TargetNotValid,
			FullHealth = 8,
			BeingConsumed
		}
	}
}
