using System.Text;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.NamingRules;

[SerializeField]
public abstract class UnitNamingRule
{
	public string LastGeneratedName { get; protected set; }

	public abstract void GenerateNew();

	public virtual void AppendName(StringBuilder sb, string unitName, RoleTypeId theirRole, PlayerInfoArea infoFlags)
	{
		if (!ReferenceHub.TryGetLocalHub(out var hub) || !(hub.roleManager.CurrentRole is HumanRole humanRole))
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
		if ((infoFlags & PlayerInfoArea.PowerStatus) == PlayerInfoArea.PowerStatus)
		{
			int rolePower = this.GetRolePower(humanRole.RoleTypeId);
			int rolePower2 = this.GetRolePower(theirRole);
			sb.Append("<b>");
			if (rolePower > rolePower2)
			{
				sb.Append(Translations.Get(LegacyInterfaces.GiveOrders));
			}
			else if (rolePower < rolePower2)
			{
				sb.Append(Translations.Get(LegacyInterfaces.FollowOrders));
			}
			else if (rolePower == rolePower2)
			{
				sb.Append(Translations.Get(LegacyInterfaces.SameRank));
			}
			sb.Append("</b>");
		}
	}

	public virtual string TranslateToCassie(string regular)
	{
		return string.Empty;
	}

	public abstract void WriteName(NetworkWriter writer);

	public abstract string ReadName(NetworkReader reader);

	public abstract int GetRolePower(RoleTypeId role);
}
