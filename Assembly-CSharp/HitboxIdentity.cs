using System.Collections.Generic;
using MapGeneration.StaticHelpers;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

public class HitboxIdentity : MonoBehaviour, IDestructible, IBlockStaticBatching
{
	public static readonly HashSet<HitboxIdentity> Instances = new HashSet<HitboxIdentity>();

	[SerializeField]
	private HitboxType _dmgMultiplier;

	private int? _indexCache;

	private static bool AllowFriendlyFire => (ServerConfigSynchronizer.Singleton.MainBoolsSync & 1) == 1;

	public int Index
	{
		get
		{
			int valueOrDefault = this._indexCache.GetValueOrDefault();
			if (!this._indexCache.HasValue)
			{
				valueOrDefault = this.CharacterModel.Hitboxes.IndexOf(this);
				this._indexCache = valueOrDefault;
				return valueOrDefault;
			}
			return valueOrDefault;
		}
	}

	public ReferenceHub TargetHub => this.CharacterModel.OwnerHub;

	public HitboxType HitboxType => this._dmgMultiplier;

	public CharacterModel CharacterModel { get; private set; }

	public Collider[] TargetColliders { get; private set; }

	public uint NetworkId => this.TargetHub.inventory.netId;

	public Vector3 CenterOfMass => base.transform.position;

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactPos)
	{
		if (this.TargetHub == null)
		{
			return false;
		}
		if (handler is StandardDamageHandler standardDamageHandler)
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
		if (attackerTeam == Team.Dead || victimTeam == Team.Dead)
		{
			return false;
		}
		if (attackerTeam == Team.SCPs && victimTeam == Team.SCPs)
		{
			return false;
		}
		return attackerTeam.GetFaction() != victimTeam.GetFaction();
	}

	public static bool IsDamageable(ReferenceHub attacker, ReferenceHub victim)
	{
		if (!HitboxIdentity.AllowFriendlyFire)
		{
			return HitboxIdentity.IsEnemy(attacker, victim);
		}
		return true;
	}

	public static bool IsDamageable(RoleTypeId attackerRole, RoleTypeId victimRole)
	{
		if (!HitboxIdentity.AllowFriendlyFire)
		{
			return HitboxIdentity.IsEnemy(attackerRole, victimRole);
		}
		return true;
	}

	public static bool IsDamageable(Team attackerTeam, Team victimTeam)
	{
		if (!HitboxIdentity.AllowFriendlyFire)
		{
			return HitboxIdentity.IsEnemy(attackerTeam, victimTeam);
		}
		if (attackerTeam == Team.Dead || victimTeam == Team.Dead)
		{
			return false;
		}
		if (attackerTeam == Team.SCPs && victimTeam == Team.SCPs)
		{
			return false;
		}
		return true;
	}
}
