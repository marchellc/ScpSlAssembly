using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Respawning
{
	public class RespawnEffectsController : NetworkBehaviour
	{
		private void Awake()
		{
			while (RespawnEffectsController.AllControllers.Contains(null))
			{
				RespawnEffectsController.AllControllers.Remove(null);
			}
			RespawnEffectsController.AllControllers.Add(this);
		}

		public static void PlayCassieAnnouncement(string words, bool makeHold, bool makeNoise, bool customAnnouncement = false, string customSubtitles = "")
		{
			foreach (RespawnEffectsController respawnEffectsController in RespawnEffectsController.AllControllers)
			{
				if (respawnEffectsController != null)
				{
					respawnEffectsController.ServerPassCassie(words, makeHold, makeNoise, customAnnouncement, customSubtitles);
					break;
				}
			}
		}

		[Server]
		private void ServerPassCassie(string words, bool makeHold, bool makeNoise, bool customAnnouncement, string customSubtitles = "")
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Respawning.RespawnEffectsController::ServerPassCassie(System.String,System.Boolean,System.Boolean,System.Boolean,System.String)' called when server was not active");
				return;
			}
			CassieAnnouncingEventArgs cassieAnnouncingEventArgs = new CassieAnnouncingEventArgs(words, makeHold, makeNoise, customAnnouncement, customSubtitles);
			ServerEvents.OnCassieAnnouncing(cassieAnnouncingEventArgs);
			if (!cassieAnnouncingEventArgs.IsAllowed)
			{
				return;
			}
			words = cassieAnnouncingEventArgs.Words;
			makeHold = cassieAnnouncingEventArgs.MakeHold;
			makeNoise = cassieAnnouncingEventArgs.MakeNoise;
			customAnnouncement = cassieAnnouncingEventArgs.CustomAnnouncement;
			customSubtitles = cassieAnnouncingEventArgs.CustomSubtitles;
			this.RpcCassieAnnouncement(words, makeHold, makeNoise, customAnnouncement, customSubtitles);
			ServerEvents.OnCassieAnnounced(new CassieAnnouncedEventArgs(words, makeHold, makeNoise, customAnnouncement, customSubtitles));
		}

		[ClientRpc]
		private void RpcCassieAnnouncement(string words, bool makeHold, bool makeNoise, bool customAnnouncement, string customSubtitles)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(words);
			networkWriterPooled.WriteBool(makeHold);
			networkWriterPooled.WriteBool(makeNoise);
			networkWriterPooled.WriteBool(customAnnouncement);
			networkWriterPooled.WriteString(customSubtitles);
			this.SendRPCInternal("System.Void Respawning.RespawnEffectsController::RpcCassieAnnouncement(System.String,System.Boolean,System.Boolean,System.Boolean,System.String)", -31296712, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public static void ClearQueue()
		{
			foreach (RespawnEffectsController respawnEffectsController in RespawnEffectsController.AllControllers)
			{
				if (respawnEffectsController != null)
				{
					respawnEffectsController.ServerPassClearQueue();
					break;
				}
			}
		}

		[Server]
		private void ServerPassClearQueue()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Respawning.RespawnEffectsController::ServerPassClearQueue()' called when server was not active");
				return;
			}
			this.RpcClearQueue();
		}

		[ClientRpc]
		public void RpcClearQueue()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Respawning.RespawnEffectsController::RpcClearQueue()", 370903972, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		static RespawnEffectsController()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(RespawnEffectsController), "System.Void Respawning.RespawnEffectsController::RpcCassieAnnouncement(System.String,System.Boolean,System.Boolean,System.Boolean,System.String)", new RemoteCallDelegate(RespawnEffectsController.InvokeUserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String));
			RemoteProcedureCalls.RegisterRpc(typeof(RespawnEffectsController), "System.Void Respawning.RespawnEffectsController::RpcClearQueue()", new RemoteCallDelegate(RespawnEffectsController.InvokeUserCode_RpcClearQueue));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(string words, bool makeHold, bool makeNoise, bool customAnnouncement, string customSubtitles)
		{
			if (string.IsNullOrEmpty(words))
			{
				return;
			}
			NineTailedFoxAnnouncer.singleton.AddPhraseToQueue(words, makeNoise, false, makeHold, customAnnouncement, customSubtitles);
		}

		protected static void InvokeUserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcCassieAnnouncement called on server.");
				return;
			}
			((RespawnEffectsController)obj).UserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(reader.ReadString(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadString());
		}

		protected void UserCode_RpcClearQueue()
		{
		}

		protected static void InvokeUserCode_RpcClearQueue(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcClearQueue called on server.");
				return;
			}
			((RespawnEffectsController)obj).UserCode_RpcClearQueue();
		}

		private static readonly List<RespawnEffectsController> AllControllers = new List<RespawnEffectsController>();
	}
}
