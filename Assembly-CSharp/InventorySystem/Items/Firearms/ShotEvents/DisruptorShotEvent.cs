using Footprinting;
using InventorySystem.Items.Firearms.Modules;

namespace InventorySystem.Items.Firearms.ShotEvents;

public class DisruptorShotEvent : ShotEvent
{
	public readonly DisruptorActionModule.FiringState State;

	public readonly Footprint HitregFootprint;

	public DisruptorShotEvent(ItemIdentifier shotFirearm, Footprint shooter, DisruptorActionModule.FiringState state)
		: base(shotFirearm)
	{
		this.State = state;
		this.HitregFootprint = shooter;
	}

	public DisruptorShotEvent(Firearm firearm, DisruptorActionModule.FiringState state)
		: base(new ItemIdentifier(firearm))
	{
		this.State = state;
		this.HitregFootprint = new Footprint(firearm.Owner);
	}
}
