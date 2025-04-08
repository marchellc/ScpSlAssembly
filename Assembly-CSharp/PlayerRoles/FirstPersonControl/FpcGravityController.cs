using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcGravityController
	{
		public static Vector3 DefaultGravity
		{
			get
			{
				return new Vector3(0f, -19.6f, 0f);
			}
		}

		public Vector3 Gravity
		{
			get
			{
				return this._gravity;
			}
			set
			{
				if (value == this._gravity)
				{
					return;
				}
				this._gravity = value;
				if (!NetworkServer.active || this.Motor.Hub == null)
				{
					return;
				}
				if (this._gravity == FpcGravityController.DefaultGravity)
				{
					FpcGravityController.AllSyncedGravities.Remove(this.Hub);
				}
				else
				{
					FpcGravityController.AllSyncedGravities[this.Hub] = this._gravity;
				}
				new SyncedGravityMessages.GravityMessage(this._gravity, this.Hub).SendToAuthenticated(0);
			}
		}

		public FpcMotor Motor { get; set; }

		public ReferenceHub Hub
		{
			get
			{
				return this.Motor.Hub;
			}
		}

		public FpcGravityController(FpcMotor fpcMotor)
		{
			this.Motor = fpcMotor;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnRoleChanged += FpcGravityController.PlayerRoleChanged;
			CustomNetworkManager.OnClientReady += FpcGravityController.RegisterHandler;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(FpcGravityController.OnPlayerAdded));
		}

		private static void PlayerRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			FpcGravityController.AllSyncedGravities.Remove(userHub);
		}

		private static void RegisterHandler()
		{
			NetworkClient.ReplaceHandler<SyncedGravityMessages.GravityMessage>(delegate(SyncedGravityMessages.GravityMessage msg)
			{
				if (!(msg.TargetHub == null))
				{
					IFpcRole fpcRole = msg.TargetHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						fpcRole.FpcModule.Motor.GravityController.Gravity = msg.Gravity;
						return;
					}
				}
			}, true);
		}

		private static void OnPlayerAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (KeyValuePair<ReferenceHub, Vector3> keyValuePair in FpcGravityController.AllSyncedGravities)
			{
				hub.connectionToClient.Send<SyncedGravityMessages.GravityMessage>(new SyncedGravityMessages.GravityMessage(keyValuePair.Value, keyValuePair.Key), 0);
			}
		}

		public static Dictionary<ReferenceHub, Vector3> AllSyncedGravities = new Dictionary<ReferenceHub, Vector3>();

		private Vector3 _gravity = FpcGravityController.DefaultGravity;
	}
}
