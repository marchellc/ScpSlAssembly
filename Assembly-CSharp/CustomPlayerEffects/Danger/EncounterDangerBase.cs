using System;

namespace CustomPlayerEffects.Danger
{
	public abstract class EncounterDangerBase : ParentDangerBase
	{
		public abstract float DangerPerEncounter { get; }

		public abstract float DangerPerAdditionalEncounter { get; }

		public void RegisterEncounter(ReferenceHub target)
		{
			float num = (base.ChildDangers.IsEmpty<DangerStackBase>() ? this.DangerPerEncounter : this.DangerPerAdditionalEncounter);
			base.ChildDangers.Add(new CachedEncounterDanger(num, base.Owner, target));
		}

		public bool WasEncounteredRecently(ReferenceHub hub, out CachedEncounterDanger cachedEncounter)
		{
			cachedEncounter = null;
			foreach (DangerStackBase dangerStackBase in base.ChildDangers)
			{
				CachedEncounterDanger cachedEncounterDanger = (CachedEncounterDanger)dangerStackBase;
				if (!(cachedEncounterDanger.EncounteredHub != hub))
				{
					cachedEncounter = cachedEncounterDanger;
					return true;
				}
			}
			return false;
		}
	}
}
