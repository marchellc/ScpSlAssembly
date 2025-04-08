using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class BrokenFireModeModule : FiringModeControllerModule
	{
		public override MicroHidFiringMode AssignedMode
		{
			get
			{
				return MicroHidFiringMode.BrokenFire;
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
				return base.InputSync.Primary && base.Broken;
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
				return 4f;
			}
		}

		public override float BacktrackerDot
		{
			get
			{
				return 0.8f;
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
			IFpcRole fpcRole = base.Item.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			int num;
			HitregUtils.OverlapSphere(fpcRole.FpcModule.Position, this.FiringRange, out num, new Predicate<IDestructible>(this.CheckAngle));
			foreach (IDestructible destructible in HitregUtils.DetectedDestructibles)
			{
				destructible.ServerDealDamage(this, 400f * Time.deltaTime);
			}
		}

		private bool CheckAngle(IDestructible dest)
		{
			Transform playerCameraReference = base.Item.Owner.PlayerCameraReference;
			Vector3 vector = dest.CenterOfMass - playerCameraReference.position;
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude < 1f)
			{
				return true;
			}
			Vector3 forward = playerCameraReference.forward;
			return Vector3.Dot(vector / Mathf.Sqrt(sqrMagnitude), forward) >= BrokenFireModeModule.FiringConeDot;
		}

		private const float FiringConeAngleDeg = 30f;

		private const float FiringAlwaysIncludeDistSqr = 1f;

		private const float DamagePerSecond = 400f;

		private static readonly float FiringConeDot = Mathf.Cos(0.5235988f);
	}
}
