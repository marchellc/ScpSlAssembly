using System;
using CustomPlayerEffects;
using Footprinting;
using GameCore;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using Respawning.NamingRules;
using Subtitles;
using UnityEngine;
using Utils.Networking;

namespace PlayerStatsSystem
{
	public abstract class AttackerDamageHandler : StandardDamageHandler
	{
		public abstract Footprint Attacker { get; protected set; }

		public bool IsFriendlyFire { get; private set; }

		public bool IsSuicide { get; private set; }

		public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
		{
			get
			{
				PlayerRoleBase playerRoleBase;
				if (!this.Attacker.IsSet || !PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this.Attacker.Role, out playerRoleBase))
				{
					return DamageHandlerBase.CassieAnnouncement.Default;
				}
				DamageHandlerBase.CassieAnnouncement cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement();
				switch (playerRoleBase.Team)
				{
				case Team.SCPs:
				case Team.Flamingos:
				{
					string text;
					string text2;
					NineTailedFoxAnnouncer.ConvertSCP(this.Attacker.Role, out text, out text2);
					cassieAnnouncement.Announcement = "TERMINATED BY SCP " + text2;
					cassieAnnouncement.SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.TerminatedBySCP, new string[] { text })
					};
					return cassieAnnouncement;
				}
				case Team.FoundationForces:
				{
					UnitNamingRule unitNamingRule;
					if (!NamingRulesManager.TryGetNamingRule(playerRoleBase.Team, out unitNamingRule))
					{
						cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY CONTAINMENTUNIT UNKNOWN";
						cassieAnnouncement.SubtitleParts = new SubtitlePart[]
						{
							new SubtitlePart(SubtitleType.ContainUnitUnknown, null)
						};
						return cassieAnnouncement;
					}
					string text3 = unitNamingRule.TranslateToCassie(this.Attacker.UnitName);
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY CONTAINMENTUNIT " + text3;
					cassieAnnouncement.SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.ContainUnit, new string[] { this.Attacker.UnitName })
					};
					return cassieAnnouncement;
				}
				case Team.ChaosInsurgency:
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY CHAOSINSURGENCY";
					cassieAnnouncement.SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.ContainedByChaos, null)
					};
					return cassieAnnouncement;
				case Team.Scientists:
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY SCIENCE PERSONNEL";
					cassieAnnouncement.SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.ContainedByScientist, null)
					};
					return cassieAnnouncement;
				case Team.ClassD:
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY CLASSD PERSONNEL";
					cassieAnnouncement.SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.ContainedByClassD, null)
					};
					return cassieAnnouncement;
				}
				return DamageHandlerBase.CassieAnnouncement.Default;
			}
		}

		public abstract bool AllowSelfDamage { get; }

		protected virtual bool ForceFullFriendlyFire { get; set; }

		public virtual bool IgnoreFriendlyFireDetector
		{
			get
			{
				return this.ForceFullFriendlyFire;
			}
		}

		public override string ServerMetricsText
		{
			get
			{
				return this.Attacker.LifeIdentifier.ToString();
			}
		}

		protected override void ProcessDamage(ReferenceHub ply)
		{
			if (this.DisableSpawnProtect(this.Attacker.Hub, ply))
			{
				this.Damage = 0f;
				return;
			}
			foreach (StatusEffectBase statusEffectBase in ply.playerEffectsController.AllEffects)
			{
				IFriendlyFireModifier friendlyFireModifier = statusEffectBase as IFriendlyFireModifier;
				if (friendlyFireModifier != null && statusEffectBase.IsEnabled && friendlyFireModifier.AllowFriendlyFire(this.Damage, this, this.Hitbox))
				{
					this.ForceFullFriendlyFire = true;
					break;
				}
			}
			if (ply.networkIdentity.netId == this.Attacker.NetId || this.ForceFullFriendlyFire)
			{
				if (!this.AllowSelfDamage && !this.ForceFullFriendlyFire)
				{
					this.Damage = 0f;
					return;
				}
				this.IsSuicide = true;
			}
			else if (!HitboxIdentity.IsEnemy(this.Attacker.Role, ply.GetRoleId()))
			{
				this.Damage *= AttackerDamageHandler._ffMultiplier;
				this.IsFriendlyFire = true;
			}
			base.ProcessDamage(ply);
		}

		private bool DisableSpawnProtect(ReferenceHub attacker, ReferenceHub target)
		{
			return !(attacker == null) && (SpawnProtected.CanShoot && SpawnProtected.CheckPlayer(attacker)) && attacker != target;
		}

		public override void WriteDeathScreen(NetworkWriter writer)
		{
			writer.WriteSpawnReason(SpectatorSpawnReason.KilledByPlayer);
			writer.WriteString(this.Attacker.Nickname);
			writer.WriteRoleType(this.Attacker.Role);
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteReferenceHub(this.Attacker.Hub);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this.Attacker = new Footprint(reader.ReadReferenceHub());
		}

		[RuntimeInitializeOnLoadMethod]
		private static void RefreshConfigs()
		{
			if (!AttackerDamageHandler._eventAssigned)
			{
				ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(AttackerDamageHandler.RefreshConfigs));
				ServerConfigSynchronizer.OnRefreshed = (Action)Delegate.Combine(ServerConfigSynchronizer.OnRefreshed, new Action(AttackerDamageHandler.RefreshConfigs));
				AttackerDamageHandler._eventAssigned = true;
			}
			AttackerDamageHandler._ffMultiplier = (ServerConsole.FriendlyFire ? ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f) : 0f);
		}

		private static float _ffMultiplier = 1f;

		private static bool _eventAssigned = false;
	}
}
