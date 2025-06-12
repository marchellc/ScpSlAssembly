using Elevators;
using UnityEngine;

namespace PlayerRoles.Ragdolls;

public class DynamicRagdoll : BasicRagdoll
{
	public Rigidbody[] LinkedRigidbodies;

	public Transform[] LinkedRigidbodiesTransforms;

	public HitboxData[] Hitboxes;

	private static readonly HitboxData[] EmptyHitboxes = new HitboxData[0];

	private static readonly Rigidbody[] EmptyRigidbodies = new Rigidbody[0];

	public override void FreezeRagdoll()
	{
		base.FreezeRagdoll();
		this.LinkedRigidbodies.ForEach(delegate(Rigidbody rg)
		{
			if (rg.TryGetComponent<Joint>(out var component))
			{
				Object.Destroy(component);
			}
			if (rg.TryGetComponent<RigidBodyElevatorFollower>(out var component2))
			{
				component2.Unlink();
			}
			Object.Destroy(rg);
		});
		this.Hitboxes = DynamicRagdoll.EmptyHitboxes;
		this.LinkedRigidbodies = DynamicRagdoll.EmptyRigidbodies;
	}

	public override bool Weaved()
	{
		return true;
	}
}
