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
			Quaternion localRotation = _gauge.transform.localRotation;
			Quaternion b = Quaternion.Euler(_mask * _values.Evaluate(f));
			_gauge.transform.localRotation = Quaternion.Lerp(localRotation, b, Time.deltaTime * _smoothing);
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
			if (b2 != _prevValue)
			{
				_rend.sharedMaterial = (b ? _onMat : _offMat);
				_prevValue = b2;
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
			return _totalActivationTime;
		}
		set
		{
			Network_totalActivationTime = Mathf.Max(0f, value);
		}
	}

	public float TotalDeactivationTime
	{
		get
		{
			return _totalDeactivationTime;
		}
		set
		{
			Network_totalDeactivationTime = Mathf.Max(0f, value);
		}
	}

	public float DropdownSpeed => _totalActivationTime / _totalDeactivationTime;

	public bool ActivationReady
	{
		get
		{
			if (Activating)
			{
				return _leverStopwatch.Elapsed.TotalSeconds > (double)_leverDelay;
			}
			return false;
		}
	}

	public bool IsOpen
	{
		get
		{
			return HasFlag(_flags, GeneratorFlags.Open);
		}
		set
		{
			ServerSetFlag(GeneratorFlags.Open, value);
		}
	}

	public bool IsUnlocked
	{
		get
		{
			return HasFlag(_flags, GeneratorFlags.Unlocked);
		}
		set
		{
			ServerSetFlag(GeneratorFlags.Unlocked, value);
		}
	}

	public float TimeLeft => (float)_leverStopwatch.Elapsed.TotalSeconds - _leverDelay;

	public float ActivationTime => _leverDelay;

	public bool Engaged
	{
		get
		{
			return HasFlag(_flags, GeneratorFlags.Engaged);
		}
		set
		{
			ServerSetFlag(GeneratorFlags.Engaged, value);
		}
	}

	public bool Activating
	{
		get
		{
			return HasFlag(_flags, GeneratorFlags.Activating);
		}
		set
		{
			ServerSetFlag(GeneratorFlags.Activating, value);
		}
	}

	public short RemainingTime
	{
		get
		{
			return _syncTime;
		}
		set
		{
			_currentTime = _totalActivationTime - (float)value;
		}
	}

	public DoorPermissionFlags RequiredPermissions
	{
		get
		{
			return _requiredPermission;
		}
		set
		{
			_requiredPermission = value;
		}
	}

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public DoorPermissionsPolicy PermissionsPolicy => new DoorPermissionsPolicy(RequiredPermissions);

	[field: SerializeField]
	public string RequesterLogSignature { get; private set; }

	public float Network_totalActivationTime
	{
		get
		{
			return _totalActivationTime;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _totalActivationTime, 1uL, null);
		}
	}

	public float Network_totalDeactivationTime
	{
		get
		{
			return _totalDeactivationTime;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _totalDeactivationTime, 2uL, null);
		}
	}

	public byte Network_flags
	{
		get
		{
			return _flags;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _flags, 4uL, null);
		}
	}

	public short Network_syncTime
	{
		get
		{
			return _syncTime;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _syncTime, 8uL, null);
		}
	}

	public static event Action<Scp079Generator, Footprint> OnGeneratorEngaged;

	public static event Action<Scp079Generator> OnCount;

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if ((_cooldownStopwatch.IsRunning && _cooldownStopwatch.Elapsed.TotalSeconds < (double)_targetCooldown) || (colliderId != 0 && !HasFlag(_flags, GeneratorFlags.Open)))
		{
			return;
		}
		_cooldownStopwatch.Stop();
		GeneratorColliderId generatorColliderId = (GeneratorColliderId)colliderId;
		PlayerInteractingGeneratorEventArgs playerInteractingGeneratorEventArgs = new PlayerInteractingGeneratorEventArgs(ply, this, generatorColliderId);
		PlayerEvents.OnInteractingGenerator(playerInteractingGeneratorEventArgs);
		if (!playerInteractingGeneratorEventArgs.IsAllowed)
		{
			_cooldownStopwatch.Restart();
			return;
		}
		switch (generatorColliderId)
		{
		case GeneratorColliderId.Door:
		{
			if (HasFlag(_flags, GeneratorFlags.Unlocked))
			{
				if (HasFlag(_flags, GeneratorFlags.Open))
				{
					PlayerClosingGeneratorEventArgs playerClosingGeneratorEventArgs = new PlayerClosingGeneratorEventArgs(ply, this);
					PlayerEvents.OnClosingGenerator(playerClosingGeneratorEventArgs);
					if (playerClosingGeneratorEventArgs.PlayDeniedAnimation)
					{
						RpcDenied(ply.GetCombinedPermissions(this));
					}
					if (!playerClosingGeneratorEventArgs.IsAllowed)
					{
						break;
					}
				}
				else
				{
					PlayerOpeningGeneratorEventArgs playerOpeningGeneratorEventArgs = new PlayerOpeningGeneratorEventArgs(ply, this);
					PlayerEvents.OnOpeningGenerator(playerOpeningGeneratorEventArgs);
					if (playerOpeningGeneratorEventArgs.PlayDeniedAnimation)
					{
						RpcDenied(ply.GetCombinedPermissions(this));
					}
					if (!playerOpeningGeneratorEventArgs.IsAllowed)
					{
						break;
					}
				}
				ServerSetFlag(GeneratorFlags.Open, !HasFlag(_flags, GeneratorFlags.Open));
				_targetCooldown = _doorToggleCooldownTime;
				if (!HasFlag(_flags, GeneratorFlags.Open))
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
			bool flag = PermissionsPolicy.CheckPermissions(ply, this, out callback);
			PlayerUnlockingGeneratorEventArgs obj = new PlayerUnlockingGeneratorEventArgs(ply, this)
			{
				IsAllowed = flag
			};
			PlayerEvents.OnUnlockingGenerator(obj);
			if (!obj.IsAllowed)
			{
				flag = false;
			}
			if (!flag)
			{
				if (!(_deniedStopwatch.Elapsed.TotalSeconds < (double)_deniedCooldownTime))
				{
					_deniedStopwatch.Restart();
					RpcDenied(ply.GetCombinedPermissions(this));
					callback?.Invoke(this, success: false);
				}
			}
			else
			{
				_targetCooldown = _unlockCooldownTime;
				callback?.Invoke(this, success: true);
				IsUnlocked = true;
				PlayerEvents.OnUnlockedGenerator(new PlayerUnlockedGeneratorEventArgs(ply, this));
			}
			break;
		}
		case GeneratorColliderId.Switch:
			if ((ply.IsSCP() && !Activating) || Engaged)
			{
				break;
			}
			if (!Activating)
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
				PlayerDeactivatingGeneratorEventArgs playerDeactivatingGeneratorEventArgs2 = new PlayerDeactivatingGeneratorEventArgs(ply, this);
				PlayerEvents.OnDeactivatingGenerator(playerDeactivatingGeneratorEventArgs2);
				if (!playerDeactivatingGeneratorEventArgs2.IsAllowed)
				{
					break;
				}
			}
			Activating = !Activating;
			if (Activating)
			{
				_leverStopwatch.Restart();
				_lastActivator = new Footprint(ply);
			}
			else
			{
				_lastActivator = default(Footprint);
			}
			_targetCooldown = _doorToggleCooldownTime;
			if (Activating)
			{
				PlayerEvents.OnActivatedGenerator(new PlayerActivatedGeneratorEventArgs(ply, this));
			}
			else
			{
				PlayerEvents.OnDeactivatedGenerator(new PlayerDeactivatedGeneratorEventArgs(ply, this));
			}
			break;
		case GeneratorColliderId.CancelButton:
			if (Activating && !Engaged)
			{
				PlayerDeactivatingGeneratorEventArgs playerDeactivatingGeneratorEventArgs = new PlayerDeactivatingGeneratorEventArgs(ply, this);
				PlayerEvents.OnDeactivatingGenerator(playerDeactivatingGeneratorEventArgs);
				if (playerDeactivatingGeneratorEventArgs.IsAllowed)
				{
					ServerSetFlag(GeneratorFlags.Activating, state: false);
					_targetCooldown = _unlockCooldownTime;
					_lastActivator = default(Footprint);
					PlayerEvents.OnDeactivatedGenerator(new PlayerDeactivatedGeneratorEventArgs(ply, this));
				}
			}
			break;
		default:
			_targetCooldown = 1f;
			break;
		}
		_cooldownStopwatch.Restart();
		PlayerEvents.OnInteractedGenerator(new PlayerInteractedGeneratorEventArgs(ply, this, (GeneratorColliderId)colliderId));
	}

	protected override void Start()
	{
		base.Start();
		_permsIndicator.Register(this);
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
			ServerUpdate();
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
		bool flag = _currentTime >= _totalActivationTime;
		if (!flag)
		{
			int num = Mathf.FloorToInt(_totalActivationTime - _currentTime);
			if (num != _syncTime)
			{
				Network_syncTime = (short)num;
			}
		}
		if (ActivationReady)
		{
			if (flag && !Engaged)
			{
				GeneratorActivatingEventArgs generatorActivatingEventArgs = new GeneratorActivatingEventArgs(this);
				ServerEvents.OnGeneratorActivating(generatorActivatingEventArgs);
				if (generatorActivatingEventArgs.IsAllowed)
				{
					Engaged = true;
					Activating = false;
					Scp079Generator.OnGeneratorEngaged?.Invoke(this, _lastActivator);
					ServerEvents.OnGeneratorActivated(new GeneratorActivatedEventArgs(this));
				}
				return;
			}
			_currentTime += Time.deltaTime;
		}
		else
		{
			if (_currentTime == 0f || flag)
			{
				return;
			}
			_currentTime -= DropdownSpeed * Time.deltaTime;
		}
		_currentTime = Mathf.Clamp(_currentTime, 0f, _totalActivationTime);
	}

	[ClientRpc]
	public void RpcDenied(DoorPermissionFlags flags)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(writer, flags);
		SendRPCInternal("System.Void MapGeneration.Distributors.Scp079Generator::RpcDenied(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", 1988689264, writer, 0, includeOwner: true);
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
		GeneratorFlags flags = (GeneratorFlags)_flags;
		flags = ((!state) ? ((GeneratorFlags)((uint)flags & (uint)(byte)(~(int)flag))) : (flags | flag));
		byte b = (byte)flags;
		if (b != _flags)
		{
			Network_flags = b;
		}
	}

	static Scp079Generator()
	{
		Scp079Generator.OnCount = delegate
		{
		};
		DoorAnimHash = Animator.StringToHash("isOpen");
		LeverAnimHash = Animator.StringToHash("isOn");
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
			writer.WriteFloat(_totalActivationTime);
			writer.WriteFloat(_totalDeactivationTime);
			NetworkWriterExtensions.WriteByte(writer, _flags);
			writer.WriteShort(_syncTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(_totalActivationTime);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteFloat(_totalDeactivationTime);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, _flags);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteShort(_syncTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref _totalActivationTime, null, reader.ReadFloat());
			GeneratedSyncVarDeserialize(ref _totalDeactivationTime, null, reader.ReadFloat());
			GeneratedSyncVarDeserialize(ref _flags, null, NetworkReaderExtensions.ReadByte(reader));
			GeneratedSyncVarDeserialize(ref _syncTime, null, reader.ReadShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _totalActivationTime, null, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _totalDeactivationTime, null, reader.ReadFloat());
		}
		if ((num & 4L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _flags, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 8L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _syncTime, null, reader.ReadShort());
		}
	}
}
