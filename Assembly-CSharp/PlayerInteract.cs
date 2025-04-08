using System;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using Security;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{
	private bool CanInteract
	{
		get
		{
			return this._playerInteractRateLimit.CanExecute(true) && (!this._hub.inventory.IsDisarmed() || PlayerInteract.CanDisarmedInteract) && !this._hub.interCoordinator.AnyBlocker(BlockedInteraction.GeneralInteractions);
		}
	}

	private void Start()
	{
		this._hub = base.GetComponent<ReferenceHub>();
		this._playerInteractRateLimit = this._hub.playerRateLimitHandler.RateLimits[0];
		this._sr = this._hub.serverRoles;
		this._inv = this._hub.inventory;
		this._invisible = this._hub.playerEffectsController.GetEffect<Invisible>();
	}

	private void Update()
	{
	}

	[Command(channel = 4)]
	private void CmdUsePanel(PlayerInteract.AlphaPanelOperations n)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		global::Mirror.GeneratedNetworkCode._Write_PlayerInteract/AlphaPanelOperations(networkWriterPooled, n);
		base.SendCommandInternal("System.Void PlayerInteract::CmdUsePanel(PlayerInteract/AlphaPanelOperations)", 338550603, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	private void RpcLeverSound()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void PlayerInteract::RpcLeverSound()", 233325680, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[Command(channel = 4)]
	private void CmdSwitchAWButton()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		base.SendCommandInternal("System.Void PlayerInteract::CmdSwitchAWButton()", 1054596508, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[Command(channel = 4)]
	private void CmdDetonateWarhead()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		base.SendCommandInternal("System.Void PlayerInteract::CmdDetonateWarhead()", 369717770, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private bool ChckDis(Vector3 pos)
	{
		return Vector3.Distance(base.transform.position, pos) < 3.63f;
	}

	private void OnInteract()
	{
		this._invisible.ServerDisable();
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdUsePanel__AlphaPanelOperations(PlayerInteract.AlphaPanelOperations n)
	{
		if (!this.CanInteract)
		{
			return;
		}
		ReferenceHub component = base.GetComponent<ReferenceHub>();
		AlphaWarheadNukesitePanel nukeside = AlphaWarheadOutsitePanel.nukeside;
		if (!this.ChckDis(nukeside.transform.position))
		{
			return;
		}
		if (n == PlayerInteract.AlphaPanelOperations.Cancel)
		{
			this.OnInteract();
			AlphaWarheadController.Singleton.CancelDetonation(this._hub);
			ServerLogs.AddLog(ServerLogs.Modules.Warhead, component.LoggedNameFromRefHub() + " cancelled the Alpha Warhead detonation.", ServerLogs.ServerLogType.GameEvent, false);
			return;
		}
		if (n != PlayerInteract.AlphaPanelOperations.Lever)
		{
			return;
		}
		this.OnInteract();
		if (!nukeside.AllowChangeLevelState())
		{
			return;
		}
		nukeside.Networkenabled = !nukeside.enabled;
		this.RpcLeverSound();
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, component.LoggedNameFromRefHub() + " set the Alpha Warhead status to " + nukeside.enabled.ToString() + ".", ServerLogs.ServerLogType.GameEvent, false);
	}

	protected static void InvokeUserCode_CmdUsePanel__AlphaPanelOperations(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUsePanel called on client.");
			return;
		}
		((PlayerInteract)obj).UserCode_CmdUsePanel__AlphaPanelOperations(global::Mirror.GeneratedNetworkCode._Read_PlayerInteract/AlphaPanelOperations(reader));
	}

	protected void UserCode_RpcLeverSound()
	{
	}

	protected static void InvokeUserCode_RpcLeverSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcLeverSound called on server.");
			return;
		}
		((PlayerInteract)obj).UserCode_RpcLeverSound();
	}

	protected void UserCode_CmdSwitchAWButton()
	{
		if (!this.CanInteract)
		{
			return;
		}
		GameObject gameObject = GameObject.Find("OutsitePanelScript");
		if (!this.ChckDis(gameObject.transform.position))
		{
			return;
		}
		bool flag = this._sr.BypassMode;
		KeycardItem keycardItem = this._inv.CurInstance as KeycardItem;
		if (keycardItem != null)
		{
			flag = keycardItem.Permissions.HasFlag(KeycardPermissions.AlphaWarhead);
		}
		PlayerUnlockingWarheadButtonEventArgs playerUnlockingWarheadButtonEventArgs = new PlayerUnlockingWarheadButtonEventArgs(this._hub);
		playerUnlockingWarheadButtonEventArgs.IsAllowed = flag;
		PlayerEvents.OnUnlockingWarheadButton(playerUnlockingWarheadButtonEventArgs);
		flag = playerUnlockingWarheadButtonEventArgs.IsAllowed;
		if (flag)
		{
			AlphaWarheadOutsitePanel componentInParent = gameObject.GetComponentInParent<AlphaWarheadOutsitePanel>();
			if (componentInParent == null || componentInParent.keycardEntered)
			{
				return;
			}
			this.OnInteract();
			componentInParent.NetworkkeycardEntered = true;
			PlayerEvents.OnUnlockedWarheadButton(new PlayerUnlockedWarheadButtonEventArgs(this._hub));
		}
	}

	protected static void InvokeUserCode_CmdSwitchAWButton(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSwitchAWButton called on client.");
			return;
		}
		((PlayerInteract)obj).UserCode_CmdSwitchAWButton();
	}

	protected void UserCode_CmdDetonateWarhead()
	{
		if (!this.CanInteract)
		{
			return;
		}
		if (!this._playerInteractRateLimit.CanExecute(true) && AlphaWarheadController.Singleton.IsLocked)
		{
			return;
		}
		GameObject gameObject = GameObject.Find("OutsitePanelScript");
		if (!this.ChckDis(gameObject.transform.position) || !AlphaWarheadOutsitePanel.nukeside.enabled || !gameObject.GetComponent<AlphaWarheadOutsitePanel>().keycardEntered)
		{
			return;
		}
		ReferenceHub component = base.GetComponent<ReferenceHub>();
		AlphaWarheadController.Singleton.StartDetonation(false, false, component);
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, component.LoggedNameFromRefHub() + " started the Alpha Warhead detonation.", ServerLogs.ServerLogType.GameEvent, false);
		this.OnInteract();
	}

	protected static void InvokeUserCode_CmdDetonateWarhead(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDetonateWarhead called on client.");
			return;
		}
		((PlayerInteract)obj).UserCode_CmdDetonateWarhead();
	}

	static PlayerInteract()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(PlayerInteract), "System.Void PlayerInteract::CmdUsePanel(PlayerInteract/AlphaPanelOperations)", new RemoteCallDelegate(PlayerInteract.InvokeUserCode_CmdUsePanel__AlphaPanelOperations), true);
		RemoteProcedureCalls.RegisterCommand(typeof(PlayerInteract), "System.Void PlayerInteract::CmdSwitchAWButton()", new RemoteCallDelegate(PlayerInteract.InvokeUserCode_CmdSwitchAWButton), true);
		RemoteProcedureCalls.RegisterCommand(typeof(PlayerInteract), "System.Void PlayerInteract::CmdDetonateWarhead()", new RemoteCallDelegate(PlayerInteract.InvokeUserCode_CmdDetonateWarhead), true);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerInteract), "System.Void PlayerInteract::RpcLeverSound()", new RemoteCallDelegate(PlayerInteract.InvokeUserCode_RpcLeverSound));
	}

	internal static bool Scp096DestroyLockedDoors;

	internal static bool CanDisarmedInteract;

	private const float ActivationTokenReward = 1f;

	public LayerMask mask;

	private ServerRoles _sr;

	private Inventory _inv;

	private string _uiToggleKey = "numlock";

	private bool _enableUiToggle;

	private Invisible _invisible;

	private RateLimit _playerInteractRateLimit;

	private ReferenceHub _hub;

	private KeyCode _interactKey;

	private enum AlphaPanelOperations : byte
	{
		Cancel,
		Lever
	}

	internal enum Generator079Operations : byte
	{
		Door,
		Tablet,
		Cancel
	}
}
