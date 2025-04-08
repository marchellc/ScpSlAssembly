using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public static class SubcontrollerRpcHandler
	{
		public static void ServerSendRpc(SubcontrollerBehaviour behaviour, Action<NetworkWriter> rpcWriter)
		{
			SubcontrollerRpcHandler.ServerSendRpc(behaviour.Model, behaviour.SubcontrollerIndex, rpcWriter);
		}

		public static void ServerSendRpc(AnimatedCharacterModel model, int syncIndex, Action<NetworkWriter> rpcWriter)
		{
			if (model.OwnerHub == null)
			{
				Debug.LogError("Attempting to send an RPC on an owner-less model.");
				return;
			}
			using (NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get())
			{
				if (rpcWriter != null)
				{
					rpcWriter(networkWriterPooled);
				}
				NetworkServer.SendToReady<SubcontrollerRpcHandler.SubcontrollerRpcMessage>(new SubcontrollerRpcHandler.SubcontrollerRpcMessage(networkWriterPooled, model, syncIndex), 0);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<SubcontrollerRpcHandler.SubcontrollerRpcMessage>(new Action<SubcontrollerRpcHandler.SubcontrollerRpcMessage>(SubcontrollerRpcHandler.ClientProcessMessage), true);
			};
		}

		private static void ClientProcessMessage(SubcontrollerRpcHandler.SubcontrollerRpcMessage msg)
		{
			try
			{
				msg.ProcessRpc();
			}
			catch (Exception ex)
			{
				Debug.Log("Exception in Subcontroller RPC handler for " + msg.ToString());
				Debug.LogException(ex);
			}
		}

		public static SubcontrollerRpcHandler.SubcontrollerRpcMessage ReadSubcontrollerRpcMessage(this NetworkReader reader)
		{
			return new SubcontrollerRpcHandler.SubcontrollerRpcMessage(reader);
		}

		public static void WriteSubcontrollerRpcMessage(this NetworkWriter writer, SubcontrollerRpcHandler.SubcontrollerRpcMessage msg)
		{
			msg.Serialize(writer);
		}

		public readonly struct SubcontrollerRpcMessage : NetworkMessage
		{
			public SubcontrollerRpcMessage(NetworkWriter writer, AnimatedCharacterModel model, int syncIndex)
			{
				this._player = new RecyclablePlayerId(model.OwnerHub.PlayerId);
				this._roleFailsafe = model.OwnerHub.GetRoleId();
				this._syncIndex = syncIndex;
				int position = writer.Position;
				writer.WriteByte((byte)Math.Min(position, 255));
				if (position >= 255)
				{
					writer.WriteUShort((ushort)(position - 255));
				}
				this._bytesWritten = position;
				Array.Copy(writer.buffer, SubcontrollerRpcHandler.SubcontrollerRpcMessage.Buffer, this._bytesWritten);
			}

			public override string ToString()
			{
				return string.Format("{0} (PlayerID={1} Role={2} Length={3} Payload={4})", new object[]
				{
					"SubcontrollerRpcMessage",
					this._player.Value,
					this._roleFailsafe,
					this._bytesWritten,
					SubcontrollerRpcHandler.SubcontrollerRpcMessage.Reader
				});
			}

			internal SubcontrollerRpcMessage(NetworkReader reader)
			{
				this._player = reader.ReadRecyclablePlayerId();
				this._roleFailsafe = reader.ReadRoleType();
				this._syncIndex = (int)reader.ReadByte();
				this._bytesWritten = (int)reader.ReadByte();
				reader.ReadBytes(SubcontrollerRpcHandler.SubcontrollerRpcMessage.Buffer, this._bytesWritten);
			}

			internal void Serialize(NetworkWriter writer)
			{
				writer.WriteRecyclablePlayerId(this._player);
				writer.WriteRoleType(this._roleFailsafe);
				writer.WriteByte((byte)this._syncIndex);
				if (this._bytesWritten <= 255)
				{
					writer.WriteByte((byte)this._bytesWritten);
				}
				else
				{
					writer.WriteByte(byte.MaxValue);
					Debug.LogError("Attempting to send a payload bigger than 255 bytes. Clients will not receive the full message.");
				}
				writer.WriteBytes(SubcontrollerRpcHandler.SubcontrollerRpcMessage.Buffer, 0, Math.Min(255, this._bytesWritten));
			}

			internal unsafe void ProcessRpc()
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHub(this._player.Value, out referenceHub))
				{
					return;
				}
				PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
				IFpcRole fpcRole = currentRole as IFpcRole;
				if (fpcRole == null || currentRole.RoleTypeId != this._roleFailsafe)
				{
					return;
				}
				AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
				if (animatedCharacterModel == null)
				{
					return;
				}
				INetworkedAnimatedModelSubcontroller networkedAnimatedModelSubcontroller = (*animatedCharacterModel.AllSubcontrollers[this._syncIndex]) as INetworkedAnimatedModelSubcontroller;
				if (networkedAnimatedModelSubcontroller == null)
				{
					return;
				}
				SubcontrollerRpcHandler.SubcontrollerRpcMessage.Reader.buffer = new ArraySegment<byte>(SubcontrollerRpcHandler.SubcontrollerRpcMessage.Buffer, 0, this._bytesWritten);
				SubcontrollerRpcHandler.SubcontrollerRpcMessage.Reader.Position = 0;
				networkedAnimatedModelSubcontroller.ProcessRpc(SubcontrollerRpcHandler.SubcontrollerRpcMessage.Reader);
			}

			private static readonly byte[] Buffer = new byte[65535];

			private static readonly NetworkReader Reader = new NetworkReader(SubcontrollerRpcHandler.SubcontrollerRpcMessage.Buffer);

			private readonly int _bytesWritten;

			private readonly RecyclablePlayerId _player;

			private readonly RoleTypeId _roleFailsafe;

			private readonly int _syncIndex;
		}
	}
}
