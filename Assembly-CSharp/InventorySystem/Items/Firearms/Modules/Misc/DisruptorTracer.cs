using System;
using System.Diagnostics;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class DisruptorTracer : TracerBase
	{
		protected override bool IsBusy
		{
			get
			{
				return this._stopwatch.Elapsed.TotalSeconds < 5.0;
			}
		}

		protected override void OnCreated()
		{
			base.OnCreated();
			this._tr = base.transform;
			this._stopwatch = Stopwatch.StartNew();
		}

		protected override void OnDequeued()
		{
			base.OnDequeued();
			this._stopwatch.Restart();
		}

		protected override void OnFired(NetworkReader reader)
		{
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(base.RelativeHitPosition.WaypointId, out waypointBase))
			{
				return;
			}
			DisruptorActionModule.FiringState firingState = (DisruptorActionModule.FiringState)reader.ReadByte();
			DisruptorTracer.EffectsPair effectsPair;
			if (firingState != DisruptorActionModule.FiringState.FiringRapid)
			{
				if (firingState != DisruptorActionModule.FiringState.FiringSingle)
				{
					return;
				}
				effectsPair = this._singleShotEffects;
			}
			else
			{
				effectsPair = this._rapidFireEffects;
			}
			Vector3 worldspacePosition = waypointBase.GetWorldspacePosition(base.RelativeHitPosition.Relative);
			Vector3 position = MainCameraController.CurrentCamera.position;
			Vector3 vector;
			Vector3 vector2;
			Vector3 vector3;
			Vector3 vector4;
			if (!Misc.TryGetClosestLineSegment(base.OriginPosition, worldspacePosition, position, 0.5f, 35f, out vector, out vector2, out vector3, out vector4))
			{
				return;
			}
			effectsPair.Play(Vector3.Distance(vector, vector2));
			this._tr.position = vector;
			this._tr.forward = vector4;
			this._tr.SetParent(waypointBase.transform);
			if ((vector3 - position).sqrMagnitude > effectsPair.WhizzTriggerRangeSqr)
			{
				return;
			}
			effectsPair.WhizzHandler.Play(vector4, vector3);
		}

		public override void ServerWriteExtraData(Firearm firearm, NetworkWriter writer)
		{
			base.ServerWriteExtraData(firearm, writer);
			DisruptorHitregModule disruptorHitregModule;
			if (!firearm.TryGetModule(out disruptorHitregModule, true))
			{
				writer.WriteByte(0);
				return;
			}
			writer.WriteByte((byte)disruptorHitregModule.LastFiringState);
		}

		private const float RecycleTime = 5f;

		private const float MinLength = 0.5f;

		private const float MaxLength = 35f;

		[SerializeField]
		private DisruptorTracer.EffectsPair _singleShotEffects;

		[SerializeField]
		private DisruptorTracer.EffectsPair _rapidFireEffects;

		private Transform _tr;

		private Stopwatch _stopwatch;

		[Serializable]
		private class EffectsPair
		{
			public void Play(float distance)
			{
				foreach (ParticleSystem particleSystem in this.Particles)
				{
					particleSystem.main.startSpeed = 100f * distance;
					particleSystem.Play();
				}
			}

			private const float DistanceToSpeed = 100f;

			public ParticleSystem[] Particles;

			public BulletWhizzHandler WhizzHandler;

			public float WhizzTriggerRangeSqr;
		}
	}
}
