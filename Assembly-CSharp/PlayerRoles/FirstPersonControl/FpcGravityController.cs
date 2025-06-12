using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl;

public class FpcGravityController
{
	private Vector3 _gravity = FpcGravityController.DefaultGravity;

	public static Vector3 DefaultGravity => new Vector3(0f, -19.6f, 0f);

	public Vector3 Gravity
	{
		get
		{
			return this._gravity;
		}
		set
		{
			if (!(value == this._gravity))
			{
				this._gravity = value;
				if (NetworkServer.active && !(this.Motor.Hub == null))
				{
					new SyncedGravityMessages.GravityMessage(this._gravity, this.Hub).SendToAuthenticated();
				}
			}
		}
	}

	public FpcMotor Motor { get; set; }

	public ReferenceHub Hub => this.Motor.Hub;

	public FpcGravityController(FpcMotor fpcMotor)
	{
		this.Motor = fpcMotor;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += RegisterHandler;
		ReferenceHub.OnPlayerAdded += OnPlayerAdded;
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
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is IFpcRole fpcRole && !(fpcRole.FpcModule.Motor.GravityController.Gravity == FpcGravityController.DefaultGravity))
			{
				hub.connectionToClient.Send(new SyncedGravityMessages.GravityMessage(fpcRole.FpcModule.Motor.GravityController.Gravity, allHub));
			}
		}
	}
}
