using System.Collections.Generic;
using LabApi.Events.Arguments.Scp0492Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies;

public class ZombieConsumeAbility : RagdollAbilityBase<ZombieRole>
{
	public enum ConsumeError : byte
	{
		None = 0,
		CannotCancel = 1,
		AlreadyConsumed = 2,
		TargetNotValid = 3,
		FullHealth = 8,
		BeingConsumed = 9
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

	protected override float Duration => 7f;

	protected override float RangeSqr => 3.3f;

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (_attackAbility.Cooldown.IsReady)
		{
			ClientTryStart();
		}
	}

	protected override byte ServerValidateCancel()
	{
		return 1;
	}

	protected override void OnProgressSet()
	{
		base.OnProgressSet();
		if (base.IsInProgress && base.CastRole.FpcModule is ZombieMovementModule zombieMovementModule)
		{
			zombieMovementModule.ForceBloodlustSpeed();
			if (NetworkServer.active && !(_bloodlustAbility.SimulatedStare > 0f))
			{
				_bloodlustAbility.SimulatedStare = Duration + 5f - 5f;
			}
		}
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
		ConsumeError error = ConsumeError.None;
		if (ConsumedRagdolls.Contains(ragdoll))
		{
			error = ConsumeError.AlreadyConsumed;
		}
		else if (!ragdoll.Info.RoleType.IsHuman() || !base.ServerValidateAny())
		{
			error = ConsumeError.TargetNotValid;
		}
		else if (Mathf.Approximately(base.Owner.playerStats.GetModule<HealthStat>().NormalizedValue, 1f))
		{
			error = ConsumeError.FullHealth;
		}
		else
		{
			foreach (ZombieConsumeAbility allAbility in AllAbilities)
			{
				if (allAbility.IsInProgress && allAbility.CurRagdoll == ragdoll)
				{
					error = ConsumeError.BeingConsumed;
					break;
				}
			}
		}
		Scp0492StartingConsumingCorpseEventArgs scp0492StartingConsumingCorpseEventArgs = new Scp0492StartingConsumingCorpseEventArgs(base.Owner, ragdoll, error);
		Scp0492Events.OnStartingConsumingCorpse(scp0492StartingConsumingCorpseEventArgs);
		return (byte)scp0492StartingConsumingCorpseEventArgs.Error;
	}

	protected override bool ServerValidateAny()
	{
		return true;
	}

	protected override void Awake()
	{
		base.Awake();
		GetSubroutine<ZombieAttackAbility>(out _attackAbility);
		GetSubroutine<ZombieBloodlustAbility>(out _bloodlustAbility);
	}

	protected override void Update()
	{
		base.Update();
		_headRotationDirty = true;
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
			bool num = ConsumedRagdolls.Contains(base.CurRagdoll);
			if (scp0492ConsumingCorpseEventArgs.AddToConsumedRagdollList)
			{
				ConsumedRagdolls.Add(base.CurRagdoll);
			}
			if (num && !scp0492ConsumingCorpseEventArgs.HealIfAlreadyConsumed)
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
		AllAbilities.Add(this);
		_headTransform = (base.CastRole.FpcModule.CharacterModelInstance as ZombieModel).HeadObject;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		AllAbilities.Remove(this);
	}

	public Vector3 ProcessCamPos(Vector3 original)
	{
		return Vector3.Lerp(original, _headTransform.position, _eatAnimPositionFade.Evaluate(base.ProgressStatus));
	}

	public Vector3 ProcessRotation()
	{
		if (_headRotationDirty)
		{
			_headRotation = Quaternion.Lerp(base.Owner.PlayerCameraReference.rotation, _headTransform.rotation, _eatAnimRotationFade.Evaluate(base.ProgressStatus)).eulerAngles;
			_headRotationDirty = false;
		}
		return _headRotation;
	}
}
