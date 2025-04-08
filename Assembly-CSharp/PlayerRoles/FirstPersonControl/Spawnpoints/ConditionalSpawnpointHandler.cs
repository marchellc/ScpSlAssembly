using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public class ConditionalSpawnpointHandler : StandardSpawnpointHandler
	{
		private void Awake()
		{
			if (base.Role == null)
			{
				return;
			}
			RoleTypeId roleTypeId = base.Role.RoleTypeId;
			if (ConditionalSpawnpointHandler.RoleSpawnpoints.ContainsKey(roleTypeId))
			{
				return;
			}
			Dictionary<RoleChangeReason, RoomRoleSpawnpoint[]> dictionary = new Dictionary<RoleChangeReason, RoomRoleSpawnpoint[]>();
			foreach (ConditionalSpawnpointHandler.SerializedSpawnpoint serializedSpawnpoint in this._conditionalSpawnpoints)
			{
				dictionary.Add(serializedSpawnpoint.Condition, serializedSpawnpoint.Spawnpoints);
			}
			ConditionalSpawnpointHandler.RoleSpawnpoints[roleTypeId] = dictionary;
		}

		public override bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
		{
			return this.TryGetConditionalSpawnpoint(out position, out horizontalRot) || base.TryGetSpawnpoint(out position, out horizontalRot);
		}

		public bool TryGetConditionalSpawnpoint(out Vector3 position, out float horizontalRot)
		{
			position = default(Vector3);
			horizontalRot = 0f;
			if (this._conditionalSpawnpoints.Length == 0)
			{
				return false;
			}
			Dictionary<RoleChangeReason, RoomRoleSpawnpoint[]> dictionary;
			if (!ConditionalSpawnpointHandler.RoleSpawnpoints.TryGetValue(base.Role.RoleTypeId, out dictionary))
			{
				return false;
			}
			RoleChangeReason serverSpawnReason = base.Role.ServerSpawnReason;
			RoomRoleSpawnpoint[] validSpawnpoints;
			if (!dictionary.TryGetValue(serverSpawnReason, out validSpawnpoints))
			{
				return false;
			}
			validSpawnpoints = base.GetValidSpawnpoints(validSpawnpoints);
			return validSpawnpoints.RandomItem<RoomRoleSpawnpoint>().TryGetSpawnpoint(out position, out horizontalRot);
		}

		private static readonly Dictionary<RoleTypeId, Dictionary<RoleChangeReason, RoomRoleSpawnpoint[]>> RoleSpawnpoints = new Dictionary<RoleTypeId, Dictionary<RoleChangeReason, RoomRoleSpawnpoint[]>>();

		[SerializeField]
		private ConditionalSpawnpointHandler.SerializedSpawnpoint[] _conditionalSpawnpoints;

		[Serializable]
		private struct SerializedSpawnpoint
		{
			public RoleChangeReason Condition;

			public RoomRoleSpawnpoint[] Spawnpoints;
		}
	}
}
