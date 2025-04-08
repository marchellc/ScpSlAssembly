using System;
using System.Collections.Generic;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace Hazards
{
	public abstract class EnvironmentalHazard : NetworkBehaviour
	{
		public List<ReferenceHub> AffectedPlayers { get; } = new List<ReferenceHub>();

		public virtual float MaxDistance { get; set; }

		public virtual float MaxHeightDistance { get; set; }

		public virtual Vector3 SourceOffset { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual Vector3 SourcePosition
		{
			get
			{
				return base.transform.position + this.SourceOffset;
			}
			set
			{
				base.transform.position = value;
			}
		}

		public virtual bool OnEnter(ReferenceHub player)
		{
			PlayerEnteringHazardEventArgs playerEnteringHazardEventArgs = new PlayerEnteringHazardEventArgs(player, this);
			PlayerEvents.OnEnteringHazard(playerEnteringHazardEventArgs);
			if (!playerEnteringHazardEventArgs.IsAllowed)
			{
				return false;
			}
			this.AffectedPlayers.Add(player);
			return true;
		}

		public virtual void OnStay(ReferenceHub player)
		{
		}

		public virtual bool OnExit(ReferenceHub player)
		{
			PlayerLeavingHazardEventArgs playerLeavingHazardEventArgs = new PlayerLeavingHazardEventArgs(player, this);
			PlayerEvents.OnLeavingHazard(playerLeavingHazardEventArgs);
			if (!playerLeavingHazardEventArgs.IsAllowed)
			{
				return false;
			}
			this.AffectedPlayers.Remove(player);
			return true;
		}

		public virtual bool IsInArea(Vector3 sourcePos, Vector3 targetPos)
		{
			return Mathf.Abs(targetPos.y - sourcePos.y) <= this.MaxHeightDistance && (sourcePos - targetPos).SqrMagnitudeIgnoreY() <= this.MaxDistance * this.MaxDistance;
		}

		protected virtual void UpdateTargets()
		{
			List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					bool flag = this.AffectedPlayers.Contains(referenceHub);
					if (this.IsInArea(this.SourcePosition, fpcRole.FpcModule.Position))
					{
						if (!flag)
						{
							this.OnEnter(referenceHub);
						}
						else
						{
							list.Add(referenceHub);
						}
					}
					else if (flag)
					{
						this.OnExit(referenceHub);
					}
				}
			}
			if (list.Count == 0)
			{
				ListPool<ReferenceHub>.Shared.Return(list);
				return;
			}
			PlayersStayingInHazardEventArgs playersStayingInHazardEventArgs = new PlayersStayingInHazardEventArgs(list, this);
			PlayerEvents.OnStayingInHazard(playersStayingInHazardEventArgs);
			list.Clear();
			foreach (Player player in playersStayingInHazardEventArgs.AffectedPlayers)
			{
				list.Add(player.ReferenceHub);
			}
			ListPool<Player>.Shared.Return(playersStayingInHazardEventArgs.AffectedPlayers);
			foreach (ReferenceHub referenceHub2 in list)
			{
				this.OnStay(referenceHub2);
			}
			ListPool<ReferenceHub>.Shared.Return(list);
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (this.AffectedPlayers.Contains(userHub))
			{
				this.OnExit(userHub);
			}
		}

		protected abstract void ClientApplyDecalSize();

		protected virtual void Awake()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Action<EnvironmentalHazard> onAdded = EnvironmentalHazard.OnAdded;
			if (onAdded == null)
			{
				return;
			}
			onAdded(this);
		}

		protected virtual void Start()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			NetworkServer.Spawn(base.gameObject, null);
			if (!RoundStart.RoundStarted)
			{
				global::GameCore.Console.AddDebugLog("MAPGEN", "Spawning hazard: \"" + base.gameObject.name + "\"", MessageImportance.LessImportant, true);
			}
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		protected virtual void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.UpdateTargets();
		}

		protected virtual void OnDestroy()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			Action<EnvironmentalHazard> onRemoved = EnvironmentalHazard.OnRemoved;
			if (onRemoved == null)
			{
				return;
			}
			onRemoved(this);
		}

		public override bool Weaved()
		{
			return true;
		}

		public static Action<EnvironmentalHazard> OnAdded;

		public static Action<EnvironmentalHazard> OnRemoved;
	}
}
