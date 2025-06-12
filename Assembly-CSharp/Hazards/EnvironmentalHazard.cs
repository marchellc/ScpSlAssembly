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

namespace Hazards;

public abstract class EnvironmentalHazard : NetworkBehaviour
{
	public static Action<EnvironmentalHazard> OnAdded;

	public static Action<EnvironmentalHazard> OnRemoved;

	public List<ReferenceHub> AffectedPlayers { get; } = new List<ReferenceHub>();

	[field: SerializeField]
	public virtual float MaxDistance { get; set; }

	[field: SerializeField]
	public virtual float MaxHeightDistance { get; set; }

	[field: SerializeField]
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
		PlayerEnteringHazardEventArgs e = new PlayerEnteringHazardEventArgs(player, this);
		PlayerEvents.OnEnteringHazard(e);
		if (!e.IsAllowed)
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
		PlayerLeavingHazardEventArgs e = new PlayerLeavingHazardEventArgs(player, this);
		PlayerEvents.OnLeavingHazard(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		this.AffectedPlayers.Remove(player);
		return true;
	}

	public virtual bool IsInArea(Vector3 sourcePos, Vector3 targetPos)
	{
		if (Mathf.Abs(targetPos.y - sourcePos.y) > this.MaxHeightDistance)
		{
			return false;
		}
		return (sourcePos - targetPos).SqrMagnitudeIgnoreY() <= this.MaxDistance * this.MaxDistance;
	}

	protected virtual void UpdateTargets()
	{
		List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!(allHub.roleManager.CurrentRole is IFpcRole fpcRole))
			{
				continue;
			}
			bool flag = this.AffectedPlayers.Contains(allHub);
			if (this.IsInArea(this.SourcePosition, fpcRole.FpcModule.Position))
			{
				if (!flag)
				{
					this.OnEnter(allHub);
				}
				else
				{
					list.Add(allHub);
				}
			}
			else if (flag)
			{
				this.OnExit(allHub);
			}
		}
		if (list.Count == 0)
		{
			ListPool<ReferenceHub>.Shared.Return(list);
			return;
		}
		PlayersStayingInHazardEventArgs e = new PlayersStayingInHazardEventArgs(list, this);
		PlayerEvents.OnStayingInHazard(e);
		list.Clear();
		foreach (Player affectedPlayer in e.AffectedPlayers)
		{
			list.Add(affectedPlayer.ReferenceHub);
		}
		ListPool<Player>.Shared.Return(e.AffectedPlayers);
		foreach (ReferenceHub item in list)
		{
			this.OnStay(item);
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
		if (NetworkServer.active)
		{
			EnvironmentalHazard.OnAdded?.Invoke(this);
		}
	}

	protected virtual void Start()
	{
		if (NetworkServer.active)
		{
			NetworkServer.Spawn(base.gameObject);
			if (!RoundStart.RoundStarted)
			{
				GameCore.Console.AddDebugLog("MAPGEN", "Spawning hazard: \"" + base.gameObject.name + "\"", MessageImportance.LessImportant, nospace: true);
			}
			PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		}
	}

	protected virtual void Update()
	{
		if (NetworkServer.active)
		{
			this.UpdateTargets();
		}
	}

	protected virtual void OnDestroy()
	{
		if (NetworkServer.active)
		{
			PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
			EnvironmentalHazard.OnRemoved?.Invoke(this);
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
