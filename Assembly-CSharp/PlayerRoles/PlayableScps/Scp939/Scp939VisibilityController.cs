using System;
using System.Collections.Generic;
using GameObjectPools;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Spectating;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939VisibilityController : FpcVisibilityController, IPoolResettable
{
	private struct LastSeenInfo
	{
		public double Time;

		public RelativePosition RelPos;

		public Vector3 Velocity;

		public Vector3 WorldPos => RelPos.Position + Velocity * Elapsed;

		public float Elapsed => (float)(NetworkTime.time - Time);
	}

	private const float DetectionRangeForShootingCrouchingOrJumping = 4f;

	[SerializeField]
	private float _pingTolerance;

	[SerializeField]
	private float _defaultRange;

	[SerializeField]
	private float _recentFootstepRangeMultiplier;

	[SerializeField]
	private float _recentFootstepTime;

	[SerializeField]
	private float _focusMultiplier;

	[SerializeField]
	private float _exhaustionMultiplier;

	[SerializeField]
	private float _fadeSpeed;

	[SerializeField]
	private float _sustain;

	private Scp939Role _scpRole;

	private StaminaStat _stamina;

	private Scp939FocusAbility _focus;

	private bool _wasFaded;

	private static readonly Dictionary<uint, LastSeenInfo> LastSeen = new Dictionary<uint, LastSeenInfo>();

	private readonly Dictionary<uint, double> _lastFootstepSounds = new Dictionary<uint, double>();

	private readonly Dictionary<uint, double> _lastShotSound = new Dictionary<uint, double>();

	public float CurrentDetectionRange
	{
		get
		{
			float defaultRange = _defaultRange;
			return (defaultRange + defaultRange * _focusMultiplier * _focus.State) * Mathf.Lerp(_exhaustionMultiplier, 1f, _stamina.NormalizedValue);
		}
	}

	private float DetectionRangeForPlayer(ReferenceHub hub)
	{
		float num = CurrentDetectionRange;
		if (_lastFootstepSounds.TryGetValue(hub.netId, out var value) && NetworkTime.time - value < (double)_recentFootstepTime)
		{
			num *= _recentFootstepRangeMultiplier;
		}
		if (!HitboxIdentity.IsEnemy(base.Owner, hub) || !(hub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
		{
			return num;
		}
		bool isJumping = fpcStandardRoleBase.FpcModule.Motor.IsJumping;
		bool flag = fpcStandardRoleBase.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting;
		double value2;
		bool flag2 = _lastShotSound.TryGetValue(hub.netId, out value2) && NetworkTime.time - value2 < (double)_recentFootstepTime;
		if (isJumping || flag || flag2)
		{
			return 4f;
		}
		return num;
	}

	private void OnDestroy()
	{
		if (_wasFaded)
		{
			ResetFade();
		}
	}

	private void LateUpdate()
	{
		if (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated())
		{
			if (AlphaWarheadController.Detonated)
			{
				ResetFade();
			}
			else
			{
				PlayerRolesUtils.ForEachRole<FpcStandardRoleBase>(UpdateEnemies);
			}
		}
	}

	private void UpdateEnemies(ReferenceHub ply, FpcStandardRoleBase human)
	{
		if (!HitboxIdentity.IsEnemy(base.Owner, ply))
		{
			return;
		}
		FirstPersonMovementModule fpcModule = human.FpcModule;
		FpcMotor motor = fpcModule.Motor;
		CharacterModel characterModelInstance = fpcModule.CharacterModelInstance;
		LastSeenInfo value;
		bool flag = LastSeen.TryGetValue(ply.netId, out value);
		bool flag2 = !motor.IsInvisible;
		bool flag3 = flag2 && (Vector3.Distance(fpcModule.Position, _scpRole.FpcModule.Position) <= DetectionRangeForPlayer(ply) || (flag && value.Elapsed < _sustain));
		float fade = characterModelInstance.Fade;
		characterModelInstance.Fade += Time.deltaTime * (flag3 ? _fadeSpeed : (0f - _fadeSpeed));
		_wasFaded = true;
		if (NetworkServer.active || !base.Owner.isLocalPlayer)
		{
			return;
		}
		if (characterModelInstance.Fade == 0f)
		{
			if (fade != 0f)
			{
				characterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity x)
				{
					x.SetColliders(newState: false);
				});
			}
		}
		else if (!flag2)
		{
			fpcModule.Position = (flag ? value.WorldPos : motor.ReceivedPosition.Position);
		}
		else if (flag3 && fade == 0f)
		{
			fpcModule.Position = motor.ReceivedPosition.Position;
			if (characterModelInstance is AnimatedCharacterModel animatedCharacterModel)
			{
				animatedCharacterModel.ForceUpdate();
			}
			HitboxIdentity[] hitboxes = characterModelInstance.Hitboxes;
			for (int i = 0; i < hitboxes.Length; i++)
			{
				hitboxes[i].SetColliders(newState: true);
			}
		}
	}

	private void ResetFade()
	{
		PlayerRolesUtils.ForEachRole(delegate(FpcStandardRoleBase x)
		{
			CharacterModel characterModelInstance = x.FpcModule.CharacterModelInstance;
			if (characterModelInstance.Fade != 1f)
			{
				characterModelInstance.Fade = 1f;
				characterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity hitbox)
				{
					hitbox.SetColliders(newState: true);
				});
			}
		});
	}

	private void OnFootstepPlayed(AnimatedCharacterModel model, float range)
	{
		ReferenceHub ownerHub = model.OwnerHub;
		if (HitboxIdentity.IsEnemy(base.Owner, ownerHub) && ownerHub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase && !((fpcStandardRoleBase.FpcModule.Position - _scpRole.FpcModule.Position).sqrMagnitude > range * range))
		{
			_lastFootstepSounds[ownerHub.netId] = NetworkTime.time;
		}
	}

	private void OnSpectatorTargetChanged()
	{
		if (_wasFaded)
		{
			ResetFade();
		}
	}

	private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (_wasFaded && hub.isLocalPlayer && !(newRole is SpectatorRole))
		{
			ResetFade();
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		_scpRole = base.Role as Scp939Role;
		base.Owner.playerStats.TryGetModule<StaminaStat>(out _stamina);
		_scpRole.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out _focus);
		SpectatorTargetTracker.OnTargetChanged += OnSpectatorTargetChanged;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Combine(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(OnFootstepPlayed));
		if (base.Owner.isLocalPlayer)
		{
			LastSeen.Clear();
		}
	}

	public override bool ValidateVisibility(ReferenceHub hub)
	{
		if (!base.ValidateVisibility(hub))
		{
			return false;
		}
		if (!HitboxIdentity.IsEnemy(base.Owner, hub))
		{
			return true;
		}
		if (!(hub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
		{
			return true;
		}
		if (AlphaWarheadController.Detonated)
		{
			return true;
		}
		FirstPersonMovementModule fpcModule = _scpRole.FpcModule;
		float a = BaseRangeForPlayer(hub, fpcStandardRoleBase);
		a = Mathf.Max(a, DetectionRangeForPlayer(hub));
		a += fpcModule.MaxMovementSpeed * _pingTolerance;
		bool num = (fpcStandardRoleBase.FpcModule.Position - fpcModule.Position).sqrMagnitude <= a * a;
		LastSeenInfo value;
		bool result = num || (LastSeen.TryGetValue(hub.netId, out value) && value.Elapsed < _sustain);
		if (!num || _scpRole.IsLocalPlayer)
		{
			return result;
		}
		LastSeen[hub.netId] = new LastSeenInfo
		{
			RelPos = new RelativePosition(fpcModule.Position),
			Time = NetworkTime.time,
			Velocity = fpcModule.Motor.Velocity
		};
		return true;
	}

	private float BaseRangeForPlayer(ReferenceHub hub, FpcStandardRoleBase targetRole)
	{
		float num = 0f;
		ItemBase curInstance = hub.inventory.CurInstance;
		if (curInstance is ISoundEmittingItem soundEmittingItem && curInstance != null && soundEmittingItem.ServerTryGetSoundEmissionRange(out var range))
		{
			num = range;
		}
		if (targetRole.VoiceModule is HumanVoiceModule { ServerIsSending: not false } humanVoiceModule)
		{
			num = Mathf.Max(num, humanVoiceModule.FirstProxPlayback.Source.maxDistance);
		}
		return num;
	}

	public void ResetObject()
	{
		SpectatorTargetTracker.OnTargetChanged -= OnSpectatorTargetChanged;
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
		AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Remove(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(OnFootstepPlayed));
		if (_wasFaded)
		{
			ResetFade();
		}
	}
}
