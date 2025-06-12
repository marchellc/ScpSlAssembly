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

namespace PlayerRoles;

public class HumanRole : FpcStandardRoleBase, IArmoredRole, IInventoryRole, IHumeShieldedRole, ICustomNicknameDisplayRole, IBleedableRole
{
	[SerializeField]
	private RoleTypeId _roleId;

	[SerializeField]
	private Team _team;

	[SerializeField]
	private Color _roleColor;

	[SerializeField]
	private MonoBehaviour _spawnpointHandler;

	[field: SerializeField]
	public BloodSettings BloodSettings { get; private set; }

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	public override RoleTypeId RoleTypeId => this._roleId;

	public override Team Team => this._team;

	public override Color RoleColor => this._roleColor;

	public override float MaxHealth => 100f;

	public Color NicknameColor => this.RoleColor;

	public override ISpawnpointHandler SpawnpointHandler => this._spawnpointHandler as ISpawnpointHandler;

	public byte UnitNameId { get; private set; }

	private bool UsesUnitNames
	{
		get
		{
			UnitNamingRule rule;
			return NamingRulesManager.TryGetNamingRule(this.Team, out rule);
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
		if (NetworkServer.active && this.UsesUnitNames)
		{
			this.UnitNameId = (byte)(NamingRulesManager.GeneratedNames.TryGetValue(this.Team, out var value) ? ((byte)value.Count) : 0);
			if (this.UnitNameId != 0 && spawnReason != RoleChangeReason.Respawn)
			{
				this.UnitNameId--;
			}
		}
	}

	public int GetArmorEfficacy(HitboxType hitbox)
	{
		if (!base.TryGetOwner(out var hub) || !hub.inventory.TryGetBodyArmor(out var bodyArmor))
		{
			return 0;
		}
		return hitbox switch
		{
			HitboxType.Headshot => bodyArmor.HelmetEfficacy, 
			HitboxType.Body => bodyArmor.VestEfficacy, 
			_ => 0, 
		};
	}

	public void WriteNickname(StringBuilder sb)
	{
		if (base.TryGetOwner(out var hub))
		{
			NicknameSync.WriteDefaultInfo(hub, sb);
			if (NamingRulesManager.TryGetNamingRule(this.Team, out var rule))
			{
				PlayerInfoArea shownPlayerInfo = hub.nicknameSync.ShownPlayerInfo;
				string unitName = NamingRulesManager.ClientFetchReceived(this.Team, this.UnitNameId);
				rule.AppendName(sb, unitName, this.RoleTypeId, shownPlayerInfo);
			}
		}
	}

	public bool AllowDisarming(ReferenceHub detainer)
	{
		if (this.Team.GetFaction() == detainer.GetFaction())
		{
			return false;
		}
		if (!base.TryGetOwner(out var hub))
		{
			return false;
		}
		if (hub.interCoordinator.AnyBlocker(BlockedInteraction.BeDisarmed))
		{
			return false;
		}
		return true;
	}

	public bool AllowUndisarming(ReferenceHub releaser)
	{
		if (releaser.interCoordinator.AnyBlocker(BlockedInteraction.UndisarmPlayers))
		{
			return false;
		}
		return releaser.IsHuman();
	}
}
