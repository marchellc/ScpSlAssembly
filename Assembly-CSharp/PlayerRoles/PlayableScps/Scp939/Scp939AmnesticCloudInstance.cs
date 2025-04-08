using System;
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

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939AmnesticCloudInstance : TemporaryHazard
	{
		public float NormalizedHoldTime
		{
			get
			{
				return Mathf.Clamp01(this._cloud.HoldDuration / this._maxHoldTime);
			}
		}

		public ReferenceHub Owner
		{
			get
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHubNetID(this._syncOwner, out referenceHub))
				{
					return null;
				}
				return referenceHub;
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

		public Scp939AmnesticCloudInstance.CloudState State
		{
			get
			{
				return (Scp939AmnesticCloudInstance.CloudState)this._syncState;
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
				if (this.State != Scp939AmnesticCloudInstance.CloudState.Created)
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

		public Vector2 MinMaxTime
		{
			get
			{
				return new Vector2(this._minHoldTime, this._maxHoldTime);
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
			this.State = Scp939AmnesticCloudInstance.CloudState.Destroyed;
		}

		public override bool OnEnter(ReferenceHub player)
		{
			if (!HitboxIdentity.IsEnemy(Team.SCPs, player.GetTeam()) || player.IsFlamingo(true))
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
			if (this.State != Scp939AmnesticCloudInstance.CloudState.Created)
			{
				return;
			}
			if (!this.IsActive)
			{
				return;
			}
			if (!this._overallCooldown.IsReady)
			{
				return;
			}
			AbilityCooldown abilityCooldown;
			if (this._individualCooldown.TryGetValue(player.netId, out abilityCooldown) && !abilityCooldown.IsReady)
			{
				return;
			}
			PlayerEffectsController playerEffectsController = player.playerEffectsController;
			Invigorated invigorated;
			if (playerEffectsController.TryGetEffect<Invigorated>(out invigorated) && invigorated.IsEnabled)
			{
				return;
			}
			playerEffectsController.EnableEffect<AmnesiaVision>(this._amnesiaDuration, false);
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
			using (List<ReferenceHub>.Enumerator enumerator = base.AffectedPlayers.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					AmnesiaVision amnesiaVision;
					if (!enumerator.Current.playerEffectsController.TryGetEffect<AmnesiaVision>(out amnesiaVision))
					{
						return;
					}
					amnesiaVision.IsEnabled = false;
				}
			}
			this._overallCooldown.Trigger((double)this._pauseDuration);
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
			ReferenceHub referenceHub;
			if (ReferenceHub.TryGetHubNetID(this._syncOwner, out referenceHub) && referenceHub.isLocalPlayer)
			{
				this._localOwner = true;
				this.SetAbilityCache();
			}
			ReferenceHub referenceHub2;
			if (this.Owner == null || (ReferenceHub.TryGetPovHub(out referenceHub2) && !(referenceHub2.roleManager.CurrentRole is Scp939Role)))
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
			PlayerStats.OnAnyPlayerDamaged -= this.OnAnyPlayerDamaged;
			if (this._lunge != null)
			{
				this._lunge.OnStateChanged -= this.OnLungeStateChanged;
			}
			if (this._claw != null)
			{
				this._claw.OnAttacked -= this.OnAttacked;
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
				this.UpdateVisuals((float)this._syncHoldTime / 255f, Time.deltaTime * this._sizeLerpTime);
			}
			if (!NetworkServer.active)
			{
				return;
			}
			Scp939AmnesticCloudInstance.CloudState state = this.State;
			if (state == Scp939AmnesticCloudInstance.CloudState.Spawning)
			{
				this.ServerUpdateSpawning();
				return;
			}
			if (state != Scp939AmnesticCloudInstance.CloudState.Destroyed)
			{
				return;
			}
			this.ServerUpdateDestroyed();
		}

		private void TryGetPlayer(out bool is939, out bool isOwner)
		{
			is939 = false;
			isOwner = false;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetPovHub(out referenceHub))
			{
				return;
			}
			is939 = referenceHub.roleManager.CurrentRole is Scp939Role;
			isOwner = referenceHub.netId == this._syncOwner;
		}

		private void OnAttacked(AttackResult attackResult)
		{
			if (attackResult == AttackResult.None)
			{
				return;
			}
			this.PauseAll();
		}

		private void OnAnyPlayerDamaged(ReferenceHub hub, DamageHandlerBase dhb)
		{
			if (hub.netId == this._syncOwner)
			{
				AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
				if (attackerDamageHandler != null)
				{
					AbilityCooldown abilityCooldown = new AbilityCooldown();
					abilityCooldown.Trigger((double)this._pauseDuration);
					uint attackerId = attackerDamageHandler.Attacker.NetId;
					this._individualCooldown[attackerId] = abilityCooldown;
					ReferenceHub referenceHub;
					if (!base.AffectedPlayers.TryGetFirst((ReferenceHub x) => x.netId == attackerId, out referenceHub))
					{
						return;
					}
					AmnesiaVision amnesiaVision;
					if (!referenceHub.playerEffectsController.TryGetEffect<AmnesiaVision>(out amnesiaVision))
					{
						return;
					}
					amnesiaVision.IsEnabled = false;
					return;
				}
			}
		}

		private void OnLungeStateChanged(Scp939LungeState state)
		{
			if (state != Scp939LungeState.LandHit)
			{
				return;
			}
			this.PauseAll();
		}

		private void SetAbilityCache()
		{
			this._abilitiesSet = false;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(this._syncOwner, out referenceHub))
			{
				return;
			}
			Scp939Role scp939Role = referenceHub.roleManager.CurrentRole as Scp939Role;
			if (scp939Role == null)
			{
				return;
			}
			this._scpRole = scp939Role;
			this._abilitiesSet = this._scpRole.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._cloud) && this._scpRole.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lunge) && this._scpRole.SubroutineModule.TryGetSubroutine<Scp939ClawAbility>(out this._claw);
		}

		private void RefreshPosition(ReferenceHub owner)
		{
			this._t.position = owner.PlayerCameraReference.position;
		}

		private void UpdateLocal()
		{
			ReferenceHub referenceHub;
			if (!this._abilitiesSet || !ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			Scp939AmnesticCloudInstance.CloudState state = this.State;
			if (state != Scp939AmnesticCloudInstance.CloudState.Created)
			{
				if (state == Scp939AmnesticCloudInstance.CloudState.Destroyed)
				{
					this._cloud.ClientCancel(Scp939HudTranslation.CloudFailedSizeInsufficient);
				}
			}
			else
			{
				this._cloud.ClientCancel(Scp939HudTranslation.PressKeyToLunge);
			}
			if (!this._cloud.ValidateFloor())
			{
				this._cloud.ClientCancel((this._cloud.HoldDuration < this._minHoldTime) ? Scp939HudTranslation.CloudFailedSizeInsufficient : Scp939HudTranslation.PressKeyToLunge);
			}
			if (this._cloud.TargetState)
			{
				this.UpdateVisuals(this.NormalizedHoldTime, 1f);
				this.RefreshPosition(referenceHub);
				return;
			}
			if (this.State != Scp939AmnesticCloudInstance.CloudState.Spawning)
			{
				this._localOwner = false;
			}
		}

		private void UpdateVisuals(float normalizedSize, float lerpTime)
		{
			ReferenceHub referenceHub;
			this._deploySound.mute = ReferenceHub.TryGetPovHub(out referenceHub) && HitboxIdentity.IsEnemy(Team.SCPs, referenceHub.GetTeam());
			bool flag;
			bool flag2;
			this.TryGetPlayer(out flag, out flag2);
			this._decalProjector.enabled = flag;
			this._t.position = this._syncPos.Position;
			this.UpdateFade(this.State != Scp939AmnesticCloudInstance.CloudState.Destroyed);
			this.UpdateRadius(normalizedSize, lerpTime);
			this.UpdateChargeup(normalizedSize, flag2);
		}

		private void UpdateChargeup(float normalizedSize, bool isOwner)
		{
			this._chargeupSound.mute = !isOwner;
			if (this.State == Scp939AmnesticCloudInstance.CloudState.Spawning)
			{
				this._chargeupSound.volume = this._chargeupVolumeOverSize.Evaluate(normalizedSize);
				return;
			}
			this._chargeupSound.volume -= Time.deltaTime;
		}

		private void UpdateFade(bool isVisible)
		{
			float num = (float)(isVisible ? 1 : 0);
			DecalProjector decalProjector = this._decalProjector;
			float num2 = Time.deltaTime * this._colorLerpTime;
			decalProjector.fadeFactor = Mathf.Lerp(decalProjector.fadeFactor, num, num2);
		}

		private void UpdateRadius(float normSize, float lerpTime)
		{
			float num = normSize * this._maxHoldTime;
			this._prevRange = Mathf.Lerp(this._prevRange, this._rangeOverHeldTime.Evaluate(num), lerpTime);
			this._mat.SetFloat(Scp939AmnesticCloudInstance.HashRadiusPercent, this._prevRange * 2f / this._decalProjector.size.x);
			if (this.State != Scp939AmnesticCloudInstance.CloudState.Created)
			{
				return;
			}
			float @float = this._mat.GetFloat(Scp939AmnesticCloudInstance.HashStatusPercent);
			float num2 = Time.deltaTime * this._colorLerpTime;
			float num3 = Mathf.Lerp(@float, 1f, num2);
			this._mat.SetFloat(Scp939AmnesticCloudInstance.HashStatusPercent, num3);
		}

		[Server]
		private void ServerUpdateSpawning()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::ServerUpdateSpawning()' called when server was not active");
				return;
			}
			ReferenceHub referenceHub;
			if (!this._abilitiesSet || !ReferenceHub.TryGetHubNetID(this._syncOwner, out referenceHub) || this._scpRole == null || this._scpRole.Pooled)
			{
				this.ServerDestroy();
				return;
			}
			this.RefreshPosition(referenceHub);
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
			this.State = Scp939AmnesticCloudInstance.CloudState.Created;
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
			if (this._destroyTime > 0f)
			{
				return;
			}
			NetworkServer.Destroy(base.gameObject);
		}

		[ClientRpc]
		private void RpcPlayCreateSound()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::RpcPlayCreateSound()", -193115792, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
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
			this._lunge.OnStateChanged += this.OnLungeStateChanged;
			PlayerStats.OnAnyPlayerDamaged += this.OnAnyPlayerDamaged;
			this._claw.OnAttacked += this.OnAttacked;
		}

		static Scp939AmnesticCloudInstance()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp939AmnesticCloudInstance), "System.Void PlayerRoles.PlayableScps.Scp939.Scp939AmnesticCloudInstance::RpcPlayCreateSound()", new RemoteCallDelegate(Scp939AmnesticCloudInstance.InvokeUserCode_RpcPlayCreateSound));
		}

		public override bool Weaved()
		{
			return true;
		}

		public byte Network_syncHoldTime
		{
			get
			{
				return this._syncHoldTime;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncHoldTime, 1UL, null);
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
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncState, 2UL, null);
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
				base.GeneratedSyncVarSetter<uint>(value, ref this._syncOwner, 4UL, null);
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
				base.GeneratedSyncVarSetter<RelativePosition>(value, ref this._syncPos, 8UL, null);
			}
		}

		protected void UserCode_RpcPlayCreateSound()
		{
			if (this._alreadyCreated)
			{
				return;
			}
			this._deploySound.Play();
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(this._syncOwner, out referenceHub))
			{
				return;
			}
			Scp939Role scp939Role = referenceHub.roleManager.CurrentRole as Scp939Role;
			if (scp939Role == null)
			{
				return;
			}
			Scp939Model scp939Model = scp939Role.FpcModule.CharacterModelInstance as Scp939Model;
			if (scp939Model == null)
			{
				return;
			}
			this._alreadyCreated = true;
			scp939Model.PlayCloudRelease();
		}

		protected static void InvokeUserCode_RpcPlayCreateSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayCreateSound called on server.");
				return;
			}
			((Scp939AmnesticCloudInstance)obj).UserCode_RpcPlayCreateSound();
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this._syncHoldTime);
				writer.WriteByte(this._syncState);
				writer.WriteUInt(this._syncOwner);
				writer.WriteRelativePosition(this._syncPos);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteByte(this._syncHoldTime);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteByte(this._syncState);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteUInt(this._syncOwner);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteRelativePosition(this._syncPos);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncHoldTime, null, reader.ReadByte());
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncState, null, reader.ReadByte());
				base.GeneratedSyncVarDeserialize<uint>(ref this._syncOwner, null, reader.ReadUInt());
				base.GeneratedSyncVarDeserialize<RelativePosition>(ref this._syncPos, null, reader.ReadRelativePosition());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncHoldTime, null, reader.ReadByte());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncState, null, reader.ReadByte());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<uint>(ref this._syncOwner, null, reader.ReadUInt());
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<RelativePosition>(ref this._syncPos, null, reader.ReadRelativePosition());
			}
		}

		public static readonly List<Scp939AmnesticCloudInstance> ActiveInstances = new List<Scp939AmnesticCloudInstance>();

		private static readonly int HashRadiusPercent = Shader.PropertyToID("_RadiusPercent");

		private static readonly int HashStatusPercent = Shader.PropertyToID("_StatusPercent");

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

		public enum CloudState
		{
			Spawning,
			Created,
			Destroyed
		}
	}
}
