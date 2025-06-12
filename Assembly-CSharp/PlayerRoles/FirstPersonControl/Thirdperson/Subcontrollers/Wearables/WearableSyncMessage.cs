using System;
using System.Collections.Generic;
using Mirror;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public readonly struct WearableSyncMessage : NetworkMessage
{
	private const int MaxSyncvarBytes = 128;

	private static readonly Stack<byte[]> PayloadPool = new Stack<byte[]>();

	private static readonly NetworkReader PayloadReader = new NetworkReader(new byte[128]);

	public readonly uint PlayerNetId;

	public readonly WearableElements Flags;

	public readonly byte[] Payload;

	public readonly int PayloadSize;

	public WearableSyncMessage(NetworkReader reader)
	{
		this.PlayerNetId = reader.ReadUInt();
		this.Flags = (WearableElements)reader.ReadByte();
		this.PayloadSize = reader.ReadByte();
		if (this.PayloadSize > 0)
		{
			if (!WearableSyncMessage.PayloadPool.TryPop(out this.Payload))
			{
				this.Payload = new byte[128];
			}
			reader.ReadBytes(this.Payload, this.PayloadSize);
		}
		else
		{
			this.Payload = null;
		}
	}

	public WearableSyncMessage(ReferenceHub hub, WearableElements flags, NetworkWriter payloadWriter)
	{
		this.PlayerNetId = hub.netId;
		this.Flags = flags;
		this.PayloadSize = payloadWriter.Position;
		if (this.PayloadSize > 128)
		{
			throw new InvalidOperationException(string.Format("Unable to create {0} with payload of {1}.", "WearableSyncMessage", this.PayloadSize));
		}
		if (this.PayloadSize > 0)
		{
			if (!WearableSyncMessage.PayloadPool.TryPop(out this.Payload))
			{
				this.Payload = new byte[128];
			}
			Array.Copy(payloadWriter.buffer, this.Payload, this.PayloadSize);
		}
		else
		{
			this.Payload = null;
		}
	}

	public WearableSyncMessage(ReferenceHub hub)
	{
		this.PlayerNetId = hub.netId;
		this.Flags = WearableElements.None;
		this.PayloadSize = 0;
		this.Payload = null;
	}

	public void Free()
	{
		WearableSyncMessage.PayloadPool.Push(this.Payload);
	}

	public NetworkReader GetPayloadReader()
	{
		if (this.Payload == null)
		{
			WearableSyncMessage.PayloadReader.Position = WearableSyncMessage.PayloadReader.Capacity;
		}
		else
		{
			WearableSyncMessage.PayloadReader.SetBuffer(new ArraySegment<byte>(this.Payload, 0, this.PayloadSize));
		}
		return WearableSyncMessage.PayloadReader;
	}

	public void SerializeAndFree(NetworkWriter writer)
	{
		writer.WriteUInt(this.PlayerNetId);
		writer.WriteByte((byte)this.Flags);
		writer.WriteByte((byte)this.PayloadSize);
		if (this.Payload != null)
		{
			writer.WriteBytes(this.Payload, 0, this.PayloadSize);
		}
	}
}
