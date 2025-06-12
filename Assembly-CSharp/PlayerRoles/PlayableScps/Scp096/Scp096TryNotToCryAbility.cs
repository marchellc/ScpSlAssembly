using System;
using System.Diagnostics;
using CursorManagement;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096TryNotToCryAbility : KeySubroutine<Scp096Role>, ICursorOverride
{
	private const float HumeRegenerationMultiplier = 2f;

	[SerializeField]
	private float _clientDotTolerance;

	[SerializeField]
	private float _serverDotTolerance;

	[SerializeField]
	private float _clientDisTolerance;

	[SerializeField]
	private float _serverDisTolerance;

	[SerializeField]
	private float _maxVerticalAngle;

	[SerializeField]
	private float _maxDistance;

	[SerializeField]
	private float _minWidth;

	[SerializeField]
	private float _sideOffset;

	[SerializeField]
	private float _groundLevelMaxDiff;

	private DynamicHumeShieldController _dhs;

	private RelativePosition _syncPoint;

	private Quaternion _syncRot;

	private float _cachedBaseRegen;

	private bool _canceled;

	private readonly Stopwatch _freezeSw = new Stopwatch();

	private const float AbsFreezeDuration = 0.1f;

	private const float RadiusTolerance = 0.9f;

	private static readonly Quaternion[] RotationAngles = new Quaternion[2]
	{
		Quaternion.Euler(Vector3.up * 90f),
		Quaternion.Euler(Vector3.down * 90f)
	};

	private static readonly ActionName[] CancelKeys = new ActionName[5]
	{
		ActionName.MoveBackward,
		ActionName.MoveForward,
		ActionName.MoveLeft,
		ActionName.MoveRight,
		ActionName.Jump
	};

	private static readonly CachedLayerMask Mask = new CachedLayerMask("Door", "Glass", "Default");

	private static readonly float[] Heights = new float[3] { 0f, -0.4f, -0.9f };

	private static readonly Vector3[] Offsets = new Vector3[Scp096TryNotToCryAbility.RotationAngles.Length + 1];

	private static readonly Vector3[] GroundPoints = new Vector3[4];

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement
	{
		get
		{
			if (!base.Owner.isLocalPlayer || this._canceled)
			{
				return false;
			}
			if (this.IsActive)
			{
				return true;
			}
			if (!this._freezeSw.IsRunning)
			{
				return false;
			}
			double num = NetworkTime.rtt + 0.10000000149011612;
			return this._freezeSw.Elapsed.TotalSeconds < num;
		}
	}

	protected override ActionName TargetKey => ActionName.Zoom;

	private bool IsActive
	{
		get
		{
			return base.CastRole.IsAbilityState(Scp096AbilityState.TryingNotToCry);
		}
		set
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException(string.Format("Cannot set {0}.{1} as client.", this, "IsActive"));
			}
			if (this.IsActive == value)
			{
				return;
			}
			if (value)
			{
				Scp096TryingNotToCryEventArgs e = new Scp096TryingNotToCryEventArgs(base.Owner);
				Scp096Events.OnTryingNotToCry(e);
				if (e.IsAllowed)
				{
					base.CastRole.StateController.SetAbilityState(Scp096AbilityState.TryingNotToCry);
					this._dhs.RegenerationRate = this._cachedBaseRegen * 2f;
					Scp096Events.OnTriedNotToCry(new Scp096TriedNotToCryEventArgs(base.Owner));
				}
			}
			else if (this.IsActive)
			{
				Scp096StartCryingEventArgs e2 = new Scp096StartCryingEventArgs(base.Owner);
				Scp096Events.OnStartCrying(e2);
				if (e2.IsAllowed)
				{
					base.CastRole.ResetAbilityState();
					this._dhs.RegenerationRate = this._cachedBaseRegen;
					Scp096Events.OnStartedCrying(new Scp096StartedCryingEventArgs(base.Owner));
				}
			}
		}
	}

	protected override void Update()
	{
		base.Update();
		if (this.IsActive)
		{
			if (base.Owner.isLocalPlayer)
			{
				this.UpdateClient();
			}
			if (NetworkServer.active && !this.ServerValidate())
			{
				this.IsActive = false;
			}
		}
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (!this.IsActive && this.ValidatePoint())
		{
			this._canceled = false;
			this._freezeSw.Restart();
			base.ClientSendCmd();
		}
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		if (!this._canceled)
		{
			RelativePosition msg = new RelativePosition(base.CastRole.FpcModule.Position);
			if (WaypointBase.TryGetWaypoint(msg.WaypointId, out var wp))
			{
				writer.WriteRelativePosition(msg);
				writer.WriteQuaternion(wp.GetRelativeRotation(base.Owner.PlayerCameraReference.rotation));
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (reader.Position >= reader.Capacity)
		{
			this.IsActive = false;
			return;
		}
		this._syncPoint = reader.ReadRelativePosition();
		this._syncRot = reader.ReadQuaternion();
		this.IsActive = this.ServerValidate();
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		CursorManager.Register(this);
		if (base.CastRole.HumeShieldModule is DynamicHumeShieldController dynamicHumeShieldController)
		{
			this._cachedBaseRegen = dynamicHumeShieldController.RegenerationRate;
			this._dhs = dynamicHumeShieldController;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._freezeSw.Reset();
		this._dhs.RegenerationRate = this._cachedBaseRegen;
		CursorManager.Unregister(this);
	}

	private void UpdateClient()
	{
		if (this._canceled)
		{
			return;
		}
		ActionName[] cancelKeys = Scp096TryNotToCryAbility.CancelKeys;
		for (int i = 0; i < cancelKeys.Length; i++)
		{
			if (Input.GetKeyDown(NewInput.GetKey(cancelKeys[i])))
			{
				this._canceled = true;
				base.ClientSendCmd();
				break;
			}
		}
	}

	private bool ServerValidate()
	{
		if (base.CastRole.StateController.RageState != Scp096RageState.Docile)
		{
			return false;
		}
		if (!WaypointBase.TryGetWaypoint(this._syncPoint.WaypointId, out var wp))
		{
			return false;
		}
		Vector3 worldspacePosition = wp.GetWorldspacePosition(this._syncPoint.Relative);
		Quaternion worldspaceRotation = wp.GetWorldspaceRotation(this._syncRot);
		using (new FpcBacktracker(base.Owner, worldspacePosition, worldspaceRotation))
		{
			return this.ValidatePoint();
		}
	}

	private bool ValidatePoint()
	{
		if (this.ValidateGround())
		{
			return this.ValidateWall();
		}
		return false;
	}

	private bool ValidateGround()
	{
		if (!base.CastRole.FpcModule.IsGrounded)
		{
			return false;
		}
		Transform transform = base.Owner.transform;
		float height = base.CastRole.FpcModule.CharController.height;
		float num = base.CastRole.FpcModule.CharController.radius * 0.9f;
		Scp096TryNotToCryAbility.GroundPoints[0] = transform.position + transform.forward * num;
		Scp096TryNotToCryAbility.GroundPoints[1] = transform.position + transform.right * num;
		Scp096TryNotToCryAbility.GroundPoints[2] = transform.position - transform.forward * num;
		Scp096TryNotToCryAbility.GroundPoints[3] = transform.position - transform.right * num;
		float num2 = float.MaxValue;
		float num3 = 0f;
		for (int i = 0; i < Scp096TryNotToCryAbility.GroundPoints.Length; i++)
		{
			if (!Physics.Raycast(Scp096TryNotToCryAbility.GroundPoints[i], Vector3.down, out var hitInfo, height, Scp096TryNotToCryAbility.Mask))
			{
				return false;
			}
			float distance = hitInfo.distance;
			if (distance < num2)
			{
				num2 = distance;
			}
			if (distance > num3)
			{
				num3 = distance;
			}
			if (num3 - num2 > this._groundLevelMaxDiff)
			{
				return false;
			}
		}
		return true;
	}

	private bool ValidateWall()
	{
		Vector3 position = base.Owner.PlayerCameraReference.position;
		Vector3 forward = base.Owner.PlayerCameraReference.forward;
		if (base.Owner.isLocalPlayer && Mathf.Abs(base.CastRole.FpcModule.MouseLook.CurrentVertical) > this._maxVerticalAngle)
		{
			return false;
		}
		forward.y = 0f;
		float magnitude = forward.magnitude;
		if (magnitude == 0f)
		{
			return false;
		}
		forward /= magnitude;
		if (!Physics.Raycast(position, forward, out var hitInfo, this._maxDistance, Scp096TryNotToCryAbility.Mask))
		{
			return false;
		}
		float num = (base.Owner.isLocalPlayer ? this._clientDotTolerance : this._serverDotTolerance);
		Vector3 normal = hitInfo.normal;
		if (Vector3.Dot(forward, normal) > num)
		{
			return false;
		}
		Vector3 vector = position + normal * this._minWidth;
		Vector3 start = vector + Vector3.down * this._sideOffset;
		Vector3 end = vector + Vector3.up * (Scp096TryNotToCryAbility.Heights[Scp096TryNotToCryAbility.Heights.Length - 1] + this._sideOffset);
		if (Physics.CheckCapsule(start, end, this._sideOffset, Scp096TryNotToCryAbility.Mask))
		{
			return false;
		}
		float num2 = (base.Owner.isLocalPlayer ? this._clientDisTolerance : this._serverDisTolerance);
		float num3 = float.MaxValue;
		float num4 = 0f;
		for (int i = 0; i < Scp096TryNotToCryAbility.RotationAngles.Length; i++)
		{
			Scp096TryNotToCryAbility.Offsets[i] = Scp096TryNotToCryAbility.RotationAngles[i] * normal;
		}
		for (int j = 0; j < Scp096TryNotToCryAbility.Offsets.Length; j++)
		{
			for (int k = 0; k < Scp096TryNotToCryAbility.Heights.Length; k++)
			{
				if (!Physics.Raycast(Scp096TryNotToCryAbility.Offsets[j] * this._sideOffset + normal + position + Vector3.up * Scp096TryNotToCryAbility.Heights[k], -normal, out var hitInfo2, this._maxDistance, Scp096TryNotToCryAbility.Mask))
				{
					return false;
				}
				if (Vector3.Dot(forward, normal) > num)
				{
					return false;
				}
				float distance = hitInfo2.distance;
				if (distance < num3)
				{
					num3 = distance;
				}
				if (distance > num4)
				{
					num4 = distance;
				}
				if (num4 - num3 > num2)
				{
					return false;
				}
			}
		}
		return true;
	}
}
