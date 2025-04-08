using System;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using TMPro;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class TestInterobject : NetworkBehaviour, IClientInteractable, IInteractable, IServerInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		[Client]
		public void ClientInteract(InteractableCollider collider)
		{
			if (!NetworkClient.active)
			{
				Debug.LogWarning("[Client] function 'System.Void Interactables.Interobjects.TestInterobject::ClientInteract(Interactables.InteractableCollider)' called when client was not active");
				return;
			}
			this.ClientText.text = string.Concat(new string[]
			{
				"Local player collider ID: ",
				collider.ColliderId.ToString(),
				" (rand ",
				global::UnityEngine.Random.value.ToString(),
				")"
			});
		}

		[Server]
		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Interactables.Interobjects.TestInterobject::ServerInteract(ReferenceHub,System.Byte)' called when server was not active");
				return;
			}
			this.RpcSendText("Player " + ply.LoggedNameFromRefHub() + " interacted using collider " + colliderId.ToString());
		}

		[ClientRpc]
		private void RpcSendText(string s)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(s);
			this.SendRPCInternal("System.Void Interactables.Interobjects.TestInterobject::RpcSendText(System.String)", 314358345, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcSendText__String(string s)
		{
			this.GlobalText.text = s;
		}

		protected static void InvokeUserCode_RpcSendText__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcSendText called on server.");
				return;
			}
			((TestInterobject)obj).UserCode_RpcSendText__String(reader.ReadString());
		}

		static TestInterobject()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(TestInterobject), "System.Void Interactables.Interobjects.TestInterobject::RpcSendText(System.String)", new RemoteCallDelegate(TestInterobject.InvokeUserCode_RpcSendText__String));
		}

		public TextMeshProUGUI ClientText;

		public TextMeshProUGUI GlobalText;
	}
}
