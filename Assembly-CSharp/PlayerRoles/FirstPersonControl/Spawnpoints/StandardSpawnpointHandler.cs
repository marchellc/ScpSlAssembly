using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public class StandardSpawnpointHandler : MonoBehaviour, ISpawnpointHandler
	{
		public PlayerRoleBase Role { get; private set; }

		public virtual bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
		{
			position = default(Vector3);
			horizontalRot = 0f;
			RoomRoleSpawnpoint[] validSpawnpoints = this.GetValidSpawnpoints(this.Spawnpoints);
			return validSpawnpoints.Length != 0 && validSpawnpoints.RandomItem<RoomRoleSpawnpoint>().TryGetSpawnpoint(out position, out horizontalRot);
		}

		protected RoomRoleSpawnpoint[] GetValidSpawnpoints(RoomRoleSpawnpoint[] allSpawnpoints)
		{
			return allSpawnpoints.Where((RoomRoleSpawnpoint x) => x.GetRoomAmount() > 0).ToArray<RoomRoleSpawnpoint>();
		}

		private void OnValidate()
		{
			if (this.Role != null)
			{
				return;
			}
			this.Role = base.GetComponent<PlayerRoleBase>();
		}

		[SerializeField]
		[FormerlySerializedAs("_spawnpoints")]
		protected RoomRoleSpawnpoint[] Spawnpoints;
	}
}
