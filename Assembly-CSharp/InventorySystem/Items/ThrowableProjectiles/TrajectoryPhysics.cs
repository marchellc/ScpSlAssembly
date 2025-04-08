using System;
using System.Runtime.CompilerServices;
using InventorySystem.Items.Pickups;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class TrajectoryPhysics : PickupPhysicsModule
	{
		private Vector3 HalfGravity
		{
			get
			{
				return Physics.gravity / 2f;
			}
		}

		protected override ItemPickupBase Pickup
		{
			get
			{
				return this._pickup;
			}
		}

		public bool DestinationReached { get; private set; }

		public Vector3 LastVelocity { get; private set; }

		public TrajectoryPhysics(ItemPickupBase ipb)
		{
			this._pickup = ipb;
			this._pickup.PhysicsModuleSyncData.OnModified += this.OnSyncVarsModified;
			StaticUnityMethods.OnUpdate += this.UpdateTrajectory;
		}

		public override void DestroyModule()
		{
			base.DestroyModule();
			StaticUnityMethods.OnUpdate -= this.UpdateTrajectory;
		}

		public void ServerSetup(Vector3 position, Vector3 velocity, float radius, LayerMask detectionMask)
		{
			TrajectoryPhysics.<>c__DisplayClass22_0 CS$<>8__locals1;
			CS$<>8__locals1.velocity = velocity;
			CS$<>8__locals1.position = position;
			CS$<>8__locals1.radius = radius;
			CS$<>8__locals1.detectionMask = detectionMask;
			RelativePosition relativePosition = new RelativePosition(CS$<>8__locals1.position);
			float y = CS$<>8__locals1.velocity.y;
			float num = 0f;
			bool flag;
			RelativePosition relativePosition2;
			do
			{
				CS$<>8__locals1.velocity += this.HalfGravity * 0.1f;
				RaycastHit raycastHit;
				flag = TrajectoryPhysics.<ServerSetup>g__TrySphereCast|22_0(CS$<>8__locals1.velocity.magnitude * 0.1f, out raycastHit, ref CS$<>8__locals1);
				CS$<>8__locals1.velocity += this.HalfGravity * 0.1f;
				CS$<>8__locals1.position = raycastHit.point;
				num += 0.1f;
				relativePosition2 = new RelativePosition(CS$<>8__locals1.position);
			}
			while (!flag && !relativePosition2.OutOfRange && !Physics.CheckSphere(CS$<>8__locals1.position, CS$<>8__locals1.radius + 0.04f, CS$<>8__locals1.detectionMask));
			this.ServerSetup(NetworkTime.time, NetworkTime.time + (double)num, relativePosition, relativePosition2, y);
		}

		public void ServerSetup(double startTime, double endTime, RelativePosition startPosition, RelativePosition endPosition, float verticalVelocity)
		{
			this._startTime = startTime;
			this._endTime = endTime;
			this._startPosition = startPosition;
			this._endPosition = endPosition;
			this._startVerticalVelocity = verticalVelocity;
			base.ServerSetSyncData(delegate(NetworkWriter writer)
			{
				writer.WriteDouble(startTime);
				writer.WriteDouble(this._endTime);
				writer.WriteRelativePosition(startPosition);
				writer.WriteRelativePosition(endPosition);
				writer.WriteFloat(verticalVelocity);
			});
		}

		private void OnSyncVarsModified()
		{
			base.ClientReadSyncData(delegate(NetworkReader reader)
			{
				this._startTime = reader.ReadDouble();
				this._endTime = reader.ReadDouble();
				this._startPosition = reader.ReadRelativePosition();
				this._endPosition = reader.ReadRelativePosition();
				this._startVerticalVelocity = reader.ReadFloat();
			});
		}

		private void UpdateTrajectory()
		{
			double num = this._endTime - this._startTime;
			double num2 = Math.Clamp(NetworkTime.time - this._startTime, 0.0, num);
			double num3 = num2 * num2;
			double num4 = num2 / num;
			Vector3 position = this._startPosition.Position;
			Vector3 position2 = this._endPosition.Position;
			float num5 = (float)((double)(-(double)this._startVerticalVelocity) * num2 - (double)this.HalfGravity.y * num3);
			Vector3 vector = Vector3.Lerp(position, position2, (float)num4);
			double num6 = num4 * num4;
			float num7 = (float)(num6 * num6);
			float num8 = Mathf.Lerp(position.y - num5, position2.y, num7);
			this._pickup.Position = new Vector3(vector.x, num8, vector.z);
			if (Math.Abs(num4 - 1.0) <= 1.401298464324817E-45)
			{
				this.DestinationReached = true;
				return;
			}
			double num9 = (double)this._startVerticalVelocity + (double)Physics.gravity.y * num2;
			Vector3 vector2 = (position2 - position) / (float)num;
			this.LastVelocity = new Vector3(vector2.x, (float)num9, vector2.z);
		}

		[CompilerGenerated]
		internal static bool <ServerSetup>g__TrySphereCast|22_0(float maxDis, out RaycastHit hit, ref TrajectoryPhysics.<>c__DisplayClass22_0 A_2)
		{
			Vector3 normalized = A_2.velocity.normalized;
			if (!Physics.SphereCast(A_2.position, A_2.radius, normalized, out hit, maxDis, A_2.detectionMask))
			{
				hit.point = A_2.position + normalized * maxDis;
				return false;
			}
			hit.point += hit.normal * A_2.radius;
			return true;
		}

		private const int UpdateFrequency = 10;

		private const float InaccuracyCompensation = 0.04f;

		private readonly ItemPickupBase _pickup;

		private double _startTime;

		private double _endTime;

		private RelativePosition _startPosition;

		private RelativePosition _endPosition;

		private float _startVerticalVelocity;
	}
}
