using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

public class HitboxIdentity : MonoBehaviour, IDestructible
{
	private static bool AllowFriendlyFire
	{
		get
		{
			return (ServerConfigSynchronizer.Singleton.MainBoolsSync & 1) == 1;
		}
	}

	public int Index
	{
		get
		{
			int num = this._indexCache.GetValueOrDefault();
			if (this._indexCache == null)
			{
				num = this.CharacterModel.Hitboxes.IndexOf(this);
				this._indexCache = new int?(num);
				return num;
			}
			return num;
		}
	}

	public ReferenceHub TargetHub
	{
		get
		{
			return this.CharacterModel.OwnerHub;
		}
	}

	public HitboxType HitboxType
	{
		get
		{
			return this._dmgMultiplier;
		}
	}

	public CharacterModel CharacterModel { get; private set; }

	public Collider[] TargetColliders { get; private set; }

	public uint NetworkId
	{
		get
		{
			return this.TargetHub.inventory.netId;
		}
	}

	public Vector3 CenterOfMass
	{
		get
		{
			return base.transform.position;
		}
	}

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactPos)
	{
		if (this.TargetHub == null)
		{
			return false;
		}
		StandardDamageHandler standardDamageHandler = handler as StandardDamageHandler;
		if (standardDamageHandler != null)
		{
			standardDamageHandler.Hitbox = this._dmgMultiplier;
		}
		return this.TargetHub.playerStats.DealDamage(handler);
	}

	public void SetColliders(bool newState)
	{
		Collider[] targetColliders = this.TargetColliders;
		for (int i = 0; i < targetColliders.Length; i++)
		{
			targetColliders[i].enabled = newState;
		}
	}

	private void Awake()
	{
		this.CharacterModel = base.GetComponentInParent<CharacterModel>();
		this.TargetColliders = base.GetComponents<Collider>();
	}

	private void OnDestroy()
	{
		HitboxIdentity.Instances.Remove(this);
	}

	public static bool IsEnemy(ReferenceHub attacker, ReferenceHub victim)
	{
		return HitboxIdentity.IsEnemy(attacker.GetTeam(), victim.GetTeam());
	}

	public static bool IsEnemy(RoleTypeId attackerRole, RoleTypeId victimRole)
	{
		Team team = attackerRole.GetTeam();
		Team team2 = victimRole.GetTeam();
		return HitboxIdentity.IsEnemy(team, team2);
	}

	public static bool IsEnemy(Team attackerTeam, Team victimTeam)
	{
		return attackerTeam != Team.Dead && victimTeam != Team.Dead && (attackerTeam != Team.SCPs || victimTeam != Team.SCPs) && attackerTeam.GetFaction() != victimTeam.GetFaction();
	}

	public static bool IsDamageable(ReferenceHub attacker, ReferenceHub victim)
	{
		return HitboxIdentity.AllowFriendlyFire || HitboxIdentity.IsEnemy(attacker, victim);
	}

	public static bool IsDamageable(RoleTypeId attackerRole, RoleTypeId victimRole)
	{
		return HitboxIdentity.AllowFriendlyFire || HitboxIdentity.IsEnemy(attackerRole, victimRole);
	}

	public static bool IsDamageable(Team attackerTeam, Team victimTeam)
	{
		if (!HitboxIdentity.AllowFriendlyFire)
		{
			return HitboxIdentity.IsEnemy(attackerTeam, victimTeam);
		}
		return attackerTeam != Team.Dead && victimTeam != Team.Dead && (attackerTeam != Team.SCPs || victimTeam != Team.SCPs);
	}

	public static readonly HashSet<HitboxIdentity> Instances = new HashSet<HitboxIdentity>();

	[SerializeField]
	private HitboxType _dmgMultiplier;

	private int? _indexCache;
}
