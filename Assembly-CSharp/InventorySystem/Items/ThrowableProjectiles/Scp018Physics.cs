using System;
using InventorySystem.Items.Pickups;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class Scp018Physics : PickupPhysicsModule
	{
		protected override ItemPickupBase Pickup
		{
			get
			{
				return this._scp018;
			}
		}

		public Vector3 Position
		{
			get
			{
				double time = NetworkTime.time;
				bool flag = false;
				while (time > this._nextBounce.Time)
				{
					Scp018Physics.BounceData bounceData;
					if (!this._buffer.TryDequeue(out bounceData))
					{
						return this._nextBounce.RelPos.Position;
					}
					this._prevBounce = this._nextBounce;
					this._nextBounce = bounceData;
					flag = true;
					WaypointBase waypointBase;
					if (WaypointBase.TryGetWaypoint(this._prevBounce.RelPos.WaypointId, out waypointBase))
					{
						ParticleSystem.MainModule main = this._trail.main;
						if (waypointBase is ElevatorWaypoint)
						{
							main.simulationSpace = ParticleSystemSimulationSpace.Custom;
							main.customSimulationSpace = waypointBase.transform;
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
					float num3 = Vector3.Distance(position, position2) / num2;
					this._scp018.RegisterBounce(num3, position);
				}
				float freefallHeight = this.GetFreefallHeight(num);
				float freefallHeight2 = this.GetFreefallHeight(num2);
				float num4 = position2.y - position.y - freefallHeight2;
				float num5 = num / num2;
				float num6 = position.y + freefallHeight + num4 * num5;
				position.y = num6;
				position2.y = num6;
				return Vector3.Lerp(position, position2, num5);
			}
		}

		public Scp018Physics(Scp018Projectile thrownScp018, ParticleSystem trail, float radius, float maxVel, float velPerBounce)
		{
			PickupStandardPhysics pickupStandardPhysics = thrownScp018.PhysicsModule as PickupStandardPhysics;
			if (pickupStandardPhysics == null)
			{
				throw new InvalidOperationException("SCP-018's physics module can only replace PickupStandardPhysics");
			}
			this._scp018 = thrownScp018;
			this._trail = trail;
			this._radius = radius;
			this._maxVel = maxVel;
			this._velPerBounce = velPerBounce;
			this._buffer = new OrderedBufferQueue<Scp018Physics.BounceData>((Scp018Physics.BounceData x, Scp018Physics.BounceData y) => x.Time > y.Time);
			this._lastTime = NetworkTime.time;
			this._lastVelocity = pickupStandardPhysics.Rb.velocity;
			this._lastPosition = new RelativePosition(this._scp018.Position);
			this._prevBounce = new Scp018Physics.BounceData
			{
				RelPos = this._lastPosition,
				Time = this._lastTime,
				VerticalSpeed = this._lastVelocity.y
			};
			pickupStandardPhysics.Rb.isKinematic = true;
			int layer = this._scp018.gameObject.layer;
			for (int i = 0; i < 32; i++)
			{
				if (!Physics.GetIgnoreLayerCollision(layer, i))
				{
					this._detectionMask |= 1 << i;
				}
			}
			this._nextBounce = this.PrecomputeNextBounce();
			if (!NetworkServer.active)
			{
				return;
			}
			this._wasServer = true;
			StaticUnityMethods.OnUpdate += this.ServerUpdatePrediction;
			this.Pickup.GetComponentsInChildren<Collider>().ForEach(delegate(Collider x)
			{
				x.isTrigger = true;
			});
		}

		public override void DestroyModule()
		{
			base.DestroyModule();
			if (!this._wasServer)
			{
				return;
			}
			StaticUnityMethods.OnUpdate -= this.ServerUpdatePrediction;
		}

		internal override void ClientProcessRpc(NetworkReader rpcData)
		{
			base.ClientProcessRpc(rpcData);
			double num = rpcData.ReadDouble();
			if (NetworkServer.active || NetworkTime.time > num)
			{
				return;
			}
			RelativePosition relativePosition = rpcData.ReadRelativePosition();
			float num2 = rpcData.ReadFloat();
			this._buffer.Enqueue(new Scp018Physics.BounceData
			{
				RelPos = relativePosition,
				Time = num,
				VerticalSpeed = num2
			});
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
				Scp018Physics.BounceData data = this.PrecomputeNextBounce();
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

		private Scp018Physics.BounceData PrecomputeNextBounce()
		{
			float magnitude;
			RaycastHit raycastHit;
			for (;;)
			{
				magnitude = this._lastVelocity.magnitude;
				float num = magnitude * 0.1f;
				bool flag = this.TrySphereCast(this._lastPosition.Position, this._lastVelocity, num, out raycastHit);
				this._lastPosition = new RelativePosition(raycastHit.point);
				if (flag)
				{
					break;
				}
				this._lastTime += 0.10000000149011612;
				this._lastVelocity += Physics.gravity * 0.1f;
				if (this._lastPosition.OutOfRange)
				{
					goto Block_1;
				}
			}
			this._lastTime += (double)(raycastHit.distance / magnitude);
			this.BounceTrajectory(raycastHit.normal);
			goto IL_00B5;
			Block_1:
			this._outOfBounds = true;
			IL_00B5:
			return new Scp018Physics.BounceData
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
			if (Physics.CheckSphere(origin, this._radius, this._detectionMask, QueryTriggerInteraction.Ignore) && this._lastSafeOrigin != null)
			{
				origin = this._lastSafeOrigin.Value;
				dir = global::UnityEngine.Random.insideUnitSphere;
			}
			if (!Physics.SphereCast(origin, this._radius, dir, out hit, maxDis + 0.04f, this._detectionMask, QueryTriggerInteraction.Ignore))
			{
				hit.point = origin + dir * maxDis;
				return false;
			}
			hit.point += hit.normal * this._radius - dir * 0.04f;
			this._lastSafeOrigin = new Vector3?(origin);
			return true;
		}

		public const int RpcSize = 19;

		private readonly OrderedBufferQueue<Scp018Physics.BounceData> _buffer;

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

		private Scp018Physics.BounceData _prevBounce;

		private Scp018Physics.BounceData _nextBounce;

		private struct BounceData
		{
			public float VerticalSpeed;

			public RelativePosition RelPos;

			public double Time;
		}
	}
}
