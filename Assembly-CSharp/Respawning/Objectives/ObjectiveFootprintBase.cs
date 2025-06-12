using System.Text;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.Objectives;

public abstract class ObjectiveFootprintBase
{
	public float InfluenceReward { get; set; }

	public float TimeReward { get; set; }

	public ObjectiveHubFootprint AchievingPlayer { get; set; }

	protected abstract FootprintsTranslation TargetTranslation { get; }

	public virtual void ClientReadRpc(NetworkReader reader)
	{
		this.InfluenceReward = reader.ReadFloat();
		this.TimeReward = reader.ReadFloat();
		this.AchievingPlayer = new ObjectiveHubFootprint(reader);
	}

	public virtual void ServerWriteRpc(NetworkWriter writer)
	{
		writer.WriteFloat(this.InfluenceReward);
		writer.WriteFloat(this.TimeReward);
		this.AchievingPlayer.Write(writer);
	}

	public virtual StringBuilder ClientCompletionText(StringBuilder builder)
	{
		RoleTypeId roleType = this.AchievingPlayer.RoleType;
		Color roleColor = roleType.GetRoleColor();
		bool flag = this.TimeReward != 0f;
		bool num = this.InfluenceReward != 0f;
		string newValue = string.Empty;
		string newValue2 = string.Empty;
		builder.Append(Translations.Get(this.TargetTranslation));
		builder.Append(Translations.Get(FootprintsTranslation.GenericRewardsDisplay));
		if (num)
		{
			newValue2 = Translations.Get(FootprintsTranslation.InfluenceRewardDisplay);
		}
		if (flag)
		{
			newValue = Translations.Get(FootprintsTranslation.TimerRewardDisplay);
		}
		builder.Replace("%timeDisplay%", newValue2);
		builder.Replace("%influenceDisplay%", newValue);
		if (num)
		{
			builder.Replace("%influence%", this.InfluenceReward.ToString());
		}
		if (flag)
		{
			builder.Replace("%time%", this.TimeReward.ToString());
		}
		builder.Replace("%achieverColor%", roleColor.ToHex());
		builder.Replace("%achieverName%", this.AchievingPlayer.Nickname);
		builder.Replace("%factionColor%", roleType.GetFaction().GetFactionColor());
		if (num || flag)
		{
			builder.Replace("(", "[");
			builder.Replace(")", "]");
		}
		return builder;
	}
}
