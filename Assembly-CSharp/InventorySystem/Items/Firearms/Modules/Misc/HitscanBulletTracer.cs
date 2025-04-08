using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;
using UnityEngine.VFX;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class HitscanBulletTracer : TracerBase
	{
		protected override bool IsBusy
		{
			get
			{
				return this._stopwatch.Elapsed.TotalSeconds < (double)this._minimalDuration || (this._supportsWhizz && this._whizzHandler.IsPlaying);
			}
		}

		public override void ServerWriteExtraData(Firearm firearm, NetworkWriter writer)
		{
			base.ServerWriteExtraData(firearm, writer);
			writer.WriteByte((byte)firearm.Owner.GetTeam());
		}

		protected override void OnCreated()
		{
			base.OnCreated();
			this._tr = base.transform;
			this._supportsWhizz = base.TryGetComponent<BulletWhizzHandler>(out this._whizzHandler);
			this._stopwatch = Stopwatch.StartNew();
		}

		protected override void OnDequeued()
		{
			base.OnDequeued();
			this._stopwatch.Restart();
		}

		protected override void OnFired(NetworkReader reader)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetPovHub(out referenceHub))
			{
				return;
			}
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(base.RelativeHitPosition.WaypointId, out waypointBase))
			{
				return;
			}
			Vector3 worldspacePosition = waypointBase.GetWorldspacePosition(base.RelativeHitPosition.Relative);
			bool flag = !HitboxIdentity.IsEnemy(referenceHub.GetTeam(), (Team)reader.ReadByte());
			Vector3 position = MainCameraController.CurrentCamera.position;
			Vector3 vector;
			Vector3 vector2;
			Vector3 vector3;
			Vector3 vector4;
			if (!Misc.TryGetClosestLineSegment(base.OriginPosition, worldspacePosition, position, 1f, 15f, out vector, out vector2, out vector3, out vector4))
			{
				return;
			}
			if (!HitscanBulletTracer.ValidateRandom(Mathf.Abs(vector4.y), flag, base.Template.FirearmCategory))
			{
				return;
			}
			this._vfx.SetVector3(HitscanBulletTracer.TracerOriginHash, vector - vector2);
			this._vfx.SetBool(HitscanBulletTracer.TracerFriendlyHash, flag);
			this._vfx.Play();
			this._tr.SetPositionAndRotation(vector2, Quaternion.identity);
			this._tr.SetParent(waypointBase.transform);
			if (flag || !this._supportsWhizz || (vector3 - position).sqrMagnitude > 25f)
			{
				return;
			}
			this._whizzHandler.Play(vector4, vector3);
		}

		private static bool ValidateRandom(float verticalDot, bool isFriendly, FirearmCategory cat)
		{
			float num = 1f - Mathf.InverseLerp(0.08f, 0.35f, verticalDot);
			float num2 = (isFriendly ? 0.5f : 1f);
			float num4;
			float num3 = (HitscanBulletTracer.CategoryMultipliers.TryGetValue(cat, out num4) ? num4 : 1f);
			return global::UnityEngine.Random.value < num * num2 * num3;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static HitscanBulletTracer()
		{
			Dictionary<FirearmCategory, float> dictionary = new Dictionary<FirearmCategory, float>();
			dictionary[FirearmCategory.Revolver] = 2f;
			dictionary[FirearmCategory.SubmachineGun] = 0.5f;
			HitscanBulletTracer.CategoryMultipliers = dictionary;
		}

		private const float MinLength = 1f;

		private const float MaxLength = 15f;

		private const float RandomMaxChanceDot = 0.08f;

		private const float RandomNoChanceDot = 0.35f;

		private const float WhizzTriggerRangeSqr = 25f;

		private const float RandomFriendlyFireMultiplier = 0.5f;

		private static readonly int TracerOriginHash = Shader.PropertyToID("Origin");

		private static readonly int TracerFriendlyHash = Shader.PropertyToID("IsFriendly");

		private static readonly Dictionary<FirearmCategory, float> CategoryMultipliers;

		[SerializeField]
		private VisualEffect _vfx;

		[SerializeField]
		private float _minimalDuration;

		private Transform _tr;

		private BulletWhizzHandler _whizzHandler;

		private bool _supportsWhizz;

		private Stopwatch _stopwatch;
	}
}
