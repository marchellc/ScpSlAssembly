using System;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.NamingRules
{
	public static class UnitNameMessageHandler
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (!NetworkServer.active || hub.isLocalPlayer)
				{
					return;
				}
				NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(new ArraySegment<byte>(UnitNameMessageHandler.SendHistory.buffer, 0, UnitNameMessageHandler.SendHistory.Position));
				while (networkReaderPooled.Remaining > 0)
				{
					Team team = (Team)networkReaderPooled.ReadByte();
					UnitNamingRule unitNamingRule;
					if (!NamingRulesManager.TryGetNamingRule(team, out unitNamingRule))
					{
						break;
					}
					hub.connectionToClient.Send<UnitNameMessage>(new UnitNameMessage
					{
						Data = networkReaderPooled,
						NamingRule = unitNamingRule,
						Team = team
					}, 0);
				}
				networkReaderPooled.Dispose();
			}));
		}

		public static UnitNameMessage ReadUnitName(this NetworkReader reader)
		{
			Team team = (Team)reader.ReadByte();
			UnitNamingRule unitNamingRule;
			if (!NamingRulesManager.TryGetNamingRule(team, out unitNamingRule))
			{
				throw new InvalidOperationException(string.Format("No compatible decoder detected to read the name of spawnable team: {0}.", team));
			}
			return new UnitNameMessage
			{
				Team = team,
				NamingRule = unitNamingRule,
				UnitName = unitNamingRule.ReadName(reader)
			};
		}

		public static void WriteUnitName(this NetworkWriter writer, UnitNameMessage msg)
		{
			byte team = (byte)msg.Team;
			writer.WriteByte(team);
			if (msg.Data == null)
			{
				int position = writer.Position;
				msg.NamingRule.WriteName(writer);
				UnitNameMessageHandler.SendHistory.WriteByte(team);
				UnitNameMessageHandler.SendHistory.WriteBytes(writer.buffer, position, writer.Position - position);
				return;
			}
			int position2 = msg.Data.Position;
			msg.NamingRule.ReadName(msg.Data);
			writer.WriteBytes(msg.Data.buffer.Array, position2, msg.Data.Position - position2);
		}

		public static void ResetHistory()
		{
			UnitNameMessageHandler.SendHistory.Reset();
		}

		private static readonly NetworkWriter SendHistory = new NetworkWriter();
	}
}
