using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class PrimaryFireModeModule : FiringModeControllerModule
	{
		public override MicroHidFiringMode AssignedMode
		{
			get
			{
				return MicroHidFiringMode.PrimaryFire;
			}
		}

		public override float WindUpRate
		{
			get
			{
				return 0.33333334f;
			}
		}

		public override float WindDownRate
		{
			get
			{
				return 1f;
			}
		}

		public override float DrainRateWindUp
		{
			get
			{
				return 0f;
			}
		}

		public override float DrainRateSustain
		{
			get
			{
				return 0f;
			}
		}

		public override float DrainRateFiring
		{
			get
			{
				return 0.05f;
			}
		}

		public override bool ValidateStart
		{
			get
			{
				return base.InputSync.Primary && !base.Broken;
			}
		}

		public override bool ValidateEnterFire
		{
			get
			{
				return base.InputSync.Primary;
			}
		}

		public override bool ValidateUpdate
		{
			get
			{
				return base.InputSync.Primary && base.Energy > 0f;
			}
		}

		public override float FiringRange
		{
			get
			{
				return 6f;
			}
		}

		public override float BacktrackerDot
		{
			get
			{
				return 0.9f;
			}
		}

		private float FrameDamage
		{
			get
			{
				return 400f * Time.deltaTime;
			}
		}

		public override void ServerUpdateSelected(MicroHidPhase status)
		{
			base.ServerUpdateSelected(status);
			if (status != MicroHidPhase.Firing)
			{
				return;
			}
			base.ServerRequestBacktrack(new Action(this.ServerFire));
		}

		private void ServerFire()
		{
			ReferenceHub owner = base.Item.Owner;
			int num;
			HitregUtils.Raycast(owner.PlayerCameraReference, 0.2f, this.FiringRange, out num);
			HitboxIdentity hitboxIdentity = null;
			float num2 = float.MaxValue;
			foreach (IDestructible destructible in HitregUtils.DetectedDestructibles)
			{
				HitboxIdentity hitboxIdentity2 = destructible as HitboxIdentity;
				if (hitboxIdentity2 == null)
				{
					destructible.ServerDealDamage(this, this.FrameDamage);
				}
				else if (!(hitboxIdentity2.TargetHub == owner))
				{
					IFpcRole fpcRole = hitboxIdentity2.TargetHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						float num3 = fpcRole.SqrDistanceTo(owner);
						if (num3 <= num2)
						{
							hitboxIdentity = hitboxIdentity2;
							num2 = num3;
						}
					}
				}
			}
			if (hitboxIdentity != null)
			{
				hitboxIdentity.ServerDealDamage(this, this.FrameDamage);
			}
		}

		private const float RaycastThickness = 0.2f;

		private const float DamagePerSec = 400f;
	}
}
