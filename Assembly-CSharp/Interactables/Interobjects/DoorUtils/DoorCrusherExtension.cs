using GameCore;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public class DoorCrusherExtension : DoorVariantExtension
{
	private const string CrushSettingsKey = "crush_players";

	public float MaxCrushThreshold = 0.2f;

	public float MinCrushThreshold = 0.1f;

	public Collider DeathCollider;

	public float ScpCrushDamage = 200f;

	public bool IgnoreScps;

	private bool _enabled;

	private void Start()
	{
		if (!ConfigFile.ServerConfig.GetBool("crush_players", def: true))
		{
			Object.Destroy(this);
			return;
		}
		bool flag = (this.DeathCollider.enabled = false);
		this._enabled = flag;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkServer.active || !ReferenceHub.TryGetHub(other.transform.root.gameObject, out var hub))
		{
			return;
		}
		PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
		if (currentRole.RoleTypeId != RoleTypeId.Scp106)
		{
			bool flag = currentRole.Team == Team.SCPs;
			if (!(this.IgnoreScps && flag))
			{
				float damage = (flag ? this.ScpCrushDamage : (-1f));
				hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Crushed));
			}
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			bool flag = this.IsColliderEnabled();
			if (this._enabled != flag)
			{
				this.DeathCollider.enabled = (this._enabled = flag);
			}
		}
	}

	private bool IsColliderEnabled()
	{
		if (base.TargetDoor.TargetState)
		{
			return false;
		}
		if (base.TargetDoor is PryableDoor { IsBeingPried: not false })
		{
			return false;
		}
		float exactState = base.TargetDoor.GetExactState();
		if (exactState < this.MaxCrushThreshold)
		{
			return exactState > this.MinCrushThreshold;
		}
		return false;
	}
}
