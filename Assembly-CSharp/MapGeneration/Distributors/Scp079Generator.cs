using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Footprinting;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using InventorySystem.Items.Keycards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using TMPro;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class Scp079Generator : SpawnableStructure, IServerInteractable, IInteractable
	{
		public static event Action<Scp079Generator, Footprint> OnGeneratorEngaged;

		public static event Action<Scp079Generator> OnCount;

		public float TotalActivationTime
		{
			get
			{
				return this._totalActivationTime;
			}
			set
			{
				this.Network_totalActivationTime = Mathf.Max(0f, value);
			}
		}

		public float TotalDeactivationTime
		{
			get
			{
				return this._totalDeactivationTime;
			}
			set
			{
				this.Network_totalDeactivationTime = Mathf.Max(0f, value);
			}
		}

		public float DropdownSpeed
		{
			get
			{
				return this._totalActivationTime / this._totalDeactivationTime;
			}
		}

		public bool ActivationReady
		{
			get
			{
				return this.Activating && this._leverStopwatch.Elapsed.TotalSeconds > (double)this._leverDelay;
			}
		}

		public bool IsOpen
		{
			get
			{
				return this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Open);
			}
			set
			{
				this.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, value);
			}
		}

		public bool IsUnlocked
		{
			get
			{
				return this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Unlocked);
			}
			set
			{
				this.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, value);
			}
		}

		public float TimeLeft
		{
			get
			{
				return (float)this._leverStopwatch.Elapsed.TotalSeconds - this._leverDelay;
			}
		}

		public float ActivationTime
		{
			get
			{
				return this._leverDelay;
			}
		}

		public bool Engaged
		{
			get
			{
				return this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Engaged);
			}
			set
			{
				this.ServerSetFlag(Scp079Generator.GeneratorFlags.Engaged, value);
			}
		}

		public RoomIdentifier Room { get; private set; }

		public bool Activating
		{
			get
			{
				return this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Activating);
			}
			set
			{
				this.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, value);
			}
		}

		public short RemainingTime
		{
			get
			{
				return this._syncTime;
			}
			set
			{
				this._currentTime = this._totalActivationTime - (float)value;
			}
		}

		public KeycardPermissions RequiredPermissions
		{
			get
			{
				return this._requiredPermission;
			}
			set
			{
				this._requiredPermission = value;
			}
		}

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (this._cooldownStopwatch.IsRunning && this._cooldownStopwatch.Elapsed.TotalSeconds < (double)this._targetCooldown)
			{
				return;
			}
			if (colliderId != 0 && !this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Open))
			{
				return;
			}
			this._cooldownStopwatch.Stop();
			PlayerInteractingGeneratorEventArgs playerInteractingGeneratorEventArgs = new PlayerInteractingGeneratorEventArgs(ply, this, (Scp079Generator.GeneratorColliderId)colliderId);
			PlayerEvents.OnInteractingGenerator(playerInteractingGeneratorEventArgs);
			if (!playerInteractingGeneratorEventArgs.IsAllowed)
			{
				this._cooldownStopwatch.Restart();
				return;
			}
			switch (colliderId)
			{
			case 0:
				if (this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Unlocked))
				{
					if (this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Open))
					{
						PlayerClosingGeneratorEventArgs playerClosingGeneratorEventArgs = new PlayerClosingGeneratorEventArgs(ply, this);
						PlayerEvents.OnClosingGenerator(playerClosingGeneratorEventArgs);
						if (!playerClosingGeneratorEventArgs.IsAllowed)
						{
							break;
						}
					}
					else
					{
						PlayerOpeningGeneratorEventArgs playerOpeningGeneratorEventArgs = new PlayerOpeningGeneratorEventArgs(ply, this);
						PlayerEvents.OnOpeningGenerator(playerOpeningGeneratorEventArgs);
						if (!playerOpeningGeneratorEventArgs.IsAllowed)
						{
							break;
						}
					}
					this.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, !this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Open));
					this._targetCooldown = this._doorToggleCooldownTime;
					if (!this.HasFlag(this._flags, Scp079Generator.GeneratorFlags.Open))
					{
						PlayerEvents.OnClosedGenerator(new PlayerClosedGeneratorEventArgs(ply, this));
					}
					else
					{
						PlayerEvents.OnOpenedGenerator(new PlayerOpenedGeneratorEventArgs(ply, this));
					}
				}
				else
				{
					bool flag;
					if (!ply.serverRoles.BypassMode)
					{
						if (ply.inventory.CurInstance != null)
						{
							KeycardItem keycardItem = ply.inventory.CurInstance as KeycardItem;
							if (keycardItem != null)
							{
								flag = keycardItem.Permissions.HasFlagFast(this._requiredPermission);
								goto IL_0172;
							}
						}
						flag = false;
					}
					else
					{
						flag = true;
					}
					IL_0172:
					bool flag2 = flag;
					PlayerUnlockingGeneratorEventArgs playerUnlockingGeneratorEventArgs = new PlayerUnlockingGeneratorEventArgs(ply, this);
					playerUnlockingGeneratorEventArgs.IsAllowed = flag2;
					PlayerEvents.OnUnlockingGenerator(playerUnlockingGeneratorEventArgs);
					if (!playerUnlockingGeneratorEventArgs.IsAllowed)
					{
						flag2 = false;
					}
					if (!flag2)
					{
						this._targetCooldown = this._unlockCooldownTime;
						this.RpcDenied();
						PlayerEvents.OnUnlockedGenerator(new PlayerUnlockedGeneratorEventArgs(ply, this));
					}
					else
					{
						PlayerEvents.OnUnlockedGenerator(new PlayerUnlockedGeneratorEventArgs(ply, this));
						this.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);
					}
				}
				break;
			case 1:
				if ((!ply.IsSCP(true) || this.Activating) && !this.Engaged)
				{
					if (!this.Activating)
					{
						PlayerActivatingGeneratorEventArgs playerActivatingGeneratorEventArgs = new PlayerActivatingGeneratorEventArgs(ply, this);
						PlayerEvents.OnActivatingGenerator(playerActivatingGeneratorEventArgs);
						if (!playerActivatingGeneratorEventArgs.IsAllowed)
						{
							break;
						}
					}
					else
					{
						PlayerDeactivatingGeneratorEventArgs playerDeactivatingGeneratorEventArgs = new PlayerDeactivatingGeneratorEventArgs(ply, this);
						PlayerEvents.OnDeactivatingGenerator(playerDeactivatingGeneratorEventArgs);
						if (!playerDeactivatingGeneratorEventArgs.IsAllowed)
						{
							break;
						}
					}
					this.Activating = !this.Activating;
					if (this.Activating)
					{
						this._leverStopwatch.Restart();
						this._lastActivator = new Footprint(ply);
					}
					else
					{
						this._lastActivator = default(Footprint);
					}
					this._targetCooldown = this._doorToggleCooldownTime;
					if (this.Activating)
					{
						PlayerEvents.OnActivatedGenerator(new PlayerActivatedGeneratorEventArgs(ply, this));
					}
					else
					{
						PlayerEvents.OnDeactivatedGenerator(new PlayerDeactivatedGeneratorEventArgs(ply, this));
					}
				}
				break;
			case 2:
				if (this.Activating && !this.Engaged)
				{
					PlayerDeactivatingGeneratorEventArgs playerDeactivatingGeneratorEventArgs2 = new PlayerDeactivatingGeneratorEventArgs(ply, this);
					PlayerEvents.OnDeactivatingGenerator(playerDeactivatingGeneratorEventArgs2);
					if (playerDeactivatingGeneratorEventArgs2.IsAllowed)
					{
						this.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, false);
						this._targetCooldown = this._unlockCooldownTime;
						this._lastActivator = default(Footprint);
						PlayerEvents.OnDeactivatedGenerator(new PlayerDeactivatedGeneratorEventArgs(ply, this));
					}
				}
				break;
			default:
				this._targetCooldown = 1f;
				break;
			}
			this._cooldownStopwatch.Restart();
			PlayerEvents.OnInteractedGenerator(new PlayerInteractedGeneratorEventArgs(ply, this, (Scp079Generator.GeneratorColliderId)colliderId));
		}

		protected override void Start()
		{
			Scp079Recontainer.AllGenerators.Add(this);
			base.Start();
			this.Room = RoomUtils.RoomAtPosition(base.transform.position);
		}

		protected override void OnDestroy()
		{
			Scp079Recontainer.AllGenerators.Remove(this);
			base.OnDestroy();
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				this.ServerUpdate();
			}
		}

		[Server]
		private void ServerUpdate()
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogWarning("[Server] function 'System.Void MapGeneration.Distributors.Scp079Generator::ServerUpdate()' called when server was not active");
				return;
			}
			bool flag = this._currentTime >= this._totalActivationTime;
			if (!flag)
			{
				int num = Mathf.FloorToInt(this._totalActivationTime - this._currentTime);
				if (num != (int)this._syncTime)
				{
					this.Network_syncTime = (short)num;
				}
			}
			if (this.ActivationReady)
			{
				if (flag && !this.Engaged)
				{
					GeneratorActivatingEventArgs generatorActivatingEventArgs = new GeneratorActivatingEventArgs(this);
					ServerEvents.OnGeneratorActivating(generatorActivatingEventArgs);
					if (!generatorActivatingEventArgs.IsAllowed)
					{
						return;
					}
					this.Engaged = true;
					this.Activating = false;
					Action<Scp079Generator, Footprint> onGeneratorEngaged = Scp079Generator.OnGeneratorEngaged;
					if (onGeneratorEngaged != null)
					{
						onGeneratorEngaged(this, this._lastActivator);
					}
					ServerEvents.OnGeneratorActivated(new GeneratorActivatedEventArgs(this));
					return;
				}
				else
				{
					this._currentTime += Time.deltaTime;
				}
			}
			else
			{
				if (this._currentTime == 0f || flag)
				{
					return;
				}
				this._currentTime -= this.DropdownSpeed * Time.deltaTime;
			}
			this._currentTime = Mathf.Clamp(this._currentTime, 0f, this._totalActivationTime);
		}

		[ClientRpc]
		private void RpcDenied()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void MapGeneration.Distributors.Scp079Generator::RpcDenied()", 130217412, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private bool HasFlag(byte flags, Scp079Generator.GeneratorFlags flag)
		{
			return (flags & (byte)flag) == (byte)flag;
		}

		[Server]
		private void ServerSetFlag(Scp079Generator.GeneratorFlags flag, bool state)
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogWarning("[Server] function 'System.Void MapGeneration.Distributors.Scp079Generator::ServerSetFlag(MapGeneration.Distributors.Scp079Generator/GeneratorFlags,System.Boolean)' called when server was not active");
				return;
			}
			Scp079Generator.GeneratorFlags generatorFlags = (Scp079Generator.GeneratorFlags)this._flags;
			if (state)
			{
				generatorFlags |= flag;
			}
			else
			{
				generatorFlags &= ~flag;
			}
			byte b = (byte)generatorFlags;
			if (b != this._flags)
			{
				this.Network_flags = b;
			}
		}

		static Scp079Generator()
		{
			Scp079Generator.OnCount = delegate(Scp079Generator generator)
			{
			};
			Scp079Generator.DoorAnimHash = Animator.StringToHash("isOpen");
			Scp079Generator.LeverAnimHash = Animator.StringToHash("isOn");
			RemoteProcedureCalls.RegisterRpc(typeof(Scp079Generator), "System.Void MapGeneration.Distributors.Scp079Generator::RpcDenied()", new RemoteCallDelegate(Scp079Generator.InvokeUserCode_RpcDenied));
		}

		public override bool Weaved()
		{
			return true;
		}

		public float Network_totalActivationTime
		{
			get
			{
				return this._totalActivationTime;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this._totalActivationTime, 1UL, null);
			}
		}

		public float Network_totalDeactivationTime
		{
			get
			{
				return this._totalDeactivationTime;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this._totalDeactivationTime, 2UL, null);
			}
		}

		public byte Network_flags
		{
			get
			{
				return this._flags;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this._flags, 4UL, null);
			}
		}

		public short Network_syncTime
		{
			get
			{
				return this._syncTime;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<short>(value, ref this._syncTime, 8UL, null);
			}
		}

		protected void UserCode_RpcDenied()
		{
			this._deniedStopwatch.Restart();
			this._deniedCooldown = this._deniedCooldownTime;
		}

		protected static void InvokeUserCode_RpcDenied(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				global::UnityEngine.Debug.LogError("RPC RpcDenied called on server.");
				return;
			}
			((Scp079Generator)obj).UserCode_RpcDenied();
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteFloat(this._totalActivationTime);
				writer.WriteFloat(this._totalDeactivationTime);
				writer.WriteByte(this._flags);
				writer.WriteShort(this._syncTime);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteFloat(this._totalActivationTime);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteFloat(this._totalDeactivationTime);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteByte(this._flags);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteShort(this._syncTime);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._totalActivationTime, null, reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this._totalDeactivationTime, null, reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<byte>(ref this._flags, null, reader.ReadByte());
				base.GeneratedSyncVarDeserialize<short>(ref this._syncTime, null, reader.ReadShort());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._totalActivationTime, null, reader.ReadFloat());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._totalDeactivationTime, null, reader.ReadFloat());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._flags, null, reader.ReadByte());
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<short>(ref this._syncTime, null, reader.ReadShort());
			}
		}

		[SerializeField]
		private Animator _doorAnimator;

		[SerializeField]
		private Animator _leverAnimator;

		[SerializeField]
		private AudioSource _audioSource;

		[SerializeField]
		private AudioClip _deniedClip;

		[SerializeField]
		private AudioClip _unlockClip;

		[SerializeField]
		private AudioClip _openClip;

		[SerializeField]
		private AudioClip _closeClip;

		[SerializeField]
		private AudioClip _countdownClip;

		[SerializeField]
		private Renderer _keycardRenderer;

		[SerializeField]
		private Material _lockedMaterial;

		[SerializeField]
		private Material _unlockedMaterial;

		[SerializeField]
		private Material _deniedMaterial;

		[SerializeField]
		private float _deniedCooldownTime;

		[SerializeField]
		private float _doorToggleCooldownTime;

		[SerializeField]
		private float _unlockCooldownTime;

		[SerializeField]
		private KeycardPermissions _requiredPermission;

		[SerializeField]
		private float _leverDelay;

		[SyncVar]
		[SerializeField]
		private float _totalActivationTime;

		[SyncVar]
		[SerializeField]
		private float _totalDeactivationTime;

		[SerializeField]
		private Scp079Generator.GeneratorGauge _localGauge;

		[SerializeField]
		private Scp079Generator.GeneratorGauge _totalGauge;

		[SerializeField]
		private Scp079Generator.GeneratorLED _onLED;

		[SerializeField]
		private Scp079Generator.GeneratorLED _offLED;

		[SerializeField]
		private Scp079Generator.GeneratorLED[] _waitLights;

		[SerializeField]
		private TextMeshProUGUI _screen;

		[Multiline]
		[SerializeField]
		private string _screenCountdown;

		[Multiline]
		[SerializeField]
		private string _screenEngaged;

		[Multiline]
		[SerializeField]
		private string _screenOffline;

		[SyncVar]
		private byte _flags;

		[SyncVar]
		private short _syncTime;

		private static readonly int DoorAnimHash;

		private static readonly int LeverAnimHash;

		private short _prevTime;

		private byte _prevFlags;

		private float _targetCooldown;

		private float _deniedCooldown;

		private float _currentTime;

		private Footprint _lastActivator;

		private readonly Stopwatch _cooldownStopwatch = new Stopwatch();

		private readonly Stopwatch _deniedStopwatch = new Stopwatch();

		private readonly Stopwatch _leverStopwatch = new Stopwatch();

		private const float UnlockTokenReward = 0.5f;

		private const float EngageTokenReward = 1f;

		[Serializable]
		private class GeneratorGauge
		{
			public void UpdateValue(float f)
			{
				Quaternion localRotation = this._gauge.transform.localRotation;
				Quaternion quaternion = Quaternion.Euler(this._mask * this._values.Evaluate(f));
				this._gauge.transform.localRotation = Quaternion.Lerp(localRotation, quaternion, Time.deltaTime * this._smoothing);
			}

			[SerializeField]
			private Transform _gauge;

			[SerializeField]
			private Vector3 _mask;

			[SerializeField]
			private AnimationCurve _values;

			[SerializeField]
			private float _smoothing;
		}

		[Serializable]
		private class GeneratorLED
		{
			public void UpdateValue(bool b)
			{
				byte b2 = (b ? 1 : 2);
				if (b2 != this._prevValue)
				{
					this._rend.sharedMaterial = (b ? this._onMat : this._offMat);
					this._prevValue = b2;
				}
			}

			[SerializeField]
			private Renderer _rend;

			[SerializeField]
			private Material _onMat;

			[SerializeField]
			private Material _offMat;

			private byte _prevValue;
		}

		[Flags]
		public enum GeneratorFlags : byte
		{
			None = 1,
			Unlocked = 2,
			Open = 4,
			Activating = 8,
			Engaged = 16
		}

		public enum GeneratorColliderId : byte
		{
			Door,
			Switch,
			CancelButton
		}
	}
}
