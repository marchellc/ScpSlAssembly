using AudioPooling;
using CustomRendering;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace CustomPlayerEffects;

public class PocketCorroding : TickingEffectBase, IFootstepEffect, IMovementSpeedModifier, IStaminaModifier
{
	private const float PDSpawnHeightOffset = 1.5f;

	[SerializeField]
	private float _startingDamage = 1f;

	[SerializeField]
	private AudioClip[] _footstepSounds;

	[SerializeField]
	private float _originalLoudness;

	private float _damagePerTick = 1f;

	public override bool AllowEnabling => true;

	public bool MovementModifierActive => base.IsEnabled;

	public bool StaminaModifierActive => base.IsEnabled;

	public float StaminaUsageMultiplier => 1f;

	public float MovementSpeedMultiplier => 0.75f;

	public float StaminaRegenMultiplier => 1f;

	public bool SprintingDisabled => true;

	public float MovementSpeedLimit => float.MaxValue;

	public RelativePosition CapturePosition { get; private set; }

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			if (base.Hub.TryGetLastKnownRoom(out var room) && room.Name == RoomName.Pocket)
			{
				base.Hub.playerStats.DealDamage(new UniversalDamageHandler(_damagePerTick, DeathTranslations.PocketDecay));
				_damagePerTick += 0.1f;
			}
			else
			{
				ServerDisable();
			}
		}
	}

	protected override void Enabled()
	{
		if (base.IsPOV)
		{
			SetFogEnabled(isEnabled: true);
		}
		if (!NetworkServer.active)
		{
			return;
		}
		_damagePerTick = _startingDamage;
		if (base.Hub.roleManager.CurrentRole is IFpcRole fpcRole && RoomUtils.TryFindRoom(RoomName.Pocket, null, null, out var foundRoom))
		{
			PlayerEnteringPocketDimensionEventArgs playerEnteringPocketDimensionEventArgs = new PlayerEnteringPocketDimensionEventArgs(base.Hub);
			PlayerEvents.OnEnteringPocketDimension(playerEnteringPocketDimensionEventArgs);
			if (playerEnteringPocketDimensionEventArgs.IsAllowed)
			{
				CapturePosition = new RelativePosition(fpcRole.FpcModule.Position);
				Vector3 position = foundRoom.transform.position;
				Vector3 vector = Vector3.up * 1.5f;
				fpcRole.FpcModule.ServerOverridePosition(position + vector);
				PlayerEvents.OnEnteredPocketDimension(new PlayerEnteredPocketDimensionEventArgs(base.Hub));
			}
		}
	}

	protected override void Disabled()
	{
		base.Disabled();
		if (base.IsPOV)
		{
			SetFogEnabled(isEnabled: false);
		}
	}

	public override void OnBeginSpectating()
	{
		base.OnBeginSpectating();
		SetFogEnabled(base.IsEnabled);
	}

	public override void OnStopSpectating()
	{
		base.OnStopSpectating();
		SetFogEnabled(isEnabled: false);
	}

	public float ProcessFootstepOverrides(float dis)
	{
		AudioSourcePoolManager.PlayOnTransform(_footstepSounds.RandomItem(), base.transform, dis);
		return _originalLoudness;
	}

	private void SetFogEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			FogController.EnableFogType(FogType.PocketDimension);
		}
		else
		{
			FogController.DisableFogType(FogType.PocketDimension);
		}
	}
}
