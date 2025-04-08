using System;
using Mirror;

namespace UserSettings.ServerSpecific
{
	public interface ISSUpdatable
	{
		void DeserializeUpdate(NetworkReader reader);
	}
}
