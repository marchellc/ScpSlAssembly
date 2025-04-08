using System;
using System.Text;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.Objectives
{
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
			bool flag2 = this.InfluenceReward != 0f;
			string text = string.Empty;
			string text2 = string.Empty;
			builder.Append(Translations.Get<FootprintsTranslation>(this.TargetTranslation));
			builder.Append(Translations.Get<FootprintsTranslation>(FootprintsTranslation.GenericRewardsDisplay));
			if (flag2)
			{
				text2 = Translations.Get<FootprintsTranslation>(FootprintsTranslation.InfluenceRewardDisplay);
			}
			if (flag)
			{
				text = Translations.Get<FootprintsTranslation>(FootprintsTranslation.TimerRewardDisplay);
			}
			builder.Replace("%timeDisplay%", text2);
			builder.Replace("%influenceDisplay%", text);
			if (flag2)
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
			if (flag2 || flag)
			{
				builder.Replace("(", "[");
				builder.Replace(")", "]");
			}
			return builder;
		}
	}
}
