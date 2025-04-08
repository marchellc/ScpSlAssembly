using System;
using System.Collections.Generic;
using System.Text;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using Mirror;
using PlayerRoles.Blood;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.HumeShield;
using Respawning.NamingRules;
using UnityEngine;

namespace PlayerRoles
{
	public class HumanRole : FpcStandardRoleBase, IArmoredRole, IInventoryRole, IHumeShieldedRole, ICustomNicknameDisplayRole, IBleedableRole
	{
		public BloodSettings BloodSettings { get; private set; }

		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public override RoleTypeId RoleTypeId
		{
			get
			{
				return this._roleId;
			}
		}

		public override Team Team
		{
			get
			{
				return this._team;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return this._roleColor;
			}
		}

		public override float MaxHealth
		{
			get
			{
				return 100f;
			}
		}

		public override ISpawnpointHandler SpawnpointHandler
		{
			get
			{
				return this._spawnpointHandler as ISpawnpointHandler;
			}
		}

		public byte UnitNameId { get; private set; }

		private bool UsesUnitNames
		{
			get
			{
				UnitNamingRule unitNamingRule;
				return NamingRulesManager.TryGetNamingRule(this.Team, out unitNamingRule);
			}
		}

		public override void WritePublicSpawnData(NetworkWriter writer)
		{
			if (this.UsesUnitNames)
			{
				writer.WriteByte(this.UnitNameId);
			}
			base.WritePublicSpawnData(writer);
		}

		public override void ReadSpawnData(NetworkReader reader)
		{
			if (this.UsesUnitNames)
			{
				this.UnitNameId = reader.ReadByte();
			}
			base.ReadSpawnData(reader);
		}

		internal override void Init(ReferenceHub hub, RoleChangeReason spawnReason, RoleSpawnFlags spawnFlags)
		{
			base.Init(hub, spawnReason, spawnFlags);
			if (!NetworkServer.active || !this.UsesUnitNames)
			{
				return;
			}
			List<string> list;
			this.UnitNameId = (NamingRulesManager.GeneratedNames.TryGetValue(this.Team, out list) ? ((byte)list.Count) : 0);
			if (this.UnitNameId == 0 || spawnReason == RoleChangeReason.Respawn)
			{
				return;
			}
			byte unitNameId = this.UnitNameId;
			this.UnitNameId = unitNameId - 1;
		}

		public int GetArmorEfficacy(HitboxType hitbox)
		{
			ReferenceHub referenceHub;
			BodyArmor bodyArmor;
			if (!base.TryGetOwner(out referenceHub) || !referenceHub.inventory.TryGetBodyArmor(out bodyArmor))
			{
				return 0;
			}
			if (hitbox == HitboxType.Body)
			{
				return bodyArmor.VestEfficacy;
			}
			if (hitbox == HitboxType.Headshot)
			{
				return bodyArmor.HelmetEfficacy;
			}
			return 0;
		}

		public void WriteNickname(ReferenceHub owner, StringBuilder sb, out Color texColor)
		{
			NicknameSync.WriteDefaultInfo(owner, sb, out texColor, null);
			UnitNamingRule unitNamingRule;
			if (!NamingRulesManager.TryGetNamingRule(this.Team, out unitNamingRule))
			{
				return;
			}
			PlayerInfoArea shownPlayerInfo = owner.nicknameSync.ShownPlayerInfo;
			string text = NamingRulesManager.ClientFetchReceived(this.Team, (int)this.UnitNameId);
			unitNamingRule.AppendName(sb, text, this.RoleTypeId, shownPlayerInfo);
		}

		public bool AllowDisarming(ReferenceHub detainer)
		{
			ReferenceHub referenceHub;
			return this.Team.GetFaction() != detainer.GetFaction() && base.TryGetOwner(out referenceHub) && !referenceHub.interCoordinator.AnyBlocker(BlockedInteraction.BeDisarmed);
		}

		public bool AllowUndisarming(ReferenceHub releaser)
		{
			return !releaser.interCoordinator.AnyBlocker(BlockedInteraction.UndisarmPlayers) && releaser.IsHuman();
		}

		[SerializeField]
		private RoleTypeId _roleId;

		[SerializeField]
		private Team _team;

		[SerializeField]
		private Color _roleColor;

		[SerializeField]
		private MonoBehaviour _spawnpointHandler;
	}
}
