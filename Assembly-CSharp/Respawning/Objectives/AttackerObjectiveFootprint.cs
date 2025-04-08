using System;
using System.Text;
using Mirror;
using PlayerRoles;

namespace Respawning.Objectives
{
	public abstract class AttackerObjectiveFootprint : ObjectiveFootprintBase
	{
		public ObjectiveHubFootprint VictimFootprint { get; set; }

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this.VictimFootprint.Write(writer);
		}

		public override void ClientReadRpc(NetworkReader reader)
		{
			base.ClientReadRpc(reader);
			this.VictimFootprint = new ObjectiveHubFootprint(reader);
		}

		public override StringBuilder ClientCompletionText(StringBuilder builder)
		{
			base.ClientCompletionText(builder);
			RoleTypeId roleType = this.VictimFootprint.RoleType;
			string text = ((roleType.GetTeam() == Team.SCPs) ? roleType.GetAbbreviatedRoleName() : this.VictimFootprint.Nickname);
			builder.Replace("%victimColor%", roleType.GetRoleColor().ToHex());
			builder.Replace("%victimName%", text);
			return builder;
		}
	}
}
