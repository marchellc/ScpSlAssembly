using System;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Jailbird;
using PlayerRoles;
using PlayerRoles.PlayableScps;

namespace CustomPlayerEffects.Danger
{
	public class ArmedEnemyDanger : EncounterDangerBase
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

		public override float DangerPerEncounter { get; } = 1f;

		public override float DangerPerAdditionalEncounter { get; } = 0.25f;

		private void UpdateState()
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (!(referenceHub == base.Owner) && referenceHub.IsHuman())
				{
					if (!HitboxIdentity.IsEnemy(base.Owner, referenceHub))
					{
						break;
					}
					if (VisionInformation.IsInView(base.Owner, referenceHub) && this.IsArmed(referenceHub))
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

		private bool IsArmed(ReferenceHub hub)
		{
			foreach (ItemBase itemBase in hub.inventory.UserInventory.Items.Values)
			{
				if (itemBase is Firearm || itemBase is JailbirdItem)
				{
					return true;
				}
			}
			return false;
		}
	}
}
