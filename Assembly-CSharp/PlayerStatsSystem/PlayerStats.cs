using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;

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
			if (_statModules != null)
			{
				return _statModules;
			}
			_statModules = new StatBase[DefinedModules.Length];
			for (int i = 0; i < DefinedModules.Length; i++)
			{
				object obj = Activator.CreateInstance(DefinedModules[i]);
				_statModules[i] = obj as StatBase;
			}
			return _statModules;
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
		StatBase[] statModules = StatModules;
		foreach (StatBase statBase in statModules)
		{
			_dictionarizedTypes.Add(statBase.GetType(), statBase);
		}
		_hub = ReferenceHub.GetHub(base.gameObject);
		statModules = StatModules;
		for (int i = 0; i < statModules.Length; i++)
		{
			statModules[i].Init(_hub);
		}
	}

	private void Start()
	{
		if (_hub.isLocalPlayer)
		{
			PlayerRoleManager.OnRoleChanged += OnClassChanged;
			_eventAssigned = true;
		}
	}

	private void OnDestroy()
	{
		if (_eventAssigned)
		{
			PlayerRoleManager.OnRoleChanged -= OnClassChanged;
			_eventAssigned = false;
		}
	}

	private void Update()
	{
		StatBase[] statModules = StatModules;
		for (int i = 0; i < statModules.Length; i++)
		{
			statModules[i].Update();
		}
	}

	public T GetModule<T>() where T : StatBase
	{
		return _dictionarizedTypes[typeof(T)] as T;
	}

	public bool TryGetModule<T>(out T module) where T : StatBase
	{
		if (_dictionarizedTypes.TryGetValue(typeof(T), out var value) && value is T val)
		{
			module = val;
			return true;
		}
		module = null;
		return false;
	}

	public bool DealDamage(DamageHandlerBase handler)
	{
		if (_hub.characterClassManager.GodMode)
		{
			return false;
		}
		if (_hub.playerEffectsController.TryGetEffect<SpawnProtected>(out var playerEffect) && playerEffect.IsEnabled)
		{
			return false;
		}
		if (_hub.roleManager.CurrentRole is IDamageHandlerProcessingRole damageHandlerProcessingRole)
		{
			handler = damageHandlerProcessingRole.ProcessDamageHandler(handler);
		}
		ReferenceHub attacker = null;
		if (handler is AttackerDamageHandler attackerDamageHandler)
		{
			attacker = attackerDamageHandler.Attacker.Hub;
		}
		PlayerHurtingEventArgs playerHurtingEventArgs = new PlayerHurtingEventArgs(attacker, _hub, handler);
		PlayerEvents.OnHurting(playerHurtingEventArgs);
		if (!playerHurtingEventArgs.IsAllowed)
		{
			return false;
		}
		DamageHandlerBase.HandlerOutput handlerOutput = handler.ApplyDamage(_hub);
		PlayerEvents.OnHurt(new PlayerHurtEventArgs(attacker, _hub, handler));
		if (handlerOutput == DamageHandlerBase.HandlerOutput.Nothing)
		{
			return false;
		}
		PlayerStats.OnAnyPlayerDamaged?.Invoke(_hub, handler);
		this.OnThisPlayerDamaged?.Invoke(handler);
		if (handlerOutput == DamageHandlerBase.HandlerOutput.Death)
		{
			PlayerDyingEventArgs playerDyingEventArgs = new PlayerDyingEventArgs(_hub, attacker, handler);
			PlayerEvents.OnDying(playerDyingEventArgs);
			if (!playerDyingEventArgs.IsAllowed)
			{
				return false;
			}
			PlayerStats.OnAnyPlayerDied?.Invoke(_hub, handler);
			this.OnThisPlayerDied?.Invoke(handler);
			KillPlayer(handler);
			PlayerEvents.OnDeath(new PlayerDeathEventArgs(_hub, attacker, handler));
		}
		return true;
	}

	private void KillPlayer(DamageHandlerBase handler)
	{
		RagdollManager.ServerSpawnRagdoll(_hub, handler);
		_hub.inventory.ServerDropEverything();
		_hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.Died);
		_hub.gameConsoleTransmission.SendToClient("You died. Reason: " + handler.ServerLogsText, "yellow");
		if (_hub.roleManager.CurrentRole is SpectatorRole spectatorRole)
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
