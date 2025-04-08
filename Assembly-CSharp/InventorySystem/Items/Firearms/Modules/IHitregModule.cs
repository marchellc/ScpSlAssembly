using System;
using InventorySystem.Items.Firearms.ShotEvents;

namespace InventorySystem.Items.Firearms.Modules
{
	[UniqueModule]
	public interface IHitregModule
	{
		event Action ServerOnFired;

		float DisplayDamage { get; }

		float DisplayPenetration { get; }

		void Fire(ReferenceHub primaryTarget, ShotEvent shotData);
	}
}
