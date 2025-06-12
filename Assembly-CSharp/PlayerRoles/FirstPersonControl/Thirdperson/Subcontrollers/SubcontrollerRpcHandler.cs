using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public static class SubcontrollerRpcHandler
{
	public readonly struct SubcontrollerRpcMessage : NetworkMessage
	{
		private static readonly byte[] Buffer = new byte[65535];

		private static readonly NetworkReader Reader = new NetworkReader(SubcontrollerRpcMessage.Buffer);

		private readonly int _bytesWritten;

		private readonly RecyclablePlayerId _player;

		private readonly RoleTypeId _roleFailsafe;

		private readonly int _syncIndex;

		public SubcontrollerRpcMessage(NetworkWriter writer, AnimatedCharacterModel model, int syncIndex)
		{
			this._player = new RecyclablePlayerId(model.OwnerHub);
			this._roleFailsafe = model.OwnerHub.GetRoleId();
			this._syncIndex = syncIndex;
			int position = writer.Position;
			writer.WriteByte((byte)Math.Min(position, 255));
			if (position >= 255)
			{
				writer.WriteUShort((ushort)(position - 255));
			}
			this._bytesWritten = position;
			Array.Copy(writer.buffer, SubcontrollerRpcMessage.Buffer, this._bytesWritten);
		}

		public override string ToString()
		{
			return string.Format("{0} (PlayerID={1} Role={2} Length={3} Payload={4})", "SubcontrollerRpcMessage", this._player.Value, this._roleFailsafe, this._bytesWritten, SubcontrollerRpcMessage.Reader);
		}

		internal SubcontrollerRpcMessage(NetworkReader reader)
		{
			this._player = reader.ReadRecyclablePlayerId();
			this._roleFailsafe = reader.ReadRoleType();
			this._syncIndex = reader.ReadByte();
			this._bytesWritten = reader.ReadByte();
			reader.ReadBytes(SubcontrollerRpcMessage.Buffer, this._bytesWritten);
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
			writer.WriteBytes(SubcontrollerRpcMessage.Buffer, 0, Math.Min(255, this._bytesWritten));
		}

		internal void ProcessRpc()
		{
			if (ReferenceHub.TryGetHub(this._player.Value, out var hub))
			{
				PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
				if (currentRole is IFpcRole role && currentRole.RoleTypeId == this._roleFailsafe && role.TryGetRpcTarget(out var target) && target.AllSubcontrollers[this._syncIndex] is INetworkedAnimatedModelSubcontroller networkedAnimatedModelSubcontroller)
				{
					SubcontrollerRpcMessage.Reader.buffer = new ArraySegment<byte>(SubcontrollerRpcMessage.Buffer, 0, this._bytesWritten);
					SubcontrollerRpcMessage.Reader.Position = 0;
					networkedAnimatedModelSubcontroller.ProcessRpc(SubcontrollerRpcMessage.Reader);
				}
			}
		}
	}

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
		using NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		rpcWriter?.Invoke(networkWriterPooled);
		NetworkServer.SendToReady(new SubcontrollerRpcMessage(networkWriterPooled, model, syncIndex));
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<SubcontrollerRpcMessage>(ClientProcessMessage);
		};
	}

	private static void ClientProcessMessage(SubcontrollerRpcMessage msg)
	{
		try
		{
			msg.ProcessRpc();
		}
		catch (Exception exception)
		{
			Debug.Log("Exception in Subcontroller RPC handler for " + msg);
			Debug.LogException(exception);
		}
	}

	public static SubcontrollerRpcMessage ReadSubcontrollerRpcMessage(this NetworkReader reader)
	{
		return new SubcontrollerRpcMessage(reader);
	}

	public static void WriteSubcontrollerRpcMessage(this NetworkWriter writer, SubcontrollerRpcMessage msg)
	{
		msg.Serialize(writer);
	}

	public static bool TryGetRpcTarget(this IFpcRole role, out AnimatedCharacterModel target)
	{
		CharacterModel characterModelInstance = role.FpcModule.CharacterModelInstance;
		if (characterModelInstance is ISubcontrollerRpcRedirector subcontrollerRpcRedirector)
		{
			target = subcontrollerRpcRedirector.RpcTarget;
			return true;
		}
		if (characterModelInstance is AnimatedCharacterModel animatedCharacterModel)
		{
			target = animatedCharacterModel;
			return true;
		}
		target = null;
		return false;
	}
}
