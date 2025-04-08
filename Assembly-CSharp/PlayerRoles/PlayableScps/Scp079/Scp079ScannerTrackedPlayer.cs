using System;
using MapGeneration;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ScannerTrackedPlayer
	{
		public bool IsCamping { get; private set; }

		public FacilityZone LastZone { get; private set; }

		public Vector3 PlyPos
		{
			get
			{
				return this._role.FpcModule.Position;
			}
		}

		public Scp079ScannerTrackedPlayer(ReferenceHub hub)
		{
			this.Hub = hub;
			this.PlayerHash = hub.GetHashCode();
			HumanRole humanRole = hub.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				throw new ArgumentOutOfRangeException("Cannot track non-human roles!");
			}
			this._role = humanRole;
			this.ResetPosition();
		}

		public void Update(float baselineRadius, float additiveRadius, float maxCampingTime)
		{
			float sqrMagnitude = (this.PlyPos - this._centerPos.Position).sqrMagnitude;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(this.PlyPos);
			if (roomIdentifier != null)
			{
				this.LastZone = roomIdentifier.Zone;
			}
			int num = ReferenceHub.AllHubs.Count(new Func<ReferenceHub, bool>(this.CheckEnemy));
			float num2 = baselineRadius + additiveRadius / (float)Mathf.Max(num, 1);
			if (sqrMagnitude > num2 * num2)
			{
				this.ResetPosition();
			}
			this.IsCamping = NetworkTime.time - this._resetTime > (double)maxCampingTime;
		}

		private bool CheckEnemy(ReferenceHub hub)
		{
			HumanRole humanRole = hub.roleManager.CurrentRole as HumanRole;
			return humanRole != null && humanRole.Team.GetFaction() != this._role.Team.GetFaction() && (humanRole.FpcModule.Position - this.PlyPos).sqrMagnitude < 22500f;
		}

		private void ResetPosition()
		{
			this.IsCamping = false;
			this._resetTime = NetworkTime.time;
			this._centerPos = new RelativePosition(this.PlyPos);
		}

		public readonly int PlayerHash;

		public readonly ReferenceHub Hub;

		private readonly HumanRole _role;

		private double _resetTime;

		private RelativePosition _centerPos;

		private const float EnemyProxSqr = 22500f;
	}
}
