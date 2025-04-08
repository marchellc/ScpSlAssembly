using System;
using Mirror;

namespace PlayerRoles.SpawnData
{
	public interface IPrivateSpawnDataWriter
	{
		void WritePrivateSpawnData(NetworkWriter writer);
	}
}
