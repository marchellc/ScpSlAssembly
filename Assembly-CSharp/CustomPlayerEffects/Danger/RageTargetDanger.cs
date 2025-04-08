using System;
using PlayerRoles.PlayableScps.Scp096;

namespace CustomPlayerEffects.Danger
{
	public class RageTargetDanger : DangerStackBase
	{
		public override float DangerValue { get; set; } = 2f;

		public override void Initialize(ReferenceHub target)
		{
			base.Initialize(target);
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp096Role scp096Role = referenceHub.roleManager.CurrentRole as Scp096Role;
				Scp096TargetsTracker scp096TargetsTracker;
				if (scp096Role != null && scp096Role.SubroutineModule.TryGetSubroutine<Scp096TargetsTracker>(out scp096TargetsTracker) && scp096TargetsTracker.Targets.Contains(base.Owner))
				{
					this.IsActive = true;
				}
			}
			Scp096TargetsTracker.OnTargetAdded += this.OnTargetAdded;
			Scp096TargetsTracker.OnTargetRemoved += this.OnTargetRemoved;
		}

		public override void Dispose()
		{
			base.Dispose();
			Scp096TargetsTracker.OnTargetAdded -= this.OnTargetAdded;
			Scp096TargetsTracker.OnTargetRemoved -= this.OnTargetRemoved;
		}

		private void OnTargetAdded(ReferenceHub owner, ReferenceHub target)
		{
			this.UpdateState(true, target);
		}

		private void OnTargetRemoved(ReferenceHub owner, ReferenceHub target)
		{
			this.UpdateState(false, target);
		}

		private void UpdateState(bool targetAdded, ReferenceHub targetedHub)
		{
			if (targetedHub == null)
			{
				return;
			}
			if (targetedHub != base.Owner)
			{
				return;
			}
			this.IsActive = targetAdded;
		}
	}
}
