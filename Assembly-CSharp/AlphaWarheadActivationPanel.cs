using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioPooling;
using Interactables;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class AlphaWarheadActivationPanel : NetworkBehaviour, IServerInteractable, IInteractable, IDoorPermissionRequester
{
	private readonly Stopwatch _deniedCooldownSw = Stopwatch.StartNew();

	[SerializeField]
	private KeycardScannerNfcIcon _nfcScanner;

	[SerializeField]
	private KeycardScannerPermsIndicator _permsIndicator;

	[SerializeField]
	private byte _keycardColliderId;

	[SerializeField]
	private byte _activatorColliderId;

	[SerializeField]
	private float _deniedCooldownDuration;

	[SerializeField]
	private AudioClip _grantedClip;

	[SerializeField]
	private AudioClip _deniedClip;

	[SyncVar]
	private bool _unlocked;

	public static AlphaWarheadActivationPanel Instance { get; private set; }

	public static bool IsUnlocked
	{
		get
		{
			if (AlphaWarheadActivationPanel.Instance != null)
			{
				return AlphaWarheadActivationPanel.Instance._unlocked;
			}
			return false;
		}
		set
		{
			if (!(AlphaWarheadActivationPanel.Instance == null))
			{
				AlphaWarheadActivationPanel.Instance.Network_unlocked = value;
				if (value)
				{
					AlphaWarheadActivationPanel.Instance.RpcGranted();
				}
				else
				{
					AlphaWarheadActivationPanel.Instance.RpcReset();
				}
			}
		}
	}

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public DoorPermissionsPolicy PermissionsPolicy => new DoorPermissionsPolicy(DoorPermissionFlags.AlphaWarhead);

	[field: SerializeField]
	public string RequesterLogSignature { get; private set; }

	public bool Network_unlocked
	{
		get
		{
			return this._unlocked;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._unlocked, 1uL, null);
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (colliderId == this._activatorColliderId)
		{
			this.ServerInteractActivator(ply);
		}
		else if (colliderId == this._keycardColliderId)
		{
			this.ServerInteractKeycard(ply);
		}
	}

	private void ServerInteractKeycard(ReferenceHub ply)
	{
		if (!this._unlocked)
		{
			PermissionUsed callback;
			bool isAllowed = this.PermissionsPolicy.CheckPermissions(ply, this, out callback);
			PlayerUnlockingWarheadButtonEventArgs obj = new PlayerUnlockingWarheadButtonEventArgs(ply)
			{
				IsAllowed = isAllowed
			};
			PlayerEvents.OnUnlockingWarheadButton(obj);
			if (obj.IsAllowed)
			{
				AlphaWarheadActivationPanel.IsUnlocked = true;
				callback?.Invoke(this, this);
				PlayerEvents.OnUnlockedWarheadButton(new PlayerUnlockedWarheadButtonEventArgs(ply));
			}
			else if (this._deniedCooldownSw.Elapsed.TotalSeconds > (double)this._deniedCooldownDuration)
			{
				this._deniedCooldownSw.Restart();
				this.RpcDenied(ply.GetCombinedPermissions(this));
				callback?.Invoke(this, success: false);
			}
		}
	}

	private void ServerInteractActivator(ReferenceHub ply)
	{
		if (this._unlocked && !AlphaWarheadController.Singleton.IsLocked && AlphaWarheadOutsitePanel.nukeside.enabled)
		{
			AlphaWarheadController.Singleton.StartDetonation(isAutomatic: false, suppressSubtitles: false, ply);
			ServerLogs.AddLog(ServerLogs.Modules.Warhead, ply.LoggedNameFromRefHub() + " started the Alpha Warhead detonation.", ServerLogs.ServerLogType.GameEvent);
		}
	}

	private void Start()
	{
		this._permsIndicator.Register(this);
		if (this._unlocked)
		{
			this._nfcScanner.SetGranted();
			this._permsIndicator.PlayAccepted(null);
		}
		else
		{
			this._nfcScanner.SetRegular();
		}
	}

	private void Awake()
	{
		AlphaWarheadActivationPanel.Instance = this;
	}

	[ClientRpc]
	private void RpcGranted()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void AlphaWarheadActivationPanel::RpcGranted()", 1769627351, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcDenied(DoorPermissionFlags flags)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(writer, flags);
		this.SendRPCInternal("System.Void AlphaWarheadActivationPanel::RpcDenied(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", -1577515699, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcReset()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void AlphaWarheadActivationPanel::RpcReset()", 58845995, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void PlayBeep(AudioClip clip)
	{
		AudioSourcePoolManager.PlayOnTransform(clip, base.transform);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcGranted()
	{
		this.PlayBeep(this._grantedClip);
		this._nfcScanner.SetGranted();
		this._permsIndicator.PlayAccepted(null);
	}

	protected static void InvokeUserCode_RpcGranted(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcGranted called on server.");
		}
		else
		{
			((AlphaWarheadActivationPanel)obj).UserCode_RpcGranted();
		}
	}

	protected void UserCode_RpcDenied__DoorPermissionFlags(DoorPermissionFlags flags)
	{
		this.PlayBeep(this._deniedClip);
		this._nfcScanner.SetTemporaryDenied(this._deniedCooldownDuration);
		this._permsIndicator.PlayDenied(flags, this._deniedCooldownDuration);
	}

	protected static void InvokeUserCode_RpcDenied__DoorPermissionFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcDenied called on server.");
		}
		else
		{
			((AlphaWarheadActivationPanel)obj).UserCode_RpcDenied__DoorPermissionFlags(GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(reader));
		}
	}

	protected void UserCode_RpcReset()
	{
		this._nfcScanner.SetRegular();
		this._permsIndicator.ShowIdle();
	}

	protected static void InvokeUserCode_RpcReset(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcReset called on server.");
		}
		else
		{
			((AlphaWarheadActivationPanel)obj).UserCode_RpcReset();
		}
	}

	static AlphaWarheadActivationPanel()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadActivationPanel), "System.Void AlphaWarheadActivationPanel::RpcGranted()", InvokeUserCode_RpcGranted);
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadActivationPanel), "System.Void AlphaWarheadActivationPanel::RpcDenied(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", InvokeUserCode_RpcDenied__DoorPermissionFlags);
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadActivationPanel), "System.Void AlphaWarheadActivationPanel::RpcReset()", InvokeUserCode_RpcReset);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this._unlocked);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this._unlocked);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._unlocked, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._unlocked, null, reader.ReadBool());
		}
	}
}
