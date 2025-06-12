using System.Collections.Generic;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using Hazards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939AmnesticCloudInstance : TemporaryHazard
{
	public enum CloudState
	{
		Spawning,
		Created,
		Destroyed
	}

	public static readonly List<Scp939AmnesticCloudInstance> ActiveInstances;

	private static readonly int HashRadiusPercent;

	private static readonly int HashStatusPercent;

	private readonly AbilityCooldown _overallCooldown = new AbilityCooldown();

	private readonly Dictionary<uint, AbilityCooldown> _individualCooldown = new Dictionary<uint, AbilityCooldown>();

	private Scp939AmnesticCloudAbility _cloud;

	private Scp939LungeAbility _lunge;

	private Scp939ClawAbility _claw;

	private Scp939Role _scpRole;

	private Transform _t;

	private Material _mat;

	private bool _abilitiesSet;

	private float _targetDuration;

	private float _lastHoldTime;

	private float _prevRange;

	private bool _localOwner;

	private bool _alreadyCreated;

	[SyncVar]
	private byte _syncHoldTime;

	[SyncVar]
	private byte _syncState;

	[SyncVar]
	private uint _syncOwner;

	[SyncVar]
	private RelativePosition _syncPos;

	[Header("Balance")]
	[SerializeField]
	private float _minHoldTime;

	[SerializeField]
	private float _maxHoldTime;

	[SerializeField]
	private AnimationCurve _rangeOverHeldTime;

	[SerializeField]
	private AnimationCurve _durationOverHeldTime;

	[SerializeField]
	private float _amnesiaDuration;

	[SerializeField]
	private float _pauseDuration;

	[Header("Audiovisual")]
	[SerializeField]
	private float _destroyTime;

	[SerializeField]
	private float _soundDropRate;

	[SerializeField]
	private float _sizeLerpTime;

	[SerializeField]
	private float _colorLerpTime;

	[SerializeField]
	private AudioSource _deploySound;

	[SerializeField]
	private AudioSource _chargeupSound;

	[SerializeField]
	private AnimationCurve _chargeupVolumeOverSize;

	[SerializeField]
	private DecalProjector _decalProjector;

	private float _decaySpeedOverride = -1f;

	public float NormalizedHoldTime => Mathf.Clamp01(this._cloud.HoldDuration / this._maxHoldTime);

	public ReferenceHub Owner
	{
		get
		{
			if (!ReferenceHub.TryGetHubNetID(this._syncOwner, out var hub))
			{
				return null;
			}
			return hub;
		}
		set
		{
			this.Network_syncOwner = value.netId;
		}
	}

	public RelativePosition SyncedPosition
	{
		get
		{
			return this._syncPos;
		}
		set
		{
			this.Network_syncPos = value;
		}
	}

	public byte HoldDuration
	{
		get
		{
			return this._syncHoldTime;
		}
		set
		{
			this.Network_syncHoldTime = value;
		}
	}

	public float PauseDuration
	{
		get
		{
			return this._pauseDuration;
		}
		set
		{
			this._pauseDuration = value;
		}
	}

	public float AmnesiaDuration
	{
		get
		{
			return this._amnesiaDuration;
		}
		set
		{
			this._amnesiaDuration = value;
		}
	}

	public CloudState State
	{
		get
		{
			return (CloudState)this._syncState;
		}
		set
		{
			this.Network_syncState = (byte)value;
		}
	}

	public override float HazardDuration
	{
		get
		{
			return this._targetDuration;
		}
		set
		{
			this._targetDuration = value;
		}
	}

	public override float DecaySpeed
	{
		get
		{
			if (this._decaySpeedOverride >= 0f)
			{
				return this._decaySpeedOverride;
			}
			if (this.State != CloudState.Created)
			{
				return 0f;
			}
			return 1f;
		}
		set
		{
			this._decaySpeedOverride = value;
		}
	}

	public Vector2 MinMaxTime => new Vector2(this._minHoldTime, this._maxHoldTime);

	public byte Network_syncHoldTime
	{
		get
		{
			return this._syncHoldTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncHoldTime, 1uL, null);
		}
	}

	public byte Network_syncState
	{
		get
		{
			return this._syncState;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncState, 2uL, null);
		}
	}

	public uint Network_syncOwner
	{
		get
		{
			return this._syncOwner;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncOwner, 4uL, null);
		}
	}

	public RelativePosition Network_syncPos
	{
		get
		{
			return this._syncPos;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncPos, 8uL, null);
		}
	}

	[Server]
	public override void ServerDestroy()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::ServerDestroy()' called when server was not active");
			return;
		}
		base.ServerDestroy();
		this._abilitiesSet = false;
		this.State = CloudState.Destroyed;
	}

	public override bool OnEnter(ReferenceHub player)
	{
		if (!HitboxIdentity.IsEnemy(Team.SCPs, player.GetTeam()) || player.IsFlamingo())
		{
			return false;
		}
		if (!base.OnEnter(player))
		{
			return false;
		}
		PlayerEvents.OnEnteredHazard(new PlayerEnteredHazardEventArgs(player, this));
		return true;
	}

	public override void OnStay(ReferenceHub player)
	{
		base.OnStay(player);
		if (this.State == CloudState.Created && this.IsActive && this._overallCooldown.IsReady && (!this._individualCooldown.TryGetValue(player.netId, out var value) || value.IsReady))
		{
			PlayerEffectsController playerEffectsController = player.playerEffectsController;
			if (!playerEffectsController.TryGetEffect<Invigorated>(out var playerEffect) || !playerEffect.IsEnabled)
			{
				playerEffectsController.EnableEffect<AmnesiaVision>(this._amnesiaDuration);
			}
		}
	}

	public override bool OnExit(ReferenceHub player)
	{
		if (!base.OnExit(player))
		{
			return false;
		}
		PlayerEvents.OnLeftHazard(new PlayerLeftHazardEventArgs(player, this));
		return true;
	}

	public void PauseAll()
	{
		foreach (ReferenceHub affectedPlayer in base.AffectedPlayers)
		{
			if (!affectedPlayer.playerEffectsController.TryGetEffect<AmnesiaVision>(out var playerEffect))
			{
				return;
			}
			playerEffect.IsEnabled = false;
		}
		this._overallCooldown.Trigger(this._pauseDuration);
	}

	protected override void ClientApplyDecalSize()
	{
	}

	protected override void Start()
	{
		this._t = base.transform;
		this._mat = new Material(this._decalProjector.material);
		this._decalProjector.material = this._mat;
		Scp939AmnesticCloudInstance.ActiveInstances.Add(this);
		if (ReferenceHub.TryGetHubNetID(this._syncOwner, out var hub) && hub.isLocalPlayer)
		{
			this._localOwner = true;
			this.SetAbilityCache();
		}
		if (this.Owner == null || (ReferenceHub.TryGetPovHub(out var hub2) && !(hub2.roleManager.CurrentRole is Scp939Role)))
		{
			this._chargeupSound.mute = true;
		}
		this.ClientApplyDecalSize();
		base.Start();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp939AmnesticCloudInstance.ActiveInstances.Remove(this);
		PlayerStats.OnAnyPlayerDamaged -= OnAnyPlayerDamaged;
		if (this._lunge != null)
		{
			this._lunge.OnStateChanged -= OnLungeStateChanged;
		}
		if (this._claw != null)
		{
			this._claw.OnAttacked -= OnAttacked;
		}
	}

	protected override void Update()
	{
		base.Update();
		if (this._localOwner)
		{
			this.UpdateLocal();
		}
		else
		{
			this.UpdateVisuals((float)(int)this._syncHoldTime / 255f, Time.deltaTime * this._sizeLerpTime);
		}
		if (NetworkServer.active)
		{
			switch (this.State)
			{
			case CloudState.Spawning:
				this.ServerUpdateSpawning();
				break;
			case CloudState.Destroyed:
				this.ServerUpdateDestroyed();
				break;
			}
		}
	}

	private void TryGetPlayer(out bool is939, out bool isOwner)
	{
		is939 = false;
		isOwner = false;
		if (ReferenceHub.TryGetPovHub(out var hub))
		{
			is939 = hub.roleManager.CurrentRole is Scp939Role;
			isOwner = hub.netId == this._syncOwner;
		}
	}

	private void OnAttacked(AttackResult attackResult)
	{
		if (attackResult != AttackResult.None)
		{
			this.PauseAll();
		}
	}

	private void OnAnyPlayerDamaged(ReferenceHub hub, DamageHandlerBase dhb)
	{
		if (hub.netId == this._syncOwner && dhb is AttackerDamageHandler attackerDamageHandler)
		{
			AbilityCooldown abilityCooldown = new AbilityCooldown();
			abilityCooldown.Trigger(this._pauseDuration);
			uint attackerId = attackerDamageHandler.Attacker.NetId;
			this._individualCooldown[attackerId] = abilityCooldown;
			if (base.AffectedPlayers.TryGetFirst((ReferenceHub x) => x.netId == attackerId, out var first) && first.playerEffectsController.TryGetEffect<AmnesiaVision>(out var playerEffect))
			{
				playerEffect.IsEnabled = false;
			}
		}
	}

	private void OnLungeStateChanged(Scp939LungeState state)
	{
		if (state == Scp939LungeState.LandHit)
		{
			this.PauseAll();
		}
	}

	private void SetAbilityCache()
	{
		this._abilitiesSet = false;
		if (ReferenceHub.TryGetHubNetID(this._syncOwner, out var hub) && hub.roleManager.CurrentRole is Scp939Role scpRole)
		{
			this._scpRole = scpRole;
			this._abilitiesSet = this._scpRole.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._cloud) && this._scpRole.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lunge) && this._scpRole.SubroutineModule.TryGetSubroutine<Scp939ClawAbility>(out this._claw);
		}
	}

	private void RefreshPosition(ReferenceHub owner)
	{
		this._t.position = owner.PlayerCameraReference.position;
	}

	private void UpdateLocal()
	{
		if (this._abilitiesSet && ReferenceHub.TryGetLocalHub(out var hub))
		{
			switch (this.State)
			{
			case CloudState.Destroyed:
				this._cloud.ClientCancel(Scp939HudTranslation.CloudFailedSizeInsufficient);
				break;
			case CloudState.Created:
				this._cloud.ClientCancel(Scp939HudTranslation.PressKeyToLunge);
				break;
			}
			if (!this._cloud.ValidateFloor())
			{
				this._cloud.ClientCancel((this._cloud.HoldDuration < this._minHoldTime) ? Scp939HudTranslation.CloudFailedSizeInsufficient : Scp939HudTranslation.PressKeyToLunge);
			}
			if (this._cloud.TargetState)
			{
				this.UpdateVisuals(this.NormalizedHoldTime, 1f);
				this.RefreshPosition(hub);
			}
			else if (this.State != CloudState.Spawning)
			{
				this._localOwner = false;
			}
		}
	}

	private void UpdateVisuals(float normalizedSize, float lerpTime)
	{
		this._deploySound.mute = ReferenceHub.TryGetPovHub(out var hub) && HitboxIdentity.IsEnemy(Team.SCPs, hub.GetTeam());
		this.TryGetPlayer(out var @is, out var isOwner);
		this._decalProjector.enabled = @is;
		this._t.position = this._syncPos.Position;
		this.UpdateFade(this.State != CloudState.Destroyed);
		this.UpdateRadius(normalizedSize, lerpTime);
		this.UpdateChargeup(normalizedSize, isOwner);
	}

	private void UpdateChargeup(float normalizedSize, bool isOwner)
	{
		this._chargeupSound.mute = !isOwner;
		if (this.State == CloudState.Spawning)
		{
			this._chargeupSound.volume = this._chargeupVolumeOverSize.Evaluate(normalizedSize);
		}
		else
		{
			this._chargeupSound.volume -= Time.deltaTime;
		}
	}

	private void UpdateFade(bool isVisible)
	{
		float b = (isVisible ? 1 : 0);
		DecalProjector decalProjector = this._decalProjector;
		decalProjector.fadeFactor = Mathf.Lerp(t: Time.deltaTime * this._colorLerpTime, a: decalProjector.fadeFactor, b: b);
	}

	private void UpdateRadius(float normSize, float lerpTime)
	{
		float time = normSize * this._maxHoldTime;
		this._prevRange = Mathf.Lerp(this._prevRange, this._rangeOverHeldTime.Evaluate(time), lerpTime);
		this._mat.SetFloat(Scp939AmnesticCloudInstance.HashRadiusPercent, this._prevRange * 2f / this._decalProjector.size.x);
		if (this.State == CloudState.Created)
		{
			float a = this._mat.GetFloat(Scp939AmnesticCloudInstance.HashStatusPercent);
			float t = Time.deltaTime * this._colorLerpTime;
			float value = Mathf.Lerp(a, 1f, t);
			this._mat.SetFloat(Scp939AmnesticCloudInstance.HashStatusPercent, value);
		}
	}

	[Server]
	private void ServerUpdateSpawning()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::ServerUpdateSpawning()' called when server was not active");
			return;
		}
		if (!this._abilitiesSet || !ReferenceHub.TryGetHubNetID(this._syncOwner, out var hub) || this._scpRole == null || this._scpRole.Pooled)
		{
			this.ServerDestroy();
			return;
		}
		this.RefreshPosition(hub);
		this.Network_syncPos = new RelativePosition(this._t.position);
		if (this._cloud.TargetState)
		{
			this._lastHoldTime = this._cloud.HoldDuration;
			this.Network_syncHoldTime = (byte)Mathf.RoundToInt(this.NormalizedHoldTime * 255f);
			if (this._lastHoldTime < this._maxHoldTime)
			{
				return;
			}
		}
		if (this._lastHoldTime < this._minHoldTime && !this._cloud.Cooldown.IsReady)
		{
			this._cloud.ServerFailPlacement();
			this.ServerDestroy();
			return;
		}
		this._targetDuration = this._durationOverHeldTime.Evaluate(this._lastHoldTime);
		this._cloud.ServerConfirmPlacement(this._targetDuration);
		this.MaxDistance = this._rangeOverHeldTime.Evaluate(this._lastHoldTime);
		this.State = CloudState.Created;
		this.RpcPlayCreateSound();
	}

	[Server]
	private void ServerUpdateDestroyed()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::ServerUpdateDestroyed()' called when server was not active");
			return;
		}
		this._destroyTime -= Time.deltaTime;
		if (!(this._destroyTime > 0f))
		{
			NetworkServer.Destroy(base.gameObject);
		}
	}

	[ClientRpc]
	private void RpcPlayCreateSound()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::RpcPlayCreateSound()", -193115792, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	public void ServerSetup(ReferenceHub owner)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::ServerSetup(ReferenceHub)' called when server was not active");
			return;
		}
		this.Network_syncOwner = owner.netId;
		this.SetAbilityCache();
		this._lunge.OnStateChanged += OnLungeStateChanged;
		PlayerStats.OnAnyPlayerDamaged += OnAnyPlayerDamaged;
		this._claw.OnAttacked += OnAttacked;
	}

	static Scp939AmnesticCloudInstance()
	{
		Scp939AmnesticCloudInstance.ActiveInstances = new List<Scp939AmnesticCloudInstance>();
		Scp939AmnesticCloudInstance.HashRadiusPercent = Shader.PropertyToID("_RadiusPercent");
		Scp939AmnesticCloudInstance.HashStatusPercent = Shader.PropertyToID("_StatusPercent");
		RemoteProcedureCalls.RegisterRpc(typeof(Scp939AmnesticCloudInstance), "System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::RpcPlayCreateSound()", InvokeUserCode_RpcPlayCreateSound);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPlayCreateSound()
	{
		if (!this._alreadyCreated)
		{
			this._deploySound.Play();
			if (ReferenceHub.TryGetHubNetID(this._syncOwner, out var hub) && hub.roleManager.CurrentRole is Scp939Role scp939Role && scp939Role.FpcModule.CharacterModelInstance is Scp939Model scp939Model)
			{
				this._alreadyCreated = true;
				scp939Model.PlayCloudRelease();
			}
		}
	}

	protected static void InvokeUserCode_RpcPlayCreateSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayCreateSound called on server.");
		}
		else
		{
			((Scp939AmnesticCloudInstance)obj).UserCode_RpcPlayCreateSound();
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncHoldTime);
			NetworkWriterExtensions.WriteByte(writer, this._syncState);
			writer.WriteUInt(this._syncOwner);
			writer.WriteRelativePosition(this._syncPos);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncHoldTime);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncState);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteUInt(this._syncOwner);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteRelativePosition(this._syncPos);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncHoldTime, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncState, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncOwner, null, reader.ReadUInt());
			base.GeneratedSyncVarDeserialize(ref this._syncPos, null, reader.ReadRelativePosition());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncHoldTime, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncState, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncOwner, null, reader.ReadUInt());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncPos, null, reader.ReadRelativePosition());
		}
	}
}
