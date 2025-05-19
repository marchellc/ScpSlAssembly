using CustomPlayerEffects;
using InventorySystem;
using InventorySystem.Disarming;
using InventorySystem.Items;
using Mirror;
using Mirror.RemoteCalls;
using Security;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{
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

	internal static bool Scp096DestroyLockedDoors;

	internal static bool CanDisarmedInteract;

	public LayerMask mask;

	private ServerRoles _sr;

	private Inventory _inv;

	private string _uiToggleKey = "numlock";

	private bool _enableUiToggle;

	private Invisible _invisible;

	private RateLimit _playerInteractRateLimit;

	private ReferenceHub _hub;

	private KeyCode _interactKey;

	private bool CanInteract
	{
		get
		{
			if (_playerInteractRateLimit.CanExecute() && (!_hub.inventory.IsDisarmed() || CanDisarmedInteract))
			{
				return !_hub.interCoordinator.AnyBlocker(BlockedInteraction.GeneralInteractions);
			}
			return false;
		}
	}

	private void Start()
	{
		_hub = GetComponent<ReferenceHub>();
		_playerInteractRateLimit = _hub.playerRateLimitHandler.RateLimits[0];
		_sr = _hub.serverRoles;
		_inv = _hub.inventory;
		_invisible = _hub.playerEffectsController.GetEffect<Invisible>();
	}

	private void Update()
	{
	}

	[Command(channel = 4)]
	private void CmdUsePanel(AlphaPanelOperations n)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_PlayerInteract_002FAlphaPanelOperations(writer, n);
		SendCommandInternal("System.Void PlayerInteract::CmdUsePanel(PlayerInteract/AlphaPanelOperations)", 338550603, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcLeverSound()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		SendRPCInternal("System.Void PlayerInteract::RpcLeverSound()", 233325680, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private bool ChckDis(Vector3 pos)
	{
		return Vector3.Distance(base.transform.position, pos) < 3.63f;
	}

	private void OnInteract()
	{
		_invisible.ServerDisable();
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdUsePanel__AlphaPanelOperations(AlphaPanelOperations n)
	{
		if (!CanInteract)
		{
			return;
		}
		ReferenceHub component = GetComponent<ReferenceHub>();
		AlphaWarheadNukesitePanel nukeside = AlphaWarheadOutsitePanel.nukeside;
		if (!ChckDis(nukeside.transform.position))
		{
			return;
		}
		switch (n)
		{
		case AlphaPanelOperations.Cancel:
			OnInteract();
			AlphaWarheadController.Singleton.CancelDetonation(_hub);
			ServerLogs.AddLog(ServerLogs.Modules.Warhead, component.LoggedNameFromRefHub() + " cancelled the Alpha Warhead detonation.", ServerLogs.ServerLogType.GameEvent);
			break;
		case AlphaPanelOperations.Lever:
			OnInteract();
			if (nukeside.AllowChangeLevelState())
			{
				nukeside.Networkenabled = !nukeside.enabled;
				RpcLeverSound();
				ServerLogs.AddLog(ServerLogs.Modules.Warhead, component.LoggedNameFromRefHub() + " set the Alpha Warhead status to " + nukeside.enabled + ".", ServerLogs.ServerLogType.GameEvent);
			}
			break;
		}
	}

	protected static void InvokeUserCode_CmdUsePanel__AlphaPanelOperations(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUsePanel called on client.");
		}
		else
		{
			((PlayerInteract)obj).UserCode_CmdUsePanel__AlphaPanelOperations(GeneratedNetworkCode._Read_PlayerInteract_002FAlphaPanelOperations(reader));
		}
	}

	protected void UserCode_RpcLeverSound()
	{
	}

	protected static void InvokeUserCode_RpcLeverSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcLeverSound called on server.");
		}
		else
		{
			((PlayerInteract)obj).UserCode_RpcLeverSound();
		}
	}

	static PlayerInteract()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(PlayerInteract), "System.Void PlayerInteract::CmdUsePanel(PlayerInteract/AlphaPanelOperations)", InvokeUserCode_CmdUsePanel__AlphaPanelOperations, requiresAuthority: true);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerInteract), "System.Void PlayerInteract::RpcLeverSound()", InvokeUserCode_RpcLeverSound);
	}
}
