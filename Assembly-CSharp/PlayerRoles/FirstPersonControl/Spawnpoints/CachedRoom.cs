using System;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public struct CachedRoom
	{
		public CachedRoom(RoomRoleSpawnpoint roomType, int roomIndex)
		{
			this.RoomType = roomType;
			this.RoomIndex = roomIndex;
		}

		public RoomRoleSpawnpoint RoomType;

		public int RoomIndex;
	}
}
