using System;
using CameraShaking;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Dance : StandardSubroutine<Scp3114Role>, IShakeEffect
{
	[SerializeField]
	private int _danceVariants;

	[SerializeField]
	private string _secretCode;

	[SerializeField]
	private float _cameraAdjustSpeed;

	[SerializeField]
	private float _cameraMaxDistance;

	[SerializeField]
	private ActionName[] _cancelKeys;

	private float _curCameraDis;

	private int _nextMatchIndex;

	private int _codeLength;

	private bool _shakeActive;

	private Transform _tr;

	private Vector3 _lastFwd;

	private double _lastRpcTime;

	private RelativePosition _serverStartPos;

	private const float MaxPositionDiffSqr = 2.5f;

	private const float CameraRadius = 0.16f;

	private const float MinDuration = 0.5f;

	public bool IsDancing { get; private set; }

	public int DanceVariant { get; private set; }

	public bool ThirdpersonMode
	{
		get
		{
			if (!(this._curCameraDis > 0f))
			{
				return this.IsDancing;
			}
			return true;
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.UpdateServer();
		}
		this.UpdateCamera();
		if (!base.Role.IsLocalPlayer)
		{
			return;
		}
		if (this.IsDancing)
		{
			if (this.TryEndDancing())
			{
				base.ClientSendCmd();
			}
		}
		else if (this.TryStartDancing())
		{
			base.ClientSendCmd();
		}
	}

	private void UpdateServer()
	{
		if (this.IsDancing && !((this._serverStartPos.Position - base.CastRole.FpcModule.Position).sqrMagnitude < 2.5f))
		{
			this.IsDancing = false;
			base.ServerSendRpc(toAll: true);
		}
	}

	private void SetModelVisibility(bool b)
	{
		base.CastRole.FpcModule.CharacterModelInstance.SetVisibility(b);
	}

	private void UpdateCamera()
	{
		this._lastFwd = this._tr.forward;
		if (!this.IsDancing)
		{
			if (!(this._curCameraDis <= 0f))
			{
				this._curCameraDis -= Time.deltaTime * this._cameraAdjustSpeed;
				if (!(this._curCameraDis > 0f))
				{
					this.SetModelVisibility(!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated());
					this._curCameraDis = 0f;
				}
			}
		}
		else
		{
			this._curCameraDis += Time.deltaTime * this._cameraAdjustSpeed;
			float num = this._cameraMaxDistance;
			if (Physics.Raycast(base.CastRole.CameraPosition, -this._lastFwd, out var hitInfo, num + 0.16f, FpcStateProcessor.Mask))
			{
				num = hitInfo.distance - 0.16f;
			}
			this._curCameraDis = Math.Min(num, this._curCameraDis);
		}
	}

	private bool TryStartDancing()
	{
		string inputString = Input.inputString;
		for (int i = 0; i < inputString.Length; i++)
		{
			if (char.ToLowerInvariant(inputString[i]) != this._secretCode[this._nextMatchIndex])
			{
				this._nextMatchIndex = 0;
			}
			else if (++this._nextMatchIndex == this._codeLength)
			{
				this._nextMatchIndex = 0;
				return true;
			}
		}
		return false;
	}

	private bool TryEndDancing()
	{
		if (NetworkTime.time < this._lastRpcTime + 0.5)
		{
			return false;
		}
		ActionName[] cancelKeys = this._cancelKeys;
		for (int i = 0; i < cancelKeys.Length; i++)
		{
			if (Input.GetKey(NewInput.GetKey(cancelKeys[i])))
			{
				return true;
			}
		}
		return false;
	}

	protected override void Awake()
	{
		base.Awake();
		this._tr = base.transform;
		this._codeLength = this._secretCode.Length;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (!this._shakeActive)
		{
			CameraShakeController.AddEffect(this);
			this._shakeActive = true;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.IsDancing = false;
		this._nextMatchIndex = 0;
		this._lastRpcTime = 0.0;
		this._curCameraDis = 0f;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteBool(!this.IsDancing);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (reader.ReadBool())
		{
			if (!base.CastRole.SkeletonIdle)
			{
				return;
			}
			this.IsDancing = true;
			this._serverStartPos = new RelativePosition(base.CastRole.FpcModule.Position);
		}
		else
		{
			this.IsDancing = false;
		}
		base.ServerSendRpc(toAll: true);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.IsDancing = reader.ReadBool();
		this.DanceVariant = reader.ReadByte() % this._danceVariants;
		this._lastRpcTime = NetworkTime.time;
		if (this.IsDancing)
		{
			this.SetModelVisibility(b: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this.IsDancing);
		writer.WriteByte((byte)UnityEngine.Random.Range(0, 255));
	}

	public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
	{
		shakeValues = ShakeEffectValues.None;
		if (base.Role.Pooled)
		{
			this._shakeActive = false;
			return false;
		}
		if (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated())
		{
			Vector3? rootCameraPositionOffset = this._lastFwd * (0f - this._curCameraDis);
			shakeValues = new ShakeEffectValues(null, null, rootCameraPositionOffset);
		}
		return true;
	}
}
