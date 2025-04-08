using System;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerStatsSystem
{
	public static class SyncedStatMessages
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += SyncedStatMessages.RegisterHandler;
			PlayerRoleManager.OnRoleChanged += SyncedStatMessages.OnRoleChanged;
		}

		private static void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevClass, PlayerRoleBase newClass)
		{
			if (!NetworkServer.active || !(newClass is SpectatorRole) || userHub.isLocalPlayer)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.IsAlive() && !(referenceHub == userHub))
				{
					SyncedStatMessages.SendAllStats(userHub.networkIdentity.connectionToClient, referenceHub.playerStats);
				}
			}
		}

		private static void SendAllStats(NetworkConnectionToClient conn, PlayerStats ply)
		{
			StatBase[] statModules = ply.StatModules;
			for (int i = 0; i < statModules.Length; i++)
			{
				SyncedStatBase syncedStatBase = statModules[i] as SyncedStatBase;
				if (syncedStatBase != null && syncedStatBase.Mode != SyncedStatBase.SyncMode.Private)
				{
					conn.Send<SyncedStatMessages.StatMessage>(new SyncedStatMessages.StatMessage
					{
						Stat = syncedStatBase,
						Type = SyncedStatMessages.StatMessageType.CurrentValue,
						SyncedValue = syncedStatBase.CurValue
					}, 0);
					conn.Send<SyncedStatMessages.StatMessage>(new SyncedStatMessages.StatMessage
					{
						Stat = syncedStatBase,
						Type = SyncedStatMessages.StatMessageType.MaxValue,
						SyncedValue = syncedStatBase.MaxValue
					}, 0);
				}
			}
		}

		public static void Serialize(this NetworkWriter writer, SyncedStatMessages.StatMessage value)
		{
			writer.WriteUInt(value.Stat.Hub.netId);
			writer.WriteByte(value.Stat.SyncId);
			writer.WriteByte((byte)value.Type);
			value.Stat.WriteValue(value.Type, writer);
		}

		public static SyncedStatMessages.StatMessage Deserialize(this NetworkReader reader)
		{
			uint num = reader.ReadUInt();
			byte b = reader.ReadByte();
			SyncedStatMessages.StatMessageType statMessageType = (SyncedStatMessages.StatMessageType)reader.ReadByte();
			SyncedStatBase statOfUser = SyncedStatBase.GetStatOfUser(num, b);
			return new SyncedStatMessages.StatMessage
			{
				Stat = statOfUser,
				Type = statMessageType,
				SyncedValue = statOfUser.ReadValue(reader)
			};
		}

		private static void RegisterHandler()
		{
			NetworkClient.ReplaceHandler<SyncedStatMessages.StatMessage>(delegate(SyncedStatMessages.StatMessage msg)
			{
				if (msg.Type == SyncedStatMessages.StatMessageType.CurrentValue)
				{
					msg.Stat.CurValue = msg.SyncedValue;
					return;
				}
				msg.Stat.MaxValue = msg.SyncedValue;
			}, true);
		}

		public struct StatMessage : NetworkMessage
		{
			public SyncedStatBase Stat;

			public SyncedStatMessages.StatMessageType Type;

			public float SyncedValue;
		}

		public enum StatMessageType : byte
		{
			CurrentValue,
			MaxValue
		}
	}
}
