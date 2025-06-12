using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl;

public class FpcScaleController
{
	private Vector3 _scale;

	public Vector3 Scale
	{
		get
		{
			return this._scale;
		}
		set
		{
			if (!(value == this._scale))
			{
				this._scale = value;
				this.Hub.transform.localScale = new Vector3(this._scale.x, this._scale.y, this._scale.z);
				if (NetworkServer.active && !(this.Motor.Hub == null))
				{
					new SyncedScaleMessages.ScaleMessage(this._scale, this.Hub).SendToAuthenticated();
				}
			}
		}
	}

	public FpcMotor Motor { get; set; }

	public ReferenceHub Hub => this.Motor.Hub;

	public FpcScaleController(FpcMotor fpcMotor)
	{
		this.Motor = fpcMotor;
		this.Scale = Vector3.one;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += RegisterHandler;
		ReferenceHub.OnPlayerAdded += OnPlayerAdded;
	}

	private static void RegisterHandler()
	{
		NetworkClient.ReplaceHandler(delegate(SyncedScaleMessages.ScaleMessage msg)
		{
			if (!(msg.TargetHub == null) && msg.TargetHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				fpcRole.FpcModule.Motor.ScaleController.Scale = msg.Scale;
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
			if (allHub.roleManager.CurrentRole is IFpcRole fpcRole && !(fpcRole.FpcModule.Motor.ScaleController.Scale == Vector3.one))
			{
				hub.connectionToClient.Send(new SyncedScaleMessages.ScaleMessage(fpcRole.FpcModule.Motor.ScaleController.Scale, allHub));
			}
		}
	}
}
