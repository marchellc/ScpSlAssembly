using System;
using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Subroutines;

public abstract class ScpAttackAbilityBase<T> : KeySubroutine<T> where T : PlayerRoleBase, IFpcRole
{
	[SerializeField]
	private float _detectionRadius;

	[SerializeField]
	private float _detectionOffset;

	[SerializeField]
	private AudioClip _killSound;

	[SerializeField]
	private AudioClip[] _hitClipsHuman;

	[SerializeField]
	private AudioClip[] _hitClipsObjects;

	private readonly Stopwatch _delaySw = new Stopwatch();

	private readonly TolerantAbilityCooldown _clientCooldown = new TolerantAbilityCooldown();

	private readonly TolerantAbilityCooldown _serverCooldown = new TolerantAbilityCooldown();

	private static readonly HashSet<FpcBacktracker> BacktrackedPlayers = new HashSet<FpcBacktracker>();

	private static readonly IDestructible[] DestDetectionsNonAlloc = new IDestructible[128];

	private static readonly Collider[] DetectionsNonAlloc = new Collider[128];

	private static readonly CachedLayerMask DetectionMask = new CachedLayerMask("Hitbox", "Glass");

	private const int DetectionsNumber = 128;

	protected readonly HashSet<ReferenceHub> DetectedPlayers = new HashSet<ReferenceHub>();

	public TolerantAbilityCooldown Cooldown
	{
		get
		{
			if (!base.Owner.isLocalPlayer && NetworkServer.active)
			{
				return this._serverCooldown;
			}
			return this._clientCooldown;
		}
	}

	public bool AttackTriggered { get; private set; }

	public AttackResult LastAttackResult { get; protected set; }

	public abstract float DamageAmount { get; }

	protected virtual float SoundRange => 13f;

	protected virtual float AttackDelay => 0f;

	protected virtual float BaseCooldown => 1f;

	protected virtual bool SelfRepeating => true;

	protected virtual bool CanTriggerAbility => this._clientCooldown.IsReady;

	protected override ActionName TargetKey => ActionName.Shoot;

	private Transform PlyCam => base.Owner.PlayerCameraReference;

	private Vector3 OverlapSphereOrigin => this.PlyCam.position + this.PlyCam.forward * this._detectionOffset;

	public event Action<AttackResult> OnAttacked;

	public event Action OnTriggered;

	protected abstract DamageHandlerBase DamageHandler(float damage);

	public static ArraySegment<IDestructible> DetectDestructibles(ReferenceHub detector, float offset, float radius, bool losTest = true)
	{
		Vector3 cameraPos = detector.PlayerCameraReference.position;
		int num = Physics.OverlapSphereNonAlloc(detector.PlayerCameraReference.TransformPoint(Vector3.forward * offset), radius, ScpAttackAbilityBase<T>.DetectionsNonAlloc, ScpAttackAbilityBase<T>.DetectionMask);
		int count = 0;
		for (int i = 0; i < num; i++)
		{
			Collider collider = ScpAttackAbilityBase<T>.DetectionsNonAlloc[i];
			if (collider.TryGetComponent<IDestructible>(out var component) && component.NetworkId != detector.netId && (!losTest || CheckLineOfSight(collider, component.CenterOfMass)))
			{
				ScpAttackAbilityBase<T>.DestDetectionsNonAlloc[count++] = component;
			}
		}
		return new ArraySegment<IDestructible>(ScpAttackAbilityBase<T>.DestDetectionsNonAlloc, 0, count);
		bool CheckLineOfSight(Collider hitColldier, Vector3 hitCenterOfMass)
		{
			if (!Physics.Linecast(cameraPos, hitCenterOfMass, out var hitInfo, PlayerRolesUtils.AttackMask))
			{
				return true;
			}
			if (hitInfo.colliderInstanceID == hitColldier.GetInstanceID())
			{
				return true;
			}
			return false;
		}
	}

	private void ServerPerformAttack()
	{
		this.LastAttackResult = AttackResult.None;
		foreach (IDestructible item in ScpAttackAbilityBase<T>.DetectDestructibles(base.Owner, this._detectionOffset, this._detectionRadius))
		{
			if (!(item is HitboxIdentity hitboxIdentity))
			{
				this.DamageDestructible(item);
				this.LastAttackResult |= AttackResult.AttackedObject;
			}
			else if (HitboxIdentity.IsEnemy(base.Owner, hitboxIdentity.TargetHub))
			{
				this.DetectedPlayers.Add(hitboxIdentity.TargetHub);
			}
		}
		this.DamagePlayers();
		base.ServerSendRpc(toAll: true);
	}

	protected virtual void DamagePlayers()
	{
		foreach (ReferenceHub detectedPlayer in this.DetectedPlayers)
		{
			this.DamagePlayer(detectedPlayer, this.DamageAmount);
		}
	}

	protected virtual void DamagePlayer(ReferenceHub hub, float damage)
	{
		PlayerStats playerStats = hub.playerStats;
		if (playerStats.DealDamage(this.DamageHandler(damage)))
		{
			this.LastAttackResult |= AttackResult.AttackedPlayer;
			if (!(playerStats.GetModule<HealthStat>().CurValue > 0f))
			{
				this.LastAttackResult |= AttackResult.KilledPlayer;
			}
		}
	}

	protected virtual void DamageDestructible(IDestructible dest)
	{
		dest.Damage(this.DamageAmount, this.DamageHandler(this.DamageAmount), dest.CenterOfMass);
	}

	protected bool HasAttackResultFlag(AttackResult flag)
	{
		return (this.LastAttackResult & flag) == flag;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		if (this.AttackTriggered)
		{
			writer.WriteRelativePosition(default(RelativePosition));
			return;
		}
		Vector3 position = base.CastRole.FpcModule.Position;
		float num = this._detectionOffset + this._detectionRadius;
		float num2 = num * num;
		writer.WriteRelativePosition(new RelativePosition(position));
		writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(this.PlyCam.rotation));
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (HitboxIdentity.IsEnemy(base.Owner, allHub) && allHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				Vector3 position2 = fpcRole.FpcModule.Position;
				if (!((position2 - position).sqrMagnitude > num2))
				{
					writer.WriteReferenceHub(allHub);
					writer.WriteRelativePosition(new RelativePosition(position2));
				}
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		RelativePosition relativePosition = reader.ReadRelativePosition();
		if (relativePosition.WaypointId == 0)
		{
			this.AttackTriggered = true;
			base.ServerSendRpc(toAll: true);
		}
		else
		{
			if (!this._serverCooldown.TolerantIsReady && !base.Owner.isLocalPlayer)
			{
				return;
			}
			this.AttackTriggered = false;
			Vector3 position = relativePosition.Position;
			Quaternion value = reader.ReadLowPrecisionQuaternion().Value;
			ScpAttackAbilityBase<T>.BacktrackedPlayers.Add(new FpcBacktracker(base.Owner, position, value));
			List<ReferenceHub> list = new List<ReferenceHub>();
			while (reader.Position < reader.Capacity)
			{
				ReferenceHub referenceHub = reader.ReadReferenceHub();
				list.Add(referenceHub);
				RelativePosition relativePosition2 = reader.ReadRelativePosition();
				if (!(referenceHub == null) && HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					ScpAttackAbilityBase<T>.BacktrackedPlayers.Add(new FpcBacktracker(referenceHub, relativePosition2.Position));
				}
			}
			this.ServerPerformAttack();
			ScpAttackAbilityBase<T>.BacktrackedPlayers.ForEach(delegate(FpcBacktracker x)
			{
				x.RestorePosition();
			});
			this._serverCooldown.Trigger(this.BaseCooldown);
			this.DetectedPlayers.Clear();
			ScpAttackAbilityBase<T>.BacktrackedPlayers.Clear();
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		if (!this.AttackTriggered)
		{
			writer.WriteByte((byte)this.LastAttackResult);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (reader.Position >= reader.Capacity)
		{
			if (!base.Role.IsControllable)
			{
				this._clientCooldown.Trigger(this.BaseCooldown);
				this.OnTriggered?.Invoke();
			}
			return;
		}
		this.LastAttackResult = (AttackResult)reader.ReadByte();
		this.OnAttacked?.Invoke(this.LastAttackResult);
		if (this.LastAttackResult != AttackResult.None && (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated()))
		{
			Hitmarker.PlayHitmarker(1f);
		}
		if (this.HasAttackResultFlag(AttackResult.KilledPlayer) && this._killSound != null)
		{
			AudioSourcePoolManager.PlayOnTransform(this._killSound, base.transform, this.SoundRange);
		}
		else if (this.HasAttackResultFlag(AttackResult.AttackedPlayer) && this._hitClipsHuman.Length != 0)
		{
			AudioSourcePoolManager.PlayOnTransform(this._hitClipsHuman.RandomItem(), base.transform, this.SoundRange);
		}
		else if (this.HasAttackResultFlag(AttackResult.AttackedObject) && this._hitClipsObjects.Length != 0)
		{
			AudioSourcePoolManager.PlayOnTransform(this._hitClipsObjects.RandomItem(), base.transform, this.SoundRange);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.AttackTriggered = false;
		this._delaySw.Reset();
		this._clientCooldown.Clear();
		this._serverCooldown.Clear();
		this.DetectedPlayers.Clear();
		ScpAttackAbilityBase<T>.BacktrackedPlayers.Clear();
	}

	protected override void Update()
	{
		base.Update();
		if (base.Role.IsControllable)
		{
			this.OnClientUpdate();
		}
	}

	protected virtual void OnClientUpdate()
	{
		if (this.AttackTriggered)
		{
			if (!(this._delaySw.Elapsed.TotalSeconds < (double)this.AttackDelay))
			{
				this.AttackTriggered = false;
				base.ClientSendCmd();
			}
		}
		else if (this.CanTriggerAbility && this.SelfRepeating && this.IsKeyHeld)
		{
			this.ClientPerformAttack();
		}
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (!this.AttackTriggered && !this.SelfRepeating && this.CanTriggerAbility)
		{
			this.ClientPerformAttack();
		}
	}

	protected virtual void ClientPerformAttack(bool attackTriggered = true)
	{
		this._clientCooldown.Trigger(this.BaseCooldown);
		this.AttackTriggered = attackTriggered;
		this._delaySw.Restart();
		this.AttackTriggered = true;
		base.ClientSendCmd();
		this.OnTriggered?.Invoke();
	}

	private void OnDrawGizmosSelected()
	{
		if (!(base.Owner == null))
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(this.OverlapSphereOrigin, this._detectionRadius);
		}
	}
}
