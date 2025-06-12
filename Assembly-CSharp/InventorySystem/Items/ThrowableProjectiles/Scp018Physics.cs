using System;
using DrawableLine;
using InventorySystem.Items.Pickups;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ThrowableProjectiles;

public class Scp018Physics : PickupPhysicsModule
{
	private struct BounceData
	{
		public float VerticalSpeed;

		public RelativePosition RelPos;

		public double Time;
	}

	public const int RpcSize = 19;

	private readonly OrderedBufferQueue<BounceData> _buffer;

	private readonly Scp018Projectile _scp018;

	private readonly ParticleSystem _trail;

	private readonly LayerMask _detectionMask;

	private readonly float _radius;

	private readonly float _maxVel;

	private readonly float _velPerBounce;

	private readonly bool _wasServer;

	private const int UpdateFrequency = 10;

	private const float PrecalcTime = 0.2f;

	private Vector3 _lastVelocity;

	private RelativePosition _lastPosition;

	private Vector3? _lastSafeOrigin;

	private double _lastTime;

	private bool _outOfBounds;

	private BounceData _prevBounce;

	private BounceData _nextBounce;

	protected override ItemPickupBase Pickup => this._scp018;

	public Vector3 Position
	{
		get
		{
			double time = NetworkTime.time;
			bool flag = false;
			while (time > this._nextBounce.Time)
			{
				if (!this._buffer.TryDequeue(out var data))
				{
					return this._nextBounce.RelPos.Position;
				}
				this._prevBounce = this._nextBounce;
				this._nextBounce = data;
				flag = true;
				if (WaypointBase.TryGetWaypoint(this._prevBounce.RelPos.WaypointId, out var wp))
				{
					ParticleSystem.MainModule main = this._trail.main;
					if (wp is ElevatorWaypoint)
					{
						main.simulationSpace = ParticleSystemSimulationSpace.Custom;
						main.customSimulationSpace = wp.transform;
					}
					else
					{
						main.simulationSpace = ParticleSystemSimulationSpace.World;
					}
				}
			}
			Vector3 position = this._prevBounce.RelPos.Position;
			Vector3 position2 = this._nextBounce.RelPos.Position;
			float num = (float)(time - this._prevBounce.Time);
			float num2 = (float)(this._nextBounce.Time - this._prevBounce.Time);
			if (flag)
			{
				float velocity = Vector3.Distance(position, position2) / num2;
				this._scp018.RegisterBounce(velocity, position);
			}
			float freefallHeight = this.GetFreefallHeight(num);
			float freefallHeight2 = this.GetFreefallHeight(num2);
			float num3 = position2.y - position.y - freefallHeight2;
			float num4 = num / num2;
			position2.y = (position.y = position.y + freefallHeight + num3 * num4);
			return Vector3.Lerp(position, position2, num4);
		}
	}

	public Scp018Physics(Scp018Projectile thrownScp018, ParticleSystem trail, float radius, float maxVel, float velPerBounce)
	{
		if (!(thrownScp018.PhysicsModule is PickupStandardPhysics pickupStandardPhysics))
		{
			throw new InvalidOperationException("SCP-018's physics module can only replace PickupStandardPhysics");
		}
		this._scp018 = thrownScp018;
		this._trail = trail;
		this._radius = radius;
		this._maxVel = maxVel;
		this._velPerBounce = velPerBounce;
		this._buffer = new OrderedBufferQueue<BounceData>((BounceData x, BounceData y) => x.Time > y.Time);
		this._lastTime = NetworkTime.time;
		this._lastVelocity = pickupStandardPhysics.Rb.linearVelocity;
		this._lastPosition = new RelativePosition(this._scp018.Position);
		this._prevBounce = new BounceData
		{
			RelPos = this._lastPosition,
			Time = this._lastTime,
			VerticalSpeed = this._lastVelocity.y
		};
		pickupStandardPhysics.Rb.isKinematic = true;
		int layer = this._scp018.gameObject.layer;
		for (int num = 0; num < 32; num++)
		{
			if (!Physics.GetIgnoreLayerCollision(layer, num))
			{
				this._detectionMask = (int)this._detectionMask | (1 << num);
			}
		}
		this._nextBounce = this.PrecomputeNextBounce();
		if (NetworkServer.active)
		{
			this._wasServer = true;
			StaticUnityMethods.OnUpdate += ServerUpdatePrediction;
			this.Pickup.GetComponentsInChildren<Collider>().ForEach(delegate(Collider x)
			{
				x.isTrigger = true;
			});
		}
	}

	public override void DestroyModule()
	{
		base.DestroyModule();
		if (this._wasServer)
		{
			StaticUnityMethods.OnUpdate -= ServerUpdatePrediction;
		}
	}

	internal override void ClientProcessRpc(NetworkReader rpcData)
	{
		base.ClientProcessRpc(rpcData);
		double num = rpcData.ReadDouble();
		if (!NetworkServer.active && !(NetworkTime.time > num))
		{
			RelativePosition relPos = rpcData.ReadRelativePosition();
			float verticalSpeed = rpcData.ReadFloat();
			this._buffer.Enqueue(new BounceData
			{
				RelPos = relPos,
				Time = num,
				VerticalSpeed = verticalSpeed
			});
		}
	}

	[Server]
	private void ServerUpdatePrediction()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void InventorySystem.Items.ThrowableProjectiles.Scp018Physics::ServerUpdatePrediction()' called when server was not active");
			return;
		}
		while (!this._outOfBounds && this._lastTime - 0.20000000298023224 < NetworkTime.time)
		{
			BounceData data = this.PrecomputeNextBounce();
			this._buffer.Enqueue(data);
			base.ServerSendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteDouble(data.Time);
				writer.WriteRelativePosition(data.RelPos);
				writer.WriteFloat(data.VerticalSpeed);
			});
		}
	}

	private float GetFreefallHeight(float elapsed)
	{
		float num = Physics.gravity.y / 2f;
		float num2 = elapsed * elapsed;
		return this._prevBounce.VerticalSpeed * elapsed + num * num2;
	}

	private BounceData PrecomputeNextBounce()
	{
		while (true)
		{
			float magnitude = this._lastVelocity.magnitude;
			float maxDis = magnitude * 0.1f;
			RaycastHit hit;
			bool num = this.TrySphereCast(this._lastPosition.Position, this._lastVelocity, maxDis, out hit);
			this._lastPosition = new RelativePosition(hit.point);
			if (num)
			{
				this._lastTime += hit.distance / magnitude;
				this.BounceTrajectory(hit.normal);
				break;
			}
			this._lastTime += 0.10000000149011612;
			this._lastVelocity += Physics.gravity * 0.1f;
			if (this._lastPosition.OutOfRange)
			{
				this._outOfBounds = true;
				break;
			}
		}
		return new BounceData
		{
			RelPos = this._lastPosition,
			Time = this._lastTime,
			VerticalSpeed = this._lastVelocity.y
		};
	}

	private void BounceTrajectory(Vector3 normal)
	{
		this._lastVelocity = Vector3.Reflect(this._lastVelocity, normal);
		float magnitude = this._lastVelocity.magnitude;
		Vector3 vector = this._lastVelocity / magnitude;
		this._lastVelocity = vector * Mathf.Min(magnitude + this._velPerBounce, this._maxVel);
	}

	private bool TrySphereCast(Vector3 origin, Vector3 dir, float maxDis, out RaycastHit hit)
	{
		dir.Normalize();
		if (Physics.CheckSphere(origin, this._radius, this._detectionMask, QueryTriggerInteraction.Ignore) && this._lastSafeOrigin.HasValue)
		{
			origin = this._lastSafeOrigin.Value;
			dir = UnityEngine.Random.insideUnitSphere;
		}
		DrawableLines.GenerateSphere(origin, this._radius, Color.cyan);
		if (!Physics.SphereCast(origin, this._radius, dir, out hit, maxDis + 0.04f, this._detectionMask, QueryTriggerInteraction.Ignore))
		{
			hit.point = origin + dir * maxDis;
			return false;
		}
		hit.point += hit.normal * this._radius - dir * 0.04f;
		this._lastSafeOrigin = origin;
		return true;
	}
}
