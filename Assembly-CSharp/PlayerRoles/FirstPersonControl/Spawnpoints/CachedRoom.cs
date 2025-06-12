namespace PlayerRoles.FirstPersonControl.Spawnpoints;

public struct CachedRoom
{
	public RoomRoleSpawnpoint RoomType;

	public int RoomIndex;

	public CachedRoom(RoomRoleSpawnpoint roomType, int roomIndex)
	{
		this.RoomType = roomType;
		this.RoomIndex = roomIndex;
	}
}
