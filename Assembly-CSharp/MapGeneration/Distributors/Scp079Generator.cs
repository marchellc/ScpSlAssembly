using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Footprinting;
using Interactables;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using TMPro;
using UnityEngine;

namespace MapGeneration.Distributors;

public class Scp079Generator : SpawnableStructure, IServerInteractable, IInteractable, IDoorPermissionRequester
{
	[Serializable]
	private class GeneratorGauge
	{
		[SerializeField]
		private Transform _gauge;

		[SerializeField]
		private Vector3 _mask;

		[SerializeField]
		private AnimationCurve _values;

		[SerializeField]
		private float _smoothing;

		public void UpdateValue(float f)
		{
			Quaternion localRotation = this._gauge.transform.localRotation;
			Quaternion b = Quaternion.Euler(this._mask * this._values.Evaluate(f));
			this._gauge.transform.localRotation = Quaternion.Lerp(localRotation, b, Time.deltaTime * this._smoothing);
		}
	}

	[Serializable]
	private class GeneratorLED
	{
		[SerializeField]
		private Renderer _rend;

		[SerializeField]
		private Material _onMat;

		[SerializeField]
		private Material _offMat;

		private byte _prevValue;

		public void UpdateValue(bool b)
		{
			byte b2 = (byte)(b ? 1u : 2u);
			if (b2 != this._prevValue)
			{
				this._rend.sharedMaterial = (b ? this._onMat : this._offMat);
				this._prevValue = b2;
			}
		}
	}

	[Flags]
	public enum GeneratorFlags : byte
	{
		None = 1,
		Unlocked = 2,
		Open = 4,
		Activating = 8,
		Engaged = 0x10
	}

	public enum GeneratorColliderId : byte
	{
		Door,
		Switch,
		CancelButton
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
	private KeycardScannerNfcIcon _nfcIcon;

	[SerializeField]
	private KeycardScannerPermsIndicator _permsIndicator;

	[SerializeField]
	private float _deniedCooldownTime;

	[SerializeField]
	private float _doorToggleCooldownTime;

	[SerializeField]
	private float _unlockCooldownTime;

	[SerializeField]
	private DoorPermissionFlags _requiredPermission;

	[SerializeField]
	private float _leverDelay;

	[SyncVar]
	[SerializeField]
	private float _totalActivationTime;

	[SyncVar]
	[SerializeField]
	private float _totalDeactivationTime;

	[SerializeField]
	private GeneratorGauge _localGauge;

	[SerializeField]
	private GeneratorGauge _totalGauge;

	[SerializeField]
	private GeneratorLED _onLED;

	[SerializeField]
	private GeneratorLED _offLED;

	[SerializeField]
	private GeneratorLED[] _waitLights;

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

	private float _currentTime;

	private Footprint _lastActivator;

	private readonly Stopwatch _cooldownStopwatch = new Stopwatch();

	private readonly Stopwatch _leverStopwatch = new Stopwatch();

	private readonly Stopwatch _deniedStopwatch = Stopwatch.StartNew();

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

	public float DropdownSpeed => this._totalActivationTime / this._totalDeactivationTime;

	public bool ActivationReady
	{
		get
		{
			if (this.Activating)
			{
				return this._leverStopwatch.Elapsed.TotalSeconds > (double)this._leverDelay;
			}
			return false;
		}
	}

	public bool IsOpen
	{
		get
		{
			return this.HasFlag(this._flags, GeneratorFlags.Open);
		}
		set
		{
			this.ServerSetFlag(GeneratorFlags.Open, value);
		}
	}

	public bool IsUnlocked
	{
		get
		{
			return this.HasFlag(this._flags, GeneratorFlags.Unlocked);
		}
		set
		{
			this.ServerSetFlag(GeneratorFlags.Unlocked, value);
		}
	}

	public float TimeLeft => (float)this._leverStopwatch.Elapsed.TotalSeconds - this._leverDelay;

	public float ActivationTime => this._leverDelay;

	public bool Engaged
	{
		get
		{
			return this.HasFlag(this._flags, GeneratorFlags.Engaged);
		}
		set
		{
			this.ServerSetFlag(GeneratorFlags.Engaged, value);
		}
	}

	public bool Activating
	{
		get
		{
			return this.HasFlag(this._flags, GeneratorFlags.Activating);
		}
		set
		{
			this.ServerSetFlag(GeneratorFlags.Activating, value);
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

	public DoorPermissionFlags RequiredPermissions
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

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public DoorPermissionsPolicy PermissionsPolicy => new DoorPermissionsPolicy(this.RequiredPermissions);

	[field: SerializeField]
	public string RequesterLogSignature { get; private set; }

	public float Network_totalActivationTime
	{
		get
		{
			return this._totalActivationTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._totalActivationTime, 1uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._totalDeactivationTime, 2uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._flags, 4uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._syncTime, 8uL, null);
		}
	}

	public static event Action<Scp079Generator, Footprint> OnGeneratorEngaged;

	public static event Action<Scp079Generator> OnCount;

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if ((this._cooldownStopwatch.IsRunning && this._cooldownStopwatch.Elapsed.TotalSeconds < (double)this._targetCooldown) || (colliderId != 0 && !this.HasFlag(this._flags, GeneratorFlags.Open)))
		{
			return;
		}
		this._cooldownStopwatch.Stop();
		GeneratorColliderId generatorColliderId = (GeneratorColliderId)colliderId;
		PlayerInteractingGeneratorEventArgs e = new PlayerInteractingGeneratorEventArgs(ply, this, generatorColliderId);
		PlayerEvents.OnInteractingGenerator(e);
		if (!e.IsAllowed)
		{
			this._cooldownStopwatch.Restart();
			return;
		}
		switch (generatorColliderId)
		{
		case GeneratorColliderId.Door:
		{
			if (this.HasFlag(this._flags, GeneratorFlags.Unlocked))
			{
				if (this.HasFlag(this._flags, GeneratorFlags.Open))
				{
					PlayerClosingGeneratorEventArgs e3 = new PlayerClosingGeneratorEventArgs(ply, this);
					PlayerEvents.OnClosingGenerator(e3);
					if (e3.PlayDeniedAnimation)
					{
						this.RpcDenied(ply.GetCombinedPermissions(this));
					}
					if (!e3.IsAllowed)
					{
						break;
					}
				}
				else
				{
					PlayerOpeningGeneratorEventArgs e4 = new PlayerOpeningGeneratorEventArgs(ply, this);
					PlayerEvents.OnOpeningGenerator(e4);
					if (e4.PlayDeniedAnimation)
					{
						this.RpcDenied(ply.GetCombinedPermissions(this));
					}
					if (!e4.IsAllowed)
					{
						break;
					}
				}
				this.ServerSetFlag(GeneratorFlags.Open, !this.HasFlag(this._flags, GeneratorFlags.Open));
				this._targetCooldown = this._doorToggleCooldownTime;
				if (!this.HasFlag(this._flags, GeneratorFlags.Open))
				{
					PlayerEvents.OnClosedGenerator(new PlayerClosedGeneratorEventArgs(ply, this));
				}
				else
				{
					PlayerEvents.OnOpenedGenerator(new PlayerOpenedGeneratorEventArgs(ply, this));
				}
				break;
			}
			PermissionUsed callback;
			bool canOpen = this.PermissionsPolicy.CheckPermissions(ply, this, out callback);
			PlayerUnlockingGeneratorEventArgs e5 = new PlayerUnlockingGeneratorEventArgs(ply, this, canOpen);
			PlayerEvents.OnUnlockingGenerator(e5);
			if (!e5.IsAllowed)
			{
				break;
			}
			if (!e5.CanOpen)
			{
				if (!(this._deniedStopwatch.Elapsed.TotalSeconds < (double)this._deniedCooldownTime))
				{
					this._deniedStopwatch.Restart();
					this.RpcDenied(ply.GetCombinedPermissions(this));
					callback?.Invoke(this, success: false);
				}
			}
			else
			{
				this._targetCooldown = this._unlockCooldownTime;
				callback?.Invoke(this, success: true);
				this.IsUnlocked = true;
				PlayerEvents.OnUnlockedGenerator(new PlayerUnlockedGeneratorEventArgs(ply, this));
			}
			break;
		}
		case GeneratorColliderId.Switch:
			if ((ply.IsSCP() && !this.Activating) || this.Engaged)
			{
				break;
			}
			if (!this.Activating)
			{
				PlayerActivatingGeneratorEventArgs e6 = new PlayerActivatingGeneratorEventArgs(ply, this);
				PlayerEvents.OnActivatingGenerator(e6);
				if (!e6.IsAllowed)
				{
					break;
				}
			}
			else
			{
				PlayerDeactivatingGeneratorEventArgs e7 = new PlayerDeactivatingGeneratorEventArgs(ply, this);
				PlayerEvents.OnDeactivatingGenerator(e7);
				if (!e7.IsAllowed)
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
			break;
		case GeneratorColliderId.CancelButton:
			if (this.Activating && !this.Engaged)
			{
				PlayerDeactivatingGeneratorEventArgs e2 = new PlayerDeactivatingGeneratorEventArgs(ply, this);
				PlayerEvents.OnDeactivatingGenerator(e2);
				if (e2.IsAllowed)
				{
					this.ServerSetFlag(GeneratorFlags.Activating, state: false);
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
		PlayerEvents.OnInteractedGenerator(new PlayerInteractedGeneratorEventArgs(ply, this, (GeneratorColliderId)colliderId));
	}

	protected override void Start()
	{
		base.Start();
		this._permsIndicator.Register(this);
		Scp079Recontainer.AllGenerators.Add(this);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp079Recontainer.AllGenerators.Remove(this);
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
			UnityEngine.Debug.LogWarning("[Server] function 'System.Void MapGeneration.Distributors.Scp079Generator::ServerUpdate()' called when server was not active");
			return;
		}
		bool flag = this._currentTime >= this._totalActivationTime;
		if (!flag)
		{
			int num = Mathf.FloorToInt(this._totalActivationTime - this._currentTime);
			if (num != this._syncTime)
			{
				this.Network_syncTime = (short)num;
			}
		}
		if (this.ActivationReady)
		{
			if (flag && !this.Engaged)
			{
				GeneratorActivatingEventArgs e = new GeneratorActivatingEventArgs(this);
				ServerEvents.OnGeneratorActivating(e);
				if (e.IsAllowed)
				{
					this.Engaged = true;
					this.Activating = false;
					Scp079Generator.OnGeneratorEngaged?.Invoke(this, this._lastActivator);
					ServerEvents.OnGeneratorActivated(new GeneratorActivatedEventArgs(this));
				}
				return;
			}
			this._currentTime += Time.deltaTime;
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
	public void RpcDenied(DoorPermissionFlags flags)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(writer, flags);
		this.SendRPCInternal("System.Void MapGeneration.Distributors.Scp079Generator::RpcDenied(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", 1988689264, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private bool HasFlag(byte flags, GeneratorFlags flag)
	{
		return ((uint)flags & (uint)flag) == (uint)flag;
	}

	[Server]
	private void ServerSetFlag(GeneratorFlags flag, bool state)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogWarning("[Server] function 'System.Void MapGeneration.Distributors.Scp079Generator::ServerSetFlag(MapGeneration.Distributors.Scp079Generator/GeneratorFlags,System.Boolean)' called when server was not active");
			return;
		}
		GeneratorFlags flags = (GeneratorFlags)this._flags;
		flags = ((!state) ? ((GeneratorFlags)((uint)flags & (uint)(byte)(~(int)flag))) : (flags | flag));
		byte b = (byte)flags;
		if (b != this._flags)
		{
			this.Network_flags = b;
		}
	}

	static Scp079Generator()
	{
		Scp079Generator.OnCount = delegate
		{
		};
		Scp079Generator.DoorAnimHash = Animator.StringToHash("isOpen");
		Scp079Generator.LeverAnimHash = Animator.StringToHash("isOn");
		RemoteProcedureCalls.RegisterRpc(typeof(Scp079Generator), "System.Void MapGeneration.Distributors.Scp079Generator::RpcDenied(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", InvokeUserCode_RpcDenied__DoorPermissionFlags);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcDenied__DoorPermissionFlags(DoorPermissionFlags flags)
	{
	}

	protected static void InvokeUserCode_RpcDenied__DoorPermissionFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcDenied called on server.");
		}
		else
		{
			((Scp079Generator)obj).UserCode_RpcDenied__DoorPermissionFlags(GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(reader));
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this._totalActivationTime);
			writer.WriteFloat(this._totalDeactivationTime);
			NetworkWriterExtensions.WriteByte(writer, this._flags);
			writer.WriteShort(this._syncTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(this._totalActivationTime);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteFloat(this._totalDeactivationTime);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._flags);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteShort(this._syncTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._totalActivationTime, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._totalDeactivationTime, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._flags, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncTime, null, reader.ReadShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._totalActivationTime, null, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._totalDeactivationTime, null, reader.ReadFloat());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._flags, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncTime, null, reader.ReadShort());
		}
	}
}
