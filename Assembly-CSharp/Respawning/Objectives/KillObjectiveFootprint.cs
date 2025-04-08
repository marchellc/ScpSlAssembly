using System;

namespace Respawning.Objectives
{
	public class KillObjectiveFootprint : AttackerObjectiveFootprint
	{
		protected override FootprintsTranslation TargetTranslation
		{
			get
			{
				return FootprintsTranslation.KillObjective;
			}
		}
	}
}
