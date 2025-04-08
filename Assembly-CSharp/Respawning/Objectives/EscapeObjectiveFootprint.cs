using System;

namespace Respawning.Objectives
{
	public class EscapeObjectiveFootprint : ObjectiveFootprintBase
	{
		protected override FootprintsTranslation TargetTranslation
		{
			get
			{
				return FootprintsTranslation.EscapeObjective;
			}
		}
	}
}
