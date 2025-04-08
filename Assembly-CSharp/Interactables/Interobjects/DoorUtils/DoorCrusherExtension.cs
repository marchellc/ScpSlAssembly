using System;
using GameCore;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class DoorCrusherExtension : DoorVariantExtension
	{
		private void Start()
		{
			if (!ConfigFile.ServerConfig.GetBool("crush_players", true))
			{
				global::UnityEngine.Object.Destroy(this);
				return;
			}
			this._enabled = (this.DeathCollider.enabled = false);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHub(other.transform.root.gameObject, out referenceHub))
			{
				return;
			}
			PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
			if (currentRole.RoleTypeId == RoleTypeId.Scp106)
			{
				return;
			}
			bool flag = currentRole.Team == Team.SCPs;
			if (this.IgnoreScps && flag)
			{
				return;
			}
			float num = (flag ? this.ScpCrushDamage : (-1f));
			referenceHub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Crushed, null));
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			bool flag = this.IsColliderEnabled();
			if (this._enabled == flag)
			{
				return;
			}
			this.DeathCollider.enabled = (this._enabled = flag);
		}

		private bool IsColliderEnabled()
		{
			if (this.TargetDoor.TargetState)
			{
				return false;
			}
			PryableDoor pryableDoor = this.TargetDoor as PryableDoor;
			if (pryableDoor != null && pryableDoor.IsBeingPried)
			{
				return false;
			}
			float exactState = this.TargetDoor.GetExactState();
			return exactState < this.MaxCrushThreshold && exactState > this.MinCrushThreshold;
		}

		private const string CrushSettingsKey = "crush_players";

		public float MaxCrushThreshold = 0.2f;

		public float MinCrushThreshold = 0.1f;

		public Collider DeathCollider;

		public float ScpCrushDamage = 200f;

		public bool IgnoreScps;

		private bool _enabled;
	}
}
