using System.Collections.Generic;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Respawning;

public class RespawnEffectsController : NetworkBehaviour
{
	private static readonly List<RespawnEffectsController> AllControllers;

	private void Awake()
	{
		while (AllControllers.Contains(null))
		{
			AllControllers.Remove(null);
		}
		AllControllers.Add(this);
	}

	public static void PlayCassieAnnouncement(string words, bool makeHold, bool makeNoise, bool customAnnouncement = false, string customSubtitles = "")
	{
		foreach (RespawnEffectsController allController in AllControllers)
		{
			if (allController != null)
			{
				allController.ServerPassCassie(words, makeHold, makeNoise, customAnnouncement, customSubtitles);
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
		if (cassieAnnouncingEventArgs.IsAllowed)
		{
			words = cassieAnnouncingEventArgs.Words;
			makeHold = cassieAnnouncingEventArgs.MakeHold;
			makeNoise = cassieAnnouncingEventArgs.MakeNoise;
			customAnnouncement = cassieAnnouncingEventArgs.CustomAnnouncement;
			customSubtitles = cassieAnnouncingEventArgs.CustomSubtitles;
			RpcCassieAnnouncement(words, makeHold, makeNoise, customAnnouncement, customSubtitles);
			ServerEvents.OnCassieAnnounced(new CassieAnnouncedEventArgs(words, makeHold, makeNoise, customAnnouncement, customSubtitles));
		}
	}

	[ClientRpc]
	private void RpcCassieAnnouncement(string words, bool makeHold, bool makeNoise, bool customAnnouncement, string customSubtitles)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(words);
		writer.WriteBool(makeHold);
		writer.WriteBool(makeNoise);
		writer.WriteBool(customAnnouncement);
		writer.WriteString(customSubtitles);
		SendRPCInternal("System.Void Respawning.RespawnEffectsController::RpcCassieAnnouncement(System.String,System.Boolean,System.Boolean,System.Boolean,System.String)", -31296712, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public static void ClearQueue()
	{
		foreach (RespawnEffectsController allController in AllControllers)
		{
			if (allController != null)
			{
				allController.ServerPassClearQueue();
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
		}
		else
		{
			RpcClearQueue();
		}
	}

	[ClientRpc]
	public void RpcClearQueue()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		SendRPCInternal("System.Void Respawning.RespawnEffectsController::RpcClearQueue()", 370903972, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	static RespawnEffectsController()
	{
		AllControllers = new List<RespawnEffectsController>();
		RemoteProcedureCalls.RegisterRpc(typeof(RespawnEffectsController), "System.Void Respawning.RespawnEffectsController::RpcCassieAnnouncement(System.String,System.Boolean,System.Boolean,System.Boolean,System.String)", InvokeUserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String);
		RemoteProcedureCalls.RegisterRpc(typeof(RespawnEffectsController), "System.Void Respawning.RespawnEffectsController::RpcClearQueue()", InvokeUserCode_RpcClearQueue);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(string words, bool makeHold, bool makeNoise, bool customAnnouncement, string customSubtitles)
	{
		if (!string.IsNullOrEmpty(words))
		{
			NineTailedFoxAnnouncer.singleton.AddPhraseToQueue(words, makeNoise, rawNumber: false, makeHold, customAnnouncement, customSubtitles);
		}
	}

	protected static void InvokeUserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCassieAnnouncement called on server.");
		}
		else
		{
			((RespawnEffectsController)obj).UserCode_RpcCassieAnnouncement__String__Boolean__Boolean__Boolean__String(reader.ReadString(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadString());
		}
	}

	protected void UserCode_RpcClearQueue()
	{
	}

	protected static void InvokeUserCode_RpcClearQueue(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClearQueue called on server.");
		}
		else
		{
			((RespawnEffectsController)obj).UserCode_RpcClearQueue();
		}
	}
}
