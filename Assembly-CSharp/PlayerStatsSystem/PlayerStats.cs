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

namespace PlayerStatsSystem
{
	public class PlayerStats : NetworkBehaviour
	{
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

		public event Action<DamageHandlerBase> OnThisPlayerDamaged = delegate(DamageHandlerBase usedHandler)
		{
		};

		public event Action<DamageHandlerBase> OnThisPlayerDied = delegate(DamageHandlerBase usedHandler)
		{
		};

		public static event Action<ReferenceHub, DamageHandlerBase> OnAnyPlayerDamaged;

		public static event Action<ReferenceHub, DamageHandlerBase> OnAnyPlayerDied;

		private void Awake()
		{
			foreach (StatBase statBase in this.StatModules)
			{
				this._dictionarizedTypes.Add(statBase.GetType(), statBase);
			}
			this._hub = ReferenceHub.GetHub(base.gameObject);
			StatBase[] array = this.StatModules;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Init(this._hub);
			}
		}

		private void Start()
		{
			if (!this._hub.isLocalPlayer)
			{
				return;
			}
			PlayerRoleManager.OnRoleChanged += this.OnClassChanged;
			this._eventAssigned = true;
		}

		private void OnDestroy()
		{
			if (!this._eventAssigned)
			{
				return;
			}
			PlayerRoleManager.OnRoleChanged -= this.OnClassChanged;
			this._eventAssigned = false;
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
			StatBase statBase;
			if (this._dictionarizedTypes.TryGetValue(typeof(T), out statBase))
			{
				T t = statBase as T;
				if (t != null)
				{
					module = t;
					return true;
				}
			}
			module = default(T);
			return false;
		}

		public bool DealDamage(DamageHandlerBase handler)
		{
			if (this._hub.characterClassManager.GodMode)
			{
				return false;
			}
			SpawnProtected spawnProtected;
			if (this._hub.playerEffectsController.TryGetEffect<SpawnProtected>(out spawnProtected) && spawnProtected.IsEnabled)
			{
				return false;
			}
			IDamageHandlerProcessingRole damageHandlerProcessingRole = this._hub.roleManager.CurrentRole as IDamageHandlerProcessingRole;
			if (damageHandlerProcessingRole != null)
			{
				handler = damageHandlerProcessingRole.ProcessDamageHandler(handler);
			}
			ReferenceHub referenceHub = null;
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler != null)
			{
				referenceHub = attackerDamageHandler.Attacker.Hub;
			}
			PlayerHurtingEventArgs playerHurtingEventArgs = new PlayerHurtingEventArgs(referenceHub, this._hub, handler);
			PlayerEvents.OnHurting(playerHurtingEventArgs);
			if (!playerHurtingEventArgs.IsAllowed)
			{
				return false;
			}
			DamageHandlerBase.HandlerOutput handlerOutput = handler.ApplyDamage(this._hub);
			PlayerEvents.OnHurt(new PlayerHurtEventArgs(referenceHub, this._hub, handler));
			if (handlerOutput == DamageHandlerBase.HandlerOutput.Nothing)
			{
				return false;
			}
			Action<ReferenceHub, DamageHandlerBase> onAnyPlayerDamaged = PlayerStats.OnAnyPlayerDamaged;
			if (onAnyPlayerDamaged != null)
			{
				onAnyPlayerDamaged(this._hub, handler);
			}
			Action<DamageHandlerBase> onThisPlayerDamaged = this.OnThisPlayerDamaged;
			if (onThisPlayerDamaged != null)
			{
				onThisPlayerDamaged(handler);
			}
			if (handlerOutput == DamageHandlerBase.HandlerOutput.Death)
			{
				PlayerDyingEventArgs playerDyingEventArgs = new PlayerDyingEventArgs(this._hub, referenceHub, handler);
				PlayerEvents.OnDying(playerDyingEventArgs);
				if (!playerDyingEventArgs.IsAllowed)
				{
					return false;
				}
				Action<ReferenceHub, DamageHandlerBase> onAnyPlayerDied = PlayerStats.OnAnyPlayerDied;
				if (onAnyPlayerDied != null)
				{
					onAnyPlayerDied(this._hub, handler);
				}
				Action<DamageHandlerBase> onThisPlayerDied = this.OnThisPlayerDied;
				if (onThisPlayerDied != null)
				{
					onThisPlayerDied(handler);
				}
				this.KillPlayer(handler);
				PlayerEvents.OnDeath(new PlayerDeathEventArgs(this._hub, referenceHub, handler));
			}
			return true;
		}

		private void KillPlayer(DamageHandlerBase handler)
		{
			RagdollManager.ServerSpawnRagdoll(this._hub, handler);
			this._hub.inventory.ServerDropEverything();
			this._hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.Died, RoleSpawnFlags.All);
			this._hub.gameConsoleTransmission.SendToClient("You died. Reason: " + handler.ServerLogsText, "yellow");
			SpectatorRole spectatorRole = this._hub.roleManager.CurrentRole as SpectatorRole;
			if (spectatorRole != null)
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

		// Note: this type is marked as 'beforefieldinit'.
		static PlayerStats()
		{
			PlayerStats.OnAnyPlayerDamaged = delegate(ReferenceHub victimPlayer, DamageHandlerBase usedHandler)
			{
			};
			PlayerStats.OnAnyPlayerDied = delegate(ReferenceHub victimPlayer, DamageHandlerBase usedHandler)
			{
			};
		}

		public override bool Weaved()
		{
			return true;
		}

		public static readonly Type[] DefinedModules = new Type[]
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
	}
}
