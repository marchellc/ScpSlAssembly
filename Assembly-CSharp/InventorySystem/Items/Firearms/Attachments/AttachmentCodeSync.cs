using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	public static class AttachmentCodeSync
	{
		public static event Action<ushort, uint> OnReceived;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += AttachmentCodeSync.OnClientReady;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(AttachmentCodeSync.OnNewPlayerAdded));
		}

		private static void OnClientReady()
		{
			AttachmentCodeSync.ReceivedCodes.Clear();
			NetworkClient.ReplaceHandler<AttachmentCodeSync.AttachmentCodeMessage>(delegate(AttachmentCodeSync.AttachmentCodeMessage x)
			{
				x.Apply();
			}, true);
			NetworkClient.ReplaceHandler<AttachmentCodeSync.AttachmentCodePackMessage>(delegate(AttachmentCodeSync.AttachmentCodePackMessage x)
			{
				x.Apply();
			}, true);
		}

		private static void OnNewPlayerAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			hub.connectionToClient.Send<AttachmentCodeSync.AttachmentCodePackMessage>(default(AttachmentCodeSync.AttachmentCodePackMessage), 0);
		}

		public static bool TryGet(ushort serial, out uint code)
		{
			return AttachmentCodeSync.ReceivedCodes.TryGetValue(serial, out code);
		}

		public static void ServerSetCode(ushort serial, uint code)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Attempting to override attachment code on client!");
			}
			AttachmentCodeSync.ReceivedCodes[serial] = code;
			NetworkServer.SendToAll<AttachmentCodeSync.AttachmentCodeMessage>(new AttachmentCodeSync.AttachmentCodeMessage(serial, code), 0, false);
		}

		public static void ServerResendAttachmentCode(this Firearm firearm)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Attempting to resend attachment code on client!");
			}
			AttachmentCodeSync.ServerSetCode(firearm.ItemSerial, firearm.GetCurrentAttachmentsCode());
		}

		public static void WriteAttachmentCodeMessage(this NetworkWriter writer, AttachmentCodeSync.AttachmentCodeMessage value)
		{
			value.Serialize(writer);
		}

		public static AttachmentCodeSync.AttachmentCodeMessage ReadAttachmentCodeMessage(this NetworkReader reader)
		{
			return new AttachmentCodeSync.AttachmentCodeMessage(reader);
		}

		public static void WriteAttachmentCodePackMessage(this NetworkWriter writer, AttachmentCodeSync.AttachmentCodePackMessage value)
		{
			value.Serialize(writer);
		}

		public static AttachmentCodeSync.AttachmentCodePackMessage ReadAttachmentCodePackMessage(this NetworkReader reader)
		{
			return new AttachmentCodeSync.AttachmentCodePackMessage(reader);
		}

		private static readonly Dictionary<ushort, uint> ReceivedCodes = new Dictionary<ushort, uint>();

		public readonly struct AttachmentCodeMessage : NetworkMessage
		{
			public AttachmentCodeMessage(NetworkReader reader)
			{
				this.WeaponSerial = reader.ReadUShort();
				this.AttachmentCode = reader.ReadUInt();
			}

			public AttachmentCodeMessage(ushort serial, uint attCode)
			{
				this.WeaponSerial = serial;
				this.AttachmentCode = attCode;
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteUShort(this.WeaponSerial);
				writer.WriteUInt(this.AttachmentCode);
			}

			public void Apply()
			{
				AttachmentCodeSync.ReceivedCodes[this.WeaponSerial] = this.AttachmentCode;
				Action<ushort, uint> onReceived = AttachmentCodeSync.OnReceived;
				if (onReceived == null)
				{
					return;
				}
				onReceived(this.WeaponSerial, this.AttachmentCode);
			}

			public readonly ushort WeaponSerial;

			public readonly uint AttachmentCode;
		}

		public readonly struct AttachmentCodePackMessage : NetworkMessage
		{
			public AttachmentCodePackMessage(NetworkReader reader)
			{
				AttachmentCodeSync.AttachmentCodePackMessage.LastDeserialized.Clear();
				int num = reader.ReadInt();
				for (int i = 0; i < num; i++)
				{
					AttachmentCodeSync.AttachmentCodePackMessage.LastDeserialized.Add(new AttachmentCodeSync.AttachmentCodeMessage(reader));
				}
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteInt(AttachmentCodeSync.ReceivedCodes.Count);
				foreach (KeyValuePair<ushort, uint> keyValuePair in AttachmentCodeSync.ReceivedCodes)
				{
					new AttachmentCodeSync.AttachmentCodeMessage(keyValuePair.Key, keyValuePair.Value).Serialize(writer);
				}
			}

			public void Apply()
			{
				AttachmentCodeSync.AttachmentCodePackMessage.LastDeserialized.ForEach(delegate(AttachmentCodeSync.AttachmentCodeMessage x)
				{
					x.Apply();
				});
			}

			private static readonly List<AttachmentCodeSync.AttachmentCodeMessage> LastDeserialized = new List<AttachmentCodeSync.AttachmentCodeMessage>();
		}
	}
}
