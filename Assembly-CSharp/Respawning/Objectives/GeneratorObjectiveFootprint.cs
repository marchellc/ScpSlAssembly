using System;

namespace Respawning.Objectives
{
	public class GeneratorObjectiveFootprint : ObjectiveFootprintBase
	{
		protected override FootprintsTranslation TargetTranslation
		{
			get
			{
				return FootprintsTranslation.GeneratorObjective;
			}
		}
	}
}
