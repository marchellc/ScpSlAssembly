using System;
using System.Collections.Generic;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public static class ThrowableNetworkHandler
{
	public readonly struct ThrowableItemRequestMessage : NetworkMessage
	{
		public readonly ushort Serial;

		public readonly RequestType Request;

		public readonly Quaternion CameraRotation;

		public readonly RelativePosition CameraPosition;

		public readonly Vector3 PlayerVelocity;

		public ThrowableItemRequestMessage(ushort serial, RequestType type, Quaternion rotation, RelativePosition position, Vector3 startVel)
		{
			this.Serial = serial;
			this.Request = type;
			this.CameraRotation = rotation;
			this.CameraPosition = position;
			this.PlayerVelocity = startVel;
		}

		public ThrowableItemRequestMessage(ThrowableItem item, RequestType type, Vector3 startVel = default(Vector3))
		{
			this.Serial = item.ItemSerial;
			this.Request = type;
			this.CameraRotation = item.Owner.PlayerCameraReference.rotation;
			this.CameraPosition = new RelativePosition(item.Owner.PlayerCameraReference.position);
			this.PlayerVelocity = startVel;
		}
	}

	public readonly struct ThrowableItemAudioMessage : NetworkMessage
	{
		public readonly ushort Serial;

		public readonly RequestType Request;

		public readonly float Time;

		public ThrowableItemAudioMessage(ushort itemSerial, RequestType rt)
		{
			this.Serial = itemSerial;
			this.Request = rt;
			this.Time = UnityEngine.Time.timeSinceLevelLoad;
		}
	}

	public enum RequestType : byte
	{
		BeginThrow,
		ConfirmThrowWeak,
		ConfirmThrowFullForce,
		CancelThrow,
		ForceCancel
	}

	public static readonly Dictionary<ushort, ThrowableItemAudioMessage> ReceivedRequests = new Dictionary<ushort, ThrowableItemAudioMessage>();

	private const float MaxPlayerSpeed = 10f;

	public static event Action<ThrowableItemAudioMessage> OnAudioMessageReceived;

	public static event Action<ThrowableItemRequestMessage> OnServerRequestReceived;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientStarted += RegisterProjectiles;
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkServer.ReplaceHandler<ThrowableItemRequestMessage>(ServerProcessRequest);
			NetworkClient.ReplaceHandler<ThrowableItemRequestMessage>(ClientProcessRequest);
			NetworkClient.ReplaceHandler<ThrowableItemAudioMessage>(ClientProcessAudio);
		};
	}

	private static void RegisterProjectiles()
	{
		foreach (KeyValuePair<ItemType, ItemBase> availableItem in InventoryItemLoader.AvailableItems)
		{
			if (availableItem.Value is ThrowableItem throwableItem && !(throwableItem.Projectile == null))
			{
				uint assetId = throwableItem.Projectile.netIdentity.assetId;
				NetworkClient.prefabs[assetId] = throwableItem.Projectile.gameObject;
			}
		}
	}

	private static void ServerProcessRequest(NetworkConnection conn, ThrowableItemRequestMessage msg)
	{
		if (ReferenceHub.TryGetHubNetID(conn.identity.netId, out var hub) && hub.inventory.CurItem.SerialNumber == msg.Serial && hub.inventory.CurInstance is ThrowableItem throwableItem)
		{
			switch (msg.Request)
			{
			case RequestType.BeginThrow:
				throwableItem.ServerProcessInitiation();
				break;
			case RequestType.CancelThrow:
				throwableItem.ServerProcessCancellation();
				break;
			case RequestType.ConfirmThrowFullForce:
				throwableItem.ServerProcessThrowConfirmation(fullForce: true, msg.CameraPosition.Position, msg.CameraRotation, msg.PlayerVelocity);
				break;
			case RequestType.ConfirmThrowWeak:
				throwableItem.ServerProcessThrowConfirmation(fullForce: false, msg.CameraPosition.Position, msg.CameraRotation, msg.PlayerVelocity);
				break;
			}
			ThrowableNetworkHandler.OnServerRequestReceived?.Invoke(msg);
		}
	}

	private static void ClientProcessRequest(ThrowableItemRequestMessage msg)
	{
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return;
		}
		ThrowableItem throwableItem = null;
		foreach (KeyValuePair<ushort, ItemBase> item in hub.inventory.UserInventory.Items)
		{
			if (item.Key == msg.Serial && item.Value is ThrowableItem throwableItem2)
			{
				throwableItem = throwableItem2;
				break;
			}
		}
		if (!(throwableItem == null) && msg.Request == RequestType.ForceCancel)
		{
			throwableItem.ClientForceCancel();
		}
	}

	private static void ClientProcessAudio(ThrowableItemAudioMessage msg)
	{
		ThrowableNetworkHandler.ReceivedRequests[msg.Serial] = msg;
		ThrowableNetworkHandler.OnAudioMessageReceived?.Invoke(msg);
		if (InventoryExtensions.TryGetHubHoldingSerial(msg.Serial, out var hub) && !hub.isLocalPlayer && InventoryItemLoader.TryGetItem<ThrowableItem>(hub.inventory.CurItem.TypeId, out var result))
		{
			AudioClip sound;
			switch (msg.Request)
			{
			default:
				return;
			case RequestType.BeginThrow:
				sound = result.BeginClip;
				break;
			case RequestType.CancelThrow:
				sound = result.CancelClip;
				break;
			case RequestType.ConfirmThrowWeak:
			case RequestType.ConfirmThrowFullForce:
				sound = result.ThrowClip;
				break;
			}
			if (!result.ItemTypeId.TryGetSpeedMultiplier(hub, out var multiplier))
			{
				multiplier = 1f;
			}
			AudioSourcePoolManager.PlayOnTransform(sound, hub.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, multiplier);
		}
	}

	public static Vector3 GetLimitedVelocity(Vector3 plyVel)
	{
		float magnitude = plyVel.magnitude;
		if (magnitude > 10f)
		{
			plyVel /= magnitude;
			plyVel *= 10f;
		}
		return plyVel;
	}

	private static bool RequiresAdditionalData(RequestType rq)
	{
		if (rq != RequestType.ConfirmThrowFullForce)
		{
			return rq == RequestType.ConfirmThrowWeak;
		}
		return true;
	}

	public static void SerializeRequestMsg(this NetworkWriter writer, ThrowableItemRequestMessage value)
	{
		writer.WriteUShort(value.Serial);
		writer.WriteByte((byte)value.Request);
		if (ThrowableNetworkHandler.RequiresAdditionalData(value.Request))
		{
			writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(value.CameraRotation));
			writer.WriteRelativePosition(value.CameraPosition);
			writer.WriteVector3(value.PlayerVelocity);
		}
	}

	public static ThrowableItemRequestMessage DeserializeRequestMsg(this NetworkReader reader)
	{
		ushort serial = reader.ReadUShort();
		RequestType requestType = (RequestType)reader.ReadByte();
		bool num = ThrowableNetworkHandler.RequiresAdditionalData(requestType);
		Quaternion rotation = (num ? reader.ReadLowPrecisionQuaternion().Value : default(Quaternion));
		RelativePosition position = (num ? reader.ReadRelativePosition() : default(RelativePosition));
		Vector3 startVel = (num ? reader.ReadVector3() : default(Vector3));
		return new ThrowableItemRequestMessage(serial, requestType, rotation, position, startVel);
	}

	public static void SerializeAudioMsg(this NetworkWriter writer, ThrowableItemAudioMessage value)
	{
		writer.WriteUShort(value.Serial);
		writer.WriteByte((byte)value.Request);
	}

	public static ThrowableItemAudioMessage DeserializeAudioMsg(this NetworkReader reader)
	{
		return new ThrowableItemAudioMessage(reader.ReadUShort(), (RequestType)reader.ReadByte());
	}
}
