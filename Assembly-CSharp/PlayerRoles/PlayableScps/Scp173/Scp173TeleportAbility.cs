using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173TeleportAbility : KeySubroutine<Scp173Role>
{
	[Flags]
	private enum CmdTeleportData
	{
		Aiming = 1,
		WantsToTeleport = 2
	}

	private const float BlinkDistance = 8f;

	private const float BreakneckDistanceMultiplier = 1.8f;

	private const float KillRadiusSqr = 1.66f;

	private const float KillHeight = 2.2f;

	private const float KillBacktracking = 0.4f;

	private const float ClientDistanceAddition = 0.1f;

	private const int GlassLayerMask = 16384;

	private const float GlassDestroyRadius = 0.8f;

	private static readonly Collider[] DetectedColliders = new Collider[8];

	private Scp173MovementModule _fpcModule;

	private Scp173ObserversTracker _observersTracker;

	private Scp173BreakneckSpeedsAbility _breakneckSpeedsAbility;

	private Scp173BlinkTimer _blinkTimer;

	private Scp173AudioPlayer _audioSubroutine;

	private bool _isAiming;

	private float _targetDis;

	private Vector3 _tpPosition;

	private float _lastBlink;

	private CmdTeleportData _cmdData;

	[SerializeField]
	private Scp173TeleportIndicator _tpIndicator;

	[SerializeField]
	private AnimationCurve _blinkIntensity;

	[SerializeField]
	private Volume _blinkEffect;

	private float EffectiveBlinkDistance => 8f * (this._breakneckSpeedsAbility.IsActive ? 1.8f : 1f);

	protected override ActionName TargetKey => ActionName.Zoom;

	public ReferenceHub BestTarget
	{
		get
		{
			ReferenceHub result = null;
			float num = float.MaxValue;
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (!HitboxIdentity.IsEnemy(base.Owner, allHub) || !(allHub.roleManager.CurrentRole is IFpcRole fpcRole))
				{
					continue;
				}
				Vector3 position = fpcRole.FpcModule.Position;
				Vector3 tpPosition = this._tpPosition;
				if ((position - tpPosition).MagnitudeOnlyY() < 2.2f)
				{
					position.y = 0f;
					tpPosition.y = 0f;
				}
				float sqrMagnitude = (position - tpPosition).sqrMagnitude;
				if (!(sqrMagnitude > num))
				{
					if (Physics.Linecast(tpPosition, position, PlayerRolesUtils.AttackMask))
					{
						num = Mathf.Min(sqrMagnitude, 1.66f);
						continue;
					}
					num = sqrMagnitude;
					result = allHub;
				}
			}
			if (!(num > 1.66f))
			{
				return result;
			}
			return null;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._fpcModule = base.CastRole.FpcModule as Scp173MovementModule;
		SubroutineManagerModule subroutineModule = base.CastRole.SubroutineModule;
		subroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
		subroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
		subroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out this._audioSubroutine);
		subroutineModule.TryGetSubroutine<Scp173BlinkTimer>(out this._blinkTimer);
	}

	protected override void Update()
	{
		base.Update();
		if (!base.Role.IsControllable && !base.Owner.IsLocallySpectated())
		{
			if (this._isAiming)
			{
				this._isAiming = false;
				this._tpIndicator.UpdateVisibility(isVisible: false);
			}
			return;
		}
		bool flag = this.IsKeyHeld && (!Cursor.visible || base.Role.IsEmulatedDummy);
		bool flag2 = (base.Role.IsControllable ? flag : this.HasDataFlag(CmdTeleportData.Aiming));
		if (this._isAiming)
		{
			this.UpdateAiming(!flag2);
		}
		else if (flag2)
		{
			this._isAiming = true;
		}
	}

	private void UpdateAiming(bool wantsToTeleport)
	{
		bool flag = this._fpcModule.TryGetTeleportPos(this.EffectiveBlinkDistance, out this._tpPosition, out this._targetDis);
		if (!wantsToTeleport)
		{
			this._tpIndicator.UpdateVisibility(flag && this._blinkTimer.AbilityReady);
			this._tpIndicator.transform.position = this._tpPosition;
			if (!this.HasDataFlag(CmdTeleportData.Aiming))
			{
				this._cmdData = CmdTeleportData.Aiming;
				base.ClientSendCmd();
			}
		}
		else
		{
			if (base.Role.IsControllable)
			{
				this._cmdData = ((!flag) ? CmdTeleportData.Aiming : CmdTeleportData.WantsToTeleport);
				base.ClientSendCmd();
			}
			this._isAiming = false;
			this._tpIndicator.UpdateVisibility(isVisible: false);
		}
	}

	private bool TryBlink(float maxDis)
	{
		maxDis = Mathf.Clamp(maxDis, 0f, this.EffectiveBlinkDistance);
		if (!this._blinkTimer.AbilityReady)
		{
			return false;
		}
		if (!this._fpcModule.TryGetTeleportPos(maxDis, out this._tpPosition, out var _))
		{
			return false;
		}
		float num = this._fpcModule.CharController.height / 2f;
		this._blinkTimer.ServerBlink(this._tpPosition + Vector3.up * num);
		return true;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteByte((byte)this._cmdData);
		if (this.HasDataFlag(CmdTeleportData.WantsToTeleport))
		{
			writer.WriteQuaternion(base.Owner.PlayerCameraReference.rotation);
			writer.WriteFloat(this._targetDis + 0.1f);
			writer.WriteReferenceHub(this.BestTarget);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this._cmdData = (CmdTeleportData)reader.ReadByte();
		if (!this.HasDataFlag(CmdTeleportData.WantsToTeleport))
		{
			base.ServerSendRpc(toAll: true);
		}
		else
		{
			if (!this._blinkTimer.AbilityReady)
			{
				return;
			}
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			HashSet<ReferenceHub> prevObservers = new HashSet<ReferenceHub>(this._observersTracker.Observers);
			CmdTeleportData cmdData = this._cmdData;
			this._cmdData = (CmdTeleportData)0;
			base.ServerSendRpc(toAll: true);
			this._cmdData = cmdData;
			Quaternion rotation = playerCameraReference.rotation;
			playerCameraReference.rotation = reader.ReadQuaternion();
			bool num = this.TryBlink(reader.ReadFloat());
			playerCameraReference.rotation = rotation;
			if (!num)
			{
				return;
			}
			prevObservers.UnionWith(this._observersTracker.Observers);
			base.ServerSendRpc((ReferenceHub x) => prevObservers.Contains(x));
			this._audioSubroutine.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Teleport);
			if (this._breakneckSpeedsAbility.IsActive)
			{
				return;
			}
			int num2 = Physics.OverlapSphereNonAlloc(this._fpcModule.Position, 0.8f, Scp173TeleportAbility.DetectedColliders, 16384);
			for (int num3 = 0; num3 < num2; num3++)
			{
				if (Scp173TeleportAbility.DetectedColliders[num3].TryGetComponent<BreakableWindow>(out var component))
				{
					component.Damage(component.health, base.CastRole.DamageHandler, Vector3.zero);
				}
			}
			ReferenceHub referenceHub = reader.ReadReferenceHub();
			if (!(referenceHub == null) && HitboxIdentity.IsEnemy(base.Owner, referenceHub) && referenceHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				Bounds bounds = fpcRole.FpcModule.Tracer.GenerateBounds(0.4f, ignoreTeleports: true);
				Vector3 position = fpcRole.FpcModule.Position;
				bounds.Encapsulate(new Bounds(position, Vector3.up * 2.2f));
				if (!(bounds.SqrDistance(this._fpcModule.Position) > 1.66f) && !Physics.Linecast(this._tpPosition, position, PlayerRolesUtils.AttackMask) && referenceHub.playerStats.DealDamage(base.CastRole.DamageHandler) && base.CastRole.SubroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out var subroutine))
				{
					Hitmarker.SendHitmarkerDirectly(base.Owner, 1f);
					subroutine.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Snap);
				}
			}
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._cmdData);
		if (this.HasDataFlag(CmdTeleportData.WantsToTeleport))
		{
			writer.WriteRelativePosition(new RelativePosition(this._fpcModule.Position));
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._cmdData = (CmdTeleportData)reader.ReadByte();
		if (this.HasDataFlag(CmdTeleportData.WantsToTeleport))
		{
			RelativePosition receivedPosition = reader.ReadRelativePosition();
			this._fpcModule.Motor.ReceivedPosition = receivedPosition;
			this._fpcModule.Position = receivedPosition.Position;
			this._lastBlink = Time.timeSinceLevelLoad;
			this._blinkEffect.weight = 1f;
			(this._fpcModule.CharacterModelInstance as Scp173CharacterModel).Frozen = false;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._lastBlink = 0f;
	}

	private bool HasDataFlag(CmdTeleportData ctd)
	{
		return (this._cmdData & ctd) == ctd;
	}
}
