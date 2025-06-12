using System;
using MapGeneration;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079ScannerTrackedPlayer
{
	public readonly int PlayerHash;

	public readonly ReferenceHub Hub;

	private readonly HumanRole _role;

	private double _resetTime;

	private RelativePosition _centerPos;

	private const float EnemyProxSqr = 22500f;

	public bool IsCamping { get; private set; }

	public FacilityZone LastZone { get; private set; }

	public Vector3 PlyPos => this._role.FpcModule.Position;

	public Scp079ScannerTrackedPlayer(ReferenceHub hub)
	{
		this.Hub = hub;
		this.PlayerHash = hub.GetHashCode();
		if (!(hub.roleManager.CurrentRole is HumanRole role))
		{
			throw new ArgumentOutOfRangeException("Cannot track non-human roles!");
		}
		this._role = role;
		this.ResetPosition();
	}

	public void Update(float baselineRadius, float additiveRadius, float maxCampingTime)
	{
		float sqrMagnitude = (this.PlyPos - this._centerPos.Position).sqrMagnitude;
		if (this.PlyPos.TryGetRoom(out var room))
		{
			this.LastZone = room.Zone;
		}
		int a = ReferenceHub.AllHubs.Count(CheckEnemy);
		float num = baselineRadius + additiveRadius / (float)Mathf.Max(a, 1);
		if (sqrMagnitude > num * num)
		{
			this.ResetPosition();
		}
		this.IsCamping = NetworkTime.time - this._resetTime > (double)maxCampingTime;
	}

	private bool CheckEnemy(ReferenceHub hub)
	{
		if (hub.roleManager.CurrentRole is HumanRole humanRole && humanRole.Team.GetFaction() != this._role.Team.GetFaction())
		{
			return (humanRole.FpcModule.Position - this.PlyPos).sqrMagnitude < 22500f;
		}
		return false;
	}

	private void ResetPosition()
	{
		this.IsCamping = false;
		this._resetTime = NetworkTime.time;
		this._centerPos = new RelativePosition(this.PlyPos);
	}
}
