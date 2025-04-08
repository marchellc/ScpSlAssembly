using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public interface IInaccuracyProviderModule
	{
		float Inaccuracy { get; }
	}
}
