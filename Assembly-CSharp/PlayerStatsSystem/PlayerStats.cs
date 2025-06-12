using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerStatsSystem;

public class PlayerStats : NetworkBehaviour
{
	public static readonly Type[] DefinedModules = new Type[6]
	{
		typeof(HealthStat),
		typeof(AhpStat),
		typeof(StaminaStat),
		typeof(AdminFlagsStat),
		typeof(HumeShieldStat),
		typeof(VigorStat)
	};

	private ReferenceHub _hub;

	private bool _eventAssigned;

	private StatBase[] _statModules;

	private readonly Dictionary<Type, StatBase> _dictionarizedTypes = new Dictionary<Type, StatBase>();

	public StatBase[] StatModules
	{
		get
		{
			if (this._statModules != null)
			{
				return this._statModules;
			}
			this._statModules = new StatBase[PlayerStats.DefinedModules.Length];
			for (int i = 0; i < PlayerStats.DefinedModules.Length; i++)
			{
				object obj = Activator.CreateInstance(PlayerStats.DefinedModules[i]);
				this._statModules[i] = obj as StatBase;
			}
			return this._statModules;
		}
	}

	public event Action<DamageHandlerBase> OnThisPlayerDamaged = delegate
	{
	};

	public event Action<DamageHandlerBase> OnThisPlayerDied = delegate
	{
	};

	public static event Action<ReferenceHub, DamageHandlerBase> OnAnyPlayerDamaged;

	public static event Action<ReferenceHub, DamageHandlerBase> OnAnyPlayerDied;

	private void Awake()
	{
		StatBase[] statModules = this.StatModules;
		foreach (StatBase statBase in statModules)
		{
			this._dictionarizedTypes.Add(statBase.GetType(), statBase);
		}
		this._hub = ReferenceHub.GetHub(base.gameObject);
		statModules = this.StatModules;
		for (int i = 0; i < statModules.Length; i++)
		{
			statModules[i].Init(this._hub);
		}
	}

	private void Start()
	{
		if (this._hub.isLocalPlayer)
		{
			PlayerRoleManager.OnRoleChanged += OnClassChanged;
			this._eventAssigned = true;
		}
	}

	private void OnDestroy()
	{
		if (this._eventAssigned)
		{
			PlayerRoleManager.OnRoleChanged -= OnClassChanged;
			this._eventAssigned = false;
		}
	}

	private void Update()
	{
		StatBase[] statModules = this.StatModules;
		for (int i = 0; i < statModules.Length; i++)
		{
			statModules[i].Update();
		}
	}

	public T GetModule<T>() where T : StatBase
	{
		return this._dictionarizedTypes[typeof(T)] as T;
	}

	public bool TryGetModule<T>(out T module) where T : StatBase
	{
		if (this._dictionarizedTypes.TryGetValue(typeof(T), out var value) && value is T val)
		{
			module = val;
			return true;
		}
		module = null;
		return false;
	}

	public bool DealDamage(DamageHandlerBase handler)
	{
		if (this._hub.characterClassManager.GodMode)
		{
			return false;
		}
		if (this._hub.playerEffectsController.TryGetEffect<SpawnProtected>(out var playerEffect) && playerEffect.IsEnabled)
		{
			return false;
		}
		if (this._hub.roleManager.CurrentRole is IDamageHandlerProcessingRole damageHandlerProcessingRole)
		{
			handler = damageHandlerProcessingRole.ProcessDamageHandler(handler);
		}
		ReferenceHub attacker = null;
		if (handler is AttackerDamageHandler attackerDamageHandler)
		{
			attacker = attackerDamageHandler.Attacker.Hub;
		}
		PlayerHurtingEventArgs e = new PlayerHurtingEventArgs(attacker, this._hub, handler);
		PlayerEvents.OnHurting(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		DamageHandlerBase.HandlerOutput handlerOutput = handler.ApplyDamage(this._hub);
		PlayerEvents.OnHurt(new PlayerHurtEventArgs(attacker, this._hub, handler));
		if (handlerOutput == DamageHandlerBase.HandlerOutput.Nothing)
		{
			return false;
		}
		PlayerStats.OnAnyPlayerDamaged?.Invoke(this._hub, handler);
		this.OnThisPlayerDamaged?.Invoke(handler);
		if (handlerOutput == DamageHandlerBase.HandlerOutput.Death)
		{
			PlayerDyingEventArgs e2 = new PlayerDyingEventArgs(this._hub, attacker, handler);
			PlayerEvents.OnDying(e2);
			if (!e2.IsAllowed)
			{
				return false;
			}
			RoleTypeId roleId = this._hub.GetRoleId();
			Vector3 position = this._hub.GetPosition();
			Vector3 velocity = this._hub.GetVelocity();
			Quaternion rotation = this._hub.PlayerCameraReference.rotation;
			PlayerStats.OnAnyPlayerDied?.Invoke(this._hub, handler);
			this.OnThisPlayerDied?.Invoke(handler);
			this.KillPlayer(handler);
			PlayerEvents.OnDeath(new PlayerDeathEventArgs(this._hub, attacker, handler, roleId, position, velocity, rotation));
		}
		return true;
	}

	private void KillPlayer(DamageHandlerBase handler)
	{
		RagdollManager.ServerSpawnRagdoll(this._hub, handler);
		this._hub.inventory.ServerDropEverything();
		this._hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.Died);
		this._hub.gameConsoleTransmission.SendToClient("You died. Reason: " + handler.ServerLogsText, "yellow");
		if (this._hub.roleManager.CurrentRole is SpectatorRole spectatorRole)
		{
			spectatorRole.ServerSetData(handler);
		}
	}

	private void OnClassChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		StatBase[] statModules = userHub.playerStats.StatModules;
		for (int i = 0; i < statModules.Length; i++)
		{
			statModules[i].ClassChanged();
		}
	}

	static PlayerStats()
	{
		PlayerStats.OnAnyPlayerDamaged = delegate
		{
		};
		PlayerStats.OnAnyPlayerDied = delegate
		{
		};
	}

	public override bool Weaved()
	{
		return true;
	}
}
