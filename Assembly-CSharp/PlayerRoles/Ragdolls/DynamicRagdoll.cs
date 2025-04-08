using System;
using Elevators;
using UnityEngine;

namespace PlayerRoles.Ragdolls
{
	public class DynamicRagdoll : BasicRagdoll
	{
		public override void FreezeRagdoll()
		{
			base.FreezeRagdoll();
			this.LinkedRigidbodies.ForEach(delegate(Rigidbody rg)
			{
				Joint joint;
				if (rg.TryGetComponent<Joint>(out joint))
				{
					global::UnityEngine.Object.Destroy(joint);
				}
				RigidBodyElevatorFollower rigidBodyElevatorFollower;
				if (rg.TryGetComponent<RigidBodyElevatorFollower>(out rigidBodyElevatorFollower))
				{
					rigidBodyElevatorFollower.Unlink();
				}
				global::UnityEngine.Object.Destroy(rg);
			});
			this.Hitboxes = DynamicRagdoll.EmptyHitboxes;
			this.LinkedRigidbodies = DynamicRagdoll.EmptyRigidbodies;
		}

		public override bool Weaved()
		{
			return true;
		}

		public Rigidbody[] LinkedRigidbodies;

		public Transform[] LinkedRigidbodiesTransforms;

		public HitboxData[] Hitboxes;

		private static readonly HitboxData[] EmptyHitboxes = new HitboxData[0];

		private static readonly Rigidbody[] EmptyRigidbodies = new Rigidbody[0];
	}
}
