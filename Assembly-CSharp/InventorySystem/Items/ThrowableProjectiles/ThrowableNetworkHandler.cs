using System;
using System.Collections.Generic;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public static class ThrowableNetworkHandler
	{
		public static event Action<ThrowableNetworkHandler.ThrowableItemAudioMessage> OnAudioMessageReceived;

		public static event Action<ThrowableNetworkHandler.ThrowableItemRequestMessage> OnServerRequestReceived;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += ThrowableNetworkHandler.RegisterProjectiles;
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<ThrowableNetworkHandler.ThrowableItemRequestMessage>(new Action<NetworkConnectionToClient, ThrowableNetworkHandler.ThrowableItemRequestMessage>(ThrowableNetworkHandler.ServerProcessRequest), true);
				NetworkClient.ReplaceHandler<ThrowableNetworkHandler.ThrowableItemAudioMessage>(new Action<ThrowableNetworkHandler.ThrowableItemAudioMessage>(ThrowableNetworkHandler.ClientProcessAudio), true);
			};
		}

		private static void RegisterProjectiles()
		{
			foreach (KeyValuePair<ItemType, ItemBase> keyValuePair in InventoryItemLoader.AvailableItems)
			{
				ThrowableItem throwableItem = keyValuePair.Value as ThrowableItem;
				if (throwableItem != null && !(throwableItem.Projectile == null))
				{
					uint assetId = throwableItem.Projectile.netIdentity.assetId;
					NetworkClient.prefabs[assetId] = throwableItem.Projectile.gameObject;
				}
			}
		}

		private static void ServerProcessRequest(NetworkConnection conn, ThrowableNetworkHandler.ThrowableItemRequestMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			if (referenceHub.inventory.CurItem.SerialNumber != msg.Serial)
			{
				return;
			}
			ThrowableItem throwableItem = referenceHub.inventory.CurInstance as ThrowableItem;
			if (throwableItem == null)
			{
				return;
			}
			switch (msg.Request)
			{
			case ThrowableNetworkHandler.RequestType.BeginThrow:
				throwableItem.ServerProcessInitiation();
				break;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
				throwableItem.ServerProcessThrowConfirmation(false, msg.CameraPosition.Position, msg.CameraRotation, msg.PlayerVelocity);
				break;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
				throwableItem.ServerProcessThrowConfirmation(true, msg.CameraPosition.Position, msg.CameraRotation, msg.PlayerVelocity);
				break;
			case ThrowableNetworkHandler.RequestType.CancelThrow:
				throwableItem.ServerProcessCancellation();
				break;
			}
			Action<ThrowableNetworkHandler.ThrowableItemRequestMessage> onServerRequestReceived = ThrowableNetworkHandler.OnServerRequestReceived;
			if (onServerRequestReceived == null)
			{
				return;
			}
			onServerRequestReceived(msg);
		}

		private static void ClientProcessAudio(ThrowableNetworkHandler.ThrowableItemAudioMessage msg)
		{
			ThrowableNetworkHandler.ReceivedRequests[msg.Serial] = msg;
			Action<ThrowableNetworkHandler.ThrowableItemAudioMessage> onAudioMessageReceived = ThrowableNetworkHandler.OnAudioMessageReceived;
			if (onAudioMessageReceived != null)
			{
				onAudioMessageReceived(msg);
			}
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(msg.Serial, out referenceHub) || referenceHub.isLocalPlayer)
			{
				return;
			}
			ThrowableItem throwableItem;
			if (!InventoryItemLoader.TryGetItem<ThrowableItem>(referenceHub.inventory.CurItem.TypeId, out throwableItem))
			{
				return;
			}
			AudioClip audioClip;
			switch (msg.Request)
			{
			case ThrowableNetworkHandler.RequestType.BeginThrow:
				audioClip = throwableItem.BeginClip;
				break;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
			case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
				audioClip = throwableItem.ThrowClip;
				break;
			case ThrowableNetworkHandler.RequestType.CancelThrow:
				audioClip = throwableItem.CancelClip;
				break;
			default:
				return;
			}
			float num;
			if (!throwableItem.ItemTypeId.TryGetSpeedMultiplier(referenceHub, out num))
			{
				num = 1f;
			}
			AudioSourcePoolManager.PlayOnTransform(audioClip, referenceHub.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, num);
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

		private static bool RequiresAdditionalData(ThrowableNetworkHandler.RequestType rq)
		{
			return rq == ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce || rq == ThrowableNetworkHandler.RequestType.ConfirmThrowWeak;
		}

		public static void SerializeRequestMsg(this NetworkWriter writer, ThrowableNetworkHandler.ThrowableItemRequestMessage value)
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

		public static ThrowableNetworkHandler.ThrowableItemRequestMessage DeserializeRequestMsg(this NetworkReader reader)
		{
			ushort num = reader.ReadUShort();
			ThrowableNetworkHandler.RequestType requestType = (ThrowableNetworkHandler.RequestType)reader.ReadByte();
			bool flag = ThrowableNetworkHandler.RequiresAdditionalData(requestType);
			Quaternion quaternion = (flag ? reader.ReadLowPrecisionQuaternion().Value : default(Quaternion));
			RelativePosition relativePosition = (flag ? reader.ReadRelativePosition() : default(RelativePosition));
			Vector3 vector = (flag ? reader.ReadVector3() : default(Vector3));
			return new ThrowableNetworkHandler.ThrowableItemRequestMessage(num, requestType, quaternion, relativePosition, vector);
		}

		public static void SerializeAudioMsg(this NetworkWriter writer, ThrowableNetworkHandler.ThrowableItemAudioMessage value)
		{
			writer.WriteUShort(value.Serial);
			writer.WriteByte((byte)value.Request);
		}

		public static ThrowableNetworkHandler.ThrowableItemAudioMessage DeserializeAudioMsg(this NetworkReader reader)
		{
			return new ThrowableNetworkHandler.ThrowableItemAudioMessage(reader.ReadUShort(), (ThrowableNetworkHandler.RequestType)reader.ReadByte());
		}

		public static readonly Dictionary<ushort, ThrowableNetworkHandler.ThrowableItemAudioMessage> ReceivedRequests = new Dictionary<ushort, ThrowableNetworkHandler.ThrowableItemAudioMessage>();

		private const float MaxPlayerSpeed = 10f;

		public readonly struct ThrowableItemRequestMessage : NetworkMessage
		{
			public ThrowableItemRequestMessage(ushort serial, ThrowableNetworkHandler.RequestType type, Quaternion rotation, RelativePosition position, Vector3 startVel)
			{
				this.Serial = serial;
				this.Request = type;
				this.CameraRotation = rotation;
				this.CameraPosition = position;
				this.PlayerVelocity = startVel;
			}

			public ThrowableItemRequestMessage(ThrowableItem item, ThrowableNetworkHandler.RequestType type, Vector3 startVel = default(Vector3))
			{
				this.Serial = item.ItemSerial;
				this.Request = type;
				this.CameraRotation = item.Owner.PlayerCameraReference.rotation;
				this.CameraPosition = new RelativePosition(item.Owner.PlayerCameraReference.position);
				this.PlayerVelocity = startVel;
			}

			public readonly ushort Serial;

			public readonly ThrowableNetworkHandler.RequestType Request;

			public readonly Quaternion CameraRotation;

			public readonly RelativePosition CameraPosition;

			public readonly Vector3 PlayerVelocity;
		}

		public readonly struct ThrowableItemAudioMessage : NetworkMessage
		{
			public ThrowableItemAudioMessage(ushort itemSerial, ThrowableNetworkHandler.RequestType rt)
			{
				this.Serial = itemSerial;
				this.Request = rt;
				this.Time = global::UnityEngine.Time.timeSinceLevelLoad;
			}

			public readonly ushort Serial;

			public readonly ThrowableNetworkHandler.RequestType Request;

			public readonly float Time;
		}

		public enum RequestType : byte
		{
			BeginThrow,
			ConfirmThrowWeak,
			ConfirmThrowFullForce,
			CancelThrow
		}
	}
}
