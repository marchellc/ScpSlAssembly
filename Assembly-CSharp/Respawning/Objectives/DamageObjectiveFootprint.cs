using System;

namespace Respawning.Objectives
{
	public class DamageObjectiveFootprint : AttackerObjectiveFootprint
	{
		protected override FootprintsTranslation TargetTranslation
		{
			get
			{
				return FootprintsTranslation.DamageObjective;
			}
		}
	}
}
