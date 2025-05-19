using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl;

public class FpcGravityController
{
	public static Dictionary<ReferenceHub, Vector3> AllSyncedGravities = new Dictionary<ReferenceHub, Vector3>();

	private Vector3 _gravity = DefaultGravity;

	public static Vector3 DefaultGravity => new Vector3(0f, -19.6f, 0f);

	public Vector3 Gravity
	{
		get
		{
			return _gravity;
		}
		set
		{
			if (value == _gravity)
			{
				return;
			}
			_gravity = value;
			if (NetworkServer.active && !(Motor.Hub == null))
			{
				if (_gravity == DefaultGravity)
				{
					AllSyncedGravities.Remove(Hub);
				}
				else
				{
					AllSyncedGravities[Hub] = _gravity;
				}
				new SyncedGravityMessages.GravityMessage(_gravity, Hub).SendToAuthenticated();
			}
		}
	}

	public FpcMotor Motor { get; set; }

	public ReferenceHub Hub => Motor.Hub;

	public FpcGravityController(FpcMotor fpcMotor)
	{
		Motor = fpcMotor;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleManager.OnRoleChanged += PlayerRoleChanged;
		CustomNetworkManager.OnClientReady += RegisterHandler;
		ReferenceHub.OnPlayerAdded += OnPlayerAdded;
	}

	private static void PlayerRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (NetworkServer.active)
		{
			AllSyncedGravities.Remove(userHub);
		}
	}

	private static void RegisterHandler()
	{
		NetworkClient.ReplaceHandler(delegate(SyncedGravityMessages.GravityMessage msg)
		{
			if (!(msg.TargetHub == null) && msg.TargetHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				fpcRole.FpcModule.Motor.GravityController.Gravity = msg.Gravity;
			}
		});
	}

	private static void OnPlayerAdded(ReferenceHub hub)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (KeyValuePair<ReferenceHub, Vector3> allSyncedGravity in AllSyncedGravities)
		{
			hub.connectionToClient.Send(new SyncedGravityMessages.GravityMessage(allSyncedGravity.Value, allSyncedGravity.Key));
		}
	}
}
