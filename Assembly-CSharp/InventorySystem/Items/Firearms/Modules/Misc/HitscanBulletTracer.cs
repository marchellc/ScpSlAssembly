using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;
using UnityEngine.VFX;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class HitscanBulletTracer : TracerBase
{
	private const float MinLength = 1f;

	private const float MaxLength = 15f;

	private const float RandomMaxChanceDot = 0.08f;

	private const float RandomNoChanceDot = 0.35f;

	private const float WhizzTriggerRangeSqr = 25f;

	private const float RandomFriendlyFireMultiplier = 0.5f;

	private static readonly int TracerOriginHash = Shader.PropertyToID("Origin");

	private static readonly int TracerFriendlyHash = Shader.PropertyToID("IsFriendly");

	private static readonly Dictionary<FirearmCategory, float> CategoryMultipliers = new Dictionary<FirearmCategory, float>
	{
		[FirearmCategory.Revolver] = 2f,
		[FirearmCategory.SubmachineGun] = 0.5f
	};

	[SerializeField]
	private VisualEffect _vfx;

	[SerializeField]
	private float _minimalDuration;

	private Transform _tr;

	private BulletWhizzHandler _whizzHandler;

	private bool _supportsWhizz;

	private Stopwatch _stopwatch;

	protected override bool IsBusy
	{
		get
		{
			if (!(this._stopwatch.Elapsed.TotalSeconds < (double)this._minimalDuration))
			{
				if (this._supportsWhizz)
				{
					return this._whizzHandler.IsPlaying;
				}
				return false;
			}
			return true;
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
		if (!ReferenceHub.TryGetPovHub(out var hub) || !WaypointBase.TryGetWaypoint(base.RelativeHitPosition.WaypointId, out var wp))
		{
			return;
		}
		Vector3 worldspacePosition = wp.GetWorldspacePosition(base.RelativeHitPosition.Relative);
		bool flag = !HitboxIdentity.IsEnemy(hub.GetTeam(), (Team)reader.ReadByte());
		Vector3 position = MainCameraController.CurrentCamera.position;
		if (global::Misc.TryGetClosestLineSegment(base.OriginPosition, worldspacePosition, position, 1f, 15f, out var newStart, out var newEnd, out var closestPointOnLine, out var normalizedDir) && HitscanBulletTracer.ValidateRandom(Mathf.Abs(normalizedDir.y), flag, base.Template.FirearmCategory))
		{
			this._vfx.SetVector3(HitscanBulletTracer.TracerOriginHash, newStart - newEnd);
			this._vfx.SetBool(HitscanBulletTracer.TracerFriendlyHash, flag);
			this._vfx.Play();
			this._tr.SetPositionAndRotation(newEnd, Quaternion.identity);
			this._tr.SetParent(wp.transform);
			if (!flag && this._supportsWhizz && !((closestPointOnLine - position).sqrMagnitude > 25f))
			{
				this._whizzHandler.Play(normalizedDir, closestPointOnLine);
			}
		}
	}

	private static bool ValidateRandom(float verticalDot, bool isFriendly, FirearmCategory cat)
	{
		float num = 1f - Mathf.InverseLerp(0.08f, 0.35f, verticalDot);
		float num2 = (isFriendly ? 0.5f : 1f);
		float value;
		float num3 = (HitscanBulletTracer.CategoryMultipliers.TryGetValue(cat, out value) ? value : 1f);
		return Random.value < num * num2 * num3;
	}
}
