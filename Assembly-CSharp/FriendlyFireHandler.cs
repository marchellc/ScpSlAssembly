using System;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

internal class FriendlyFireHandler
{
	internal FriendlyFireHandler(ReferenceHub hub)
	{
		this.Round = new RoundFriendlyFireDetector(hub);
		this.Life = new LifeFriendlyFireDetector(hub);
		this.Window = new WindowFriendlyFireDetector(hub);
		this.Respawn = new RespawnFriendlyFireDetector(hub);
		if (!FriendlyFireHandler._eventsAssigned)
		{
			PlayerStats.OnAnyPlayerDamaged += FriendlyFireHandler.OnAnyDamaged;
			PlayerStats.OnAnyPlayerDied += FriendlyFireHandler.OnAnyDied;
			FriendlyFireHandler._eventsAssigned = true;
		}
	}

	private static void OnAnyDied(ReferenceHub deadPlayer, DamageHandlerBase handler)
	{
		AttackerDamageHandler attackerDamageHandler;
		if (!FriendlyFireHandler.IsFriendlyFire(deadPlayer, handler, out attackerDamageHandler))
		{
			return;
		}
		ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
		if (hub == null)
		{
			if (FriendlyFireConfig.ExplosionAfterDisconnecting && attackerDamageHandler is ExplosionDamageHandler)
			{
				FriendlyFireDetector.TakeAction(attackerDamageHandler.Attacker, ref FriendlyFireConfig.ExplosionAfterDisconnectingAction, "Explosion After Disconnection", ref FriendlyFireConfig.ExplosionAfterDisconnectingTime, ref FriendlyFireConfig.ExplosionAfterDisconnectingBanReason, ref FriendlyFireConfig.ExplosionAfterDisconnectingBanReason, ref FriendlyFireConfig.ExplosionAfterDisconnectingAdminMessage, ref FriendlyFireConfig.ExplosionAfterDisconnectingBroadcastMessage, ref FriendlyFireConfig.ExplosionAfterDisconnectingWebhook);
			}
			return;
		}
		if (hub.FriendlyFireHandler.Respawn.RegisterKill())
		{
			return;
		}
		if (hub.FriendlyFireHandler.Window.RegisterKill())
		{
			return;
		}
		if (hub.FriendlyFireHandler.Life.RegisterKill())
		{
			return;
		}
		hub.FriendlyFireHandler.Round.RegisterKill();
	}

	private static void OnAnyDamaged(ReferenceHub damagedPlayer, DamageHandlerBase handler)
	{
		AttackerDamageHandler attackerDamageHandler;
		if (!FriendlyFireHandler.IsFriendlyFire(damagedPlayer, handler, out attackerDamageHandler))
		{
			return;
		}
		ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
		if (hub == null)
		{
			return;
		}
		float damage = attackerDamageHandler.Damage;
		if (hub.FriendlyFireHandler.Respawn.RegisterDamage(damage))
		{
			return;
		}
		if (hub.FriendlyFireHandler.Window.RegisterDamage(damage))
		{
			return;
		}
		if (hub.FriendlyFireHandler.Life.RegisterDamage(damage))
		{
			return;
		}
		hub.FriendlyFireHandler.Round.RegisterDamage(damage);
	}

	private static bool IsFriendlyFire(ReferenceHub damagedPlayer, DamageHandlerBase handler, out AttackerDamageHandler attackerHandler)
	{
		attackerHandler = null;
		if (FriendlyFireConfig.PauseDetector || !NetworkServer.active)
		{
			return false;
		}
		AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
		if (attackerDamageHandler == null || attackerDamageHandler.IgnoreFriendlyFireDetector)
		{
			return false;
		}
		attackerHandler = attackerDamageHandler;
		PlayerRoleBase playerRoleBase;
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(attackerHandler.Attacker.Role, out playerRoleBase))
		{
			return false;
		}
		if (damagedPlayer.GetFaction() != playerRoleBase.Team.GetFaction())
		{
			return false;
		}
		ReferenceHub hub = attackerHandler.Attacker.Hub;
		return (!(hub != null) || (!(hub == damagedPlayer) && !PermissionsHandler.IsPermitted(hub.serverRoles.Permissions, PlayerPermissions.FriendlyFireDetectorImmunity))) && (!FriendlyFireConfig.IgnoreClassDTeamkills || damagedPlayer.GetRoleId() != RoleTypeId.ClassD || attackerHandler.Attacker.Role != RoleTypeId.ClassD);
	}

	private static bool _eventsAssigned;

	internal readonly RoundFriendlyFireDetector Round;

	internal readonly LifeFriendlyFireDetector Life;

	internal readonly WindowFriendlyFireDetector Window;

	internal readonly RespawnFriendlyFireDetector Respawn;
}
