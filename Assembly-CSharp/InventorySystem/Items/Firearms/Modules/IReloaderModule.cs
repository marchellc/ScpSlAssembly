using System;

namespace InventorySystem.Items.Firearms.Modules
{
	[UniqueModule]
	public interface IReloaderModule
	{
		bool IsReloading { get; }

		bool IsUnloading { get; }

		bool IsReloadingOrUnloading
		{
			get
			{
				return this.IsReloading || this.IsUnloading;
			}
		}

		bool GetDisplayReloadingOrUnloading(ushort serial);
	}
}
