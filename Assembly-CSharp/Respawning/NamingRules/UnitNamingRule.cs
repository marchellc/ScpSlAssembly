using System;
using System.Text;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.NamingRules
{
	[SerializeField]
	public abstract class UnitNamingRule
	{
		public string LastGeneratedName { get; protected set; }

		public abstract void GenerateNew();

		public virtual void AppendName(StringBuilder sb, string unitName, RoleTypeId theirRole, PlayerInfoArea infoFlags)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			if ((infoFlags & PlayerInfoArea.UnitName) == PlayerInfoArea.UnitName)
			{
				sb.Append(" (");
				sb.Append(unitName);
				sb.Append(")");
			}
			sb.AppendLine("\n");
			if ((infoFlags & PlayerInfoArea.PowerStatus) != PlayerInfoArea.PowerStatus)
			{
				return;
			}
			int rolePower = this.GetRolePower(humanRole.RoleTypeId);
			int rolePower2 = this.GetRolePower(theirRole);
			sb.Append("<b>");
			if (rolePower > rolePower2)
			{
				sb.Append(Translations.Get<LegacyInterfaces>(LegacyInterfaces.GiveOrders));
			}
			else if (rolePower < rolePower2)
			{
				sb.Append(Translations.Get<LegacyInterfaces>(LegacyInterfaces.FollowOrders));
			}
			else if (rolePower == rolePower2)
			{
				sb.Append(Translations.Get<LegacyInterfaces>(LegacyInterfaces.SameRank));
			}
			sb.Append("</b>");
		}

		public virtual string TranslateToCassie(string regular)
		{
			return string.Empty;
		}

		public abstract void WriteName(NetworkWriter writer);

		public abstract string ReadName(NetworkReader reader);

		public abstract int GetRolePower(RoleTypeId role);
	}
}
