using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public class SingleTrajectoryProjectile : ThrownProjectile
{
	[SerializeField]
	private LayerMask _collisionMask;

	[SerializeField]
	private GameObject _destroyedObject;

	[SerializeField]
	private float _destroyTime;

	[field: SerializeField]
	public float ProjectileRadius { get; private set; }

	protected override PickupPhysicsModule DefaultPhysicsModule => new TrajectoryPhysics(this);

	protected TrajectoryPhysics TrajPhysics => base.PhysicsModule as TrajectoryPhysics;

	protected virtual bool AlreadyCollided => this.TrajPhysics.DestinationReached;

	protected override void Awake()
	{
		base.Awake();
		this.TrajPhysics.ServerSetup(base.Position, Vector3.zero, this.ProjectileRadius, this._collisionMask);
	}

	public override void ServerOnThrown(Vector3 torque, Vector3 velocity)
	{
		base.ServerOnThrown(torque, velocity);
		this.TrajPhysics.ServerSetup(base.Position, velocity, this.ProjectileRadius, this._collisionMask);
	}

	protected virtual void Update()
	{
		if (!this.AlreadyCollided)
		{
			return;
		}
		if (!this._destroyedObject.activeSelf)
		{
			this._destroyedObject.SetActive(value: true);
			this.ToggleRenderers(state: false);
			Vector3 fragmentVel = Vector3.ClampMagnitude(this.TrajPhysics.LastVelocity, 3f);
			this._destroyedObject.ForEachComponentInChildren(delegate(Rigidbody rb)
			{
				rb.linearVelocity = fragmentVel;
			}, includeInactive: false);
		}
		if (NetworkServer.active)
		{
			this._destroyTime -= Time.deltaTime;
			if (!(this._destroyTime > 0f))
			{
				NetworkServer.Destroy(base.gameObject);
			}
		}
	}

	public override void ToggleRenderers(bool state)
	{
		if (this._destroyedObject.activeSelf)
		{
			state = false;
		}
		base.ToggleRenderers(state);
	}

	public override bool Weaved()
	{
		return true;
	}
}
