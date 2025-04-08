using System;
using Mirror;

namespace PlayerRoles.SpawnData
{
	public interface ISpawnDataReader
	{
		void ReadSpawnData(NetworkReader reader);
	}
}
