using System;
using PlayerRoles;
using PlayerRoles.PlayableScps;

namespace CustomPlayerEffects.Danger
{
	public class ZombieEncounterDanger : EncounterDangerBase
	{
		public override bool IsActive
		{
			get
			{
				this.UpdateState();
				return base.IsActive;
			}
			protected set
			{
				base.IsActive = value;
			}
		}

		public override float DangerPerEncounter { get; } = 0.25f;

		public override float DangerPerAdditionalEncounter { get; } = 0.25f;

		private void UpdateState()
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (!(referenceHub == base.Owner) && referenceHub.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp0492 && VisionInformation.IsInView(base.Owner, referenceHub))
				{
					CachedEncounterDanger cachedEncounterDanger;
					if (base.WasEncounteredRecently(referenceHub, out cachedEncounterDanger))
					{
						cachedEncounterDanger.TimeTracker.Restart();
					}
					else
					{
						base.RegisterEncounter(referenceHub);
					}
				}
			}
		}
	}
}
