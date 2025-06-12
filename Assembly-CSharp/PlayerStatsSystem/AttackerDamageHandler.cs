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

namespace PlayerStatsSystem;

public abstract class AttackerDamageHandler : StandardDamageHandler
{
	private static float _ffMultiplier = 1f;

	private static bool _eventAssigned = false;

	public abstract Footprint Attacker { get; protected set; }

	public bool IsFriendlyFire { get; private set; }

	public bool IsSuicide { get; private set; }

	public override CassieAnnouncement CassieDeathAnnouncement
	{
		get
		{
			if (!this.Attacker.IsSet || !PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this.Attacker.Role, out var result))
			{
				return CassieAnnouncement.Default;
			}
			CassieAnnouncement cassieAnnouncement = new CassieAnnouncement();
			switch (result.Team)
			{
			case Team.ClassD:
				cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY CLASSD PERSONNEL";
				cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
				{
					new SubtitlePart(SubtitleType.ContainedByClassD, (string[])null)
				};
				break;
			case Team.ChaosInsurgency:
				cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY CHAOSINSURGENCY";
				cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
				{
					new SubtitlePart(SubtitleType.ContainedByChaos, (string[])null)
				};
				break;
			case Team.Scientists:
				cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY BY SCIENCE PERSONNEL";
				cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
				{
					new SubtitlePart(SubtitleType.ContainedByScientist, (string[])null)
				};
				break;
			case Team.FoundationForces:
			{
				if (!NamingRulesManager.TryGetNamingRule(result.Team, out var rule))
				{
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY CONTAINMENTUNIT UNKNOWN";
					cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
					{
						new SubtitlePart(SubtitleType.ContainUnitUnknown, (string[])null)
					};
				}
				else
				{
					string text = rule.TranslateToCassie(this.Attacker.UnitName);
					cassieAnnouncement.Announcement = "CONTAINEDSUCCESSFULLY CONTAINMENTUNIT " + text;
					cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
					{
						new SubtitlePart(SubtitleType.ContainUnit, this.Attacker.UnitName)
					};
				}
				break;
			}
			case Team.SCPs:
			case Team.Flamingos:
			{
				NineTailedFoxAnnouncer.ConvertSCP(this.Attacker.Role, out var withoutSpace, out var withSpace);
				cassieAnnouncement.Announcement = "TERMINATED BY SCP " + withSpace;
				cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
				{
					new SubtitlePart(SubtitleType.TerminatedBySCP, withoutSpace)
				};
				break;
			}
			default:
				return CassieAnnouncement.Default;
			}
			return cassieAnnouncement;
		}
	}

	public abstract bool AllowSelfDamage { get; }

	protected virtual bool ForceFullFriendlyFire { get; set; }

	public virtual bool IgnoreFriendlyFireDetector => this.ForceFullFriendlyFire;

	public override string ServerMetricsText
	{
		get
		{
			int lifeIdentifier = this.Attacker.LifeIdentifier;
			return lifeIdentifier.ToString();
		}
	}

	protected override void ProcessDamage(ReferenceHub ply)
	{
		if (this.DisableSpawnProtect(this.Attacker.Hub, ply))
		{
			this.Damage = 0f;
			return;
		}
		StatusEffectBase[] allEffects = ply.playerEffectsController.AllEffects;
		foreach (StatusEffectBase statusEffectBase in allEffects)
		{
			if (statusEffectBase is IFriendlyFireModifier friendlyFireModifier && statusEffectBase.IsEnabled && friendlyFireModifier.AllowFriendlyFire(this.Damage, this, base.Hitbox))
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
		if (attacker == null)
		{
			return false;
		}
		if (SpawnProtected.CanShoot && SpawnProtected.CheckPlayer(attacker))
		{
			return attacker != target;
		}
		return false;
	}

	public override void WriteDeathScreen(NetworkWriter writer)
	{
		writer.WriteSpawnReason(SpectatorSpawnReason.KilledByPlayer);
		writer.WriteUInt(this.Attacker.NetId);
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
			ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(RefreshConfigs));
			ServerConfigSynchronizer.OnRefreshed = (Action)Delegate.Combine(ServerConfigSynchronizer.OnRefreshed, new Action(RefreshConfigs));
			AttackerDamageHandler._eventAssigned = true;
		}
		AttackerDamageHandler._ffMultiplier = (ServerConsole.FriendlyFire ? ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f) : 0f);
	}
}
