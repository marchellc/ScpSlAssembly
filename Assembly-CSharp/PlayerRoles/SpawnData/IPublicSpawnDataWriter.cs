using System;
using Mirror;

namespace PlayerRoles.SpawnData
{
	public interface IPublicSpawnDataWriter
	{
		void WritePublicSpawnData(NetworkWriter writer);
	}
}
