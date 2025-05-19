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
			return base.transform.position + SourceOffset;
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
		AffectedPlayers.Add(player);
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
		AffectedPlayers.Remove(player);
		return true;
	}

	public virtual bool IsInArea(Vector3 sourcePos, Vector3 targetPos)
	{
		if (Mathf.Abs(targetPos.y - sourcePos.y) > MaxHeightDistance)
		{
			return false;
		}
		return (sourcePos - targetPos).SqrMagnitudeIgnoreY() <= MaxDistance * MaxDistance;
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
			bool flag = AffectedPlayers.Contains(allHub);
			if (IsInArea(SourcePosition, fpcRole.FpcModule.Position))
			{
				if (!flag)
				{
					OnEnter(allHub);
				}
				else
				{
					list.Add(allHub);
				}
			}
			else if (flag)
			{
				OnExit(allHub);
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
		foreach (Player affectedPlayer in playersStayingInHazardEventArgs.AffectedPlayers)
		{
			list.Add(affectedPlayer.ReferenceHub);
		}
		ListPool<Player>.Shared.Return(playersStayingInHazardEventArgs.AffectedPlayers);
		foreach (ReferenceHub item in list)
		{
			OnStay(item);
		}
		ListPool<ReferenceHub>.Shared.Return(list);
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (AffectedPlayers.Contains(userHub))
		{
			OnExit(userHub);
		}
	}

	protected abstract void ClientApplyDecalSize();

	protected virtual void Awake()
	{
		if (NetworkServer.active)
		{
			OnAdded?.Invoke(this);
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
			UpdateTargets();
		}
	}

	protected virtual void OnDestroy()
	{
		if (NetworkServer.active)
		{
			PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
			OnRemoved?.Invoke(this);
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
