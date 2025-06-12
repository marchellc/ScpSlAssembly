using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicPointController : StandardSubroutine<Scp939Role>
{
	private enum RpcStateMsg
	{
		None = 0,
		PlacedByUser = 25,
		RemovedByUser = 26,
		DestroyedByDistance = 27
	}

	[SerializeField]
	private Renderer _mimicPointIcon;

	private bool _active;

	private RelativePosition _syncPos;

	private RpcStateMsg _syncMessage;

	private readonly AbilityCooldown _cooldown = new AbilityCooldown();

	private const float CooldownDuration = 0.2f;

	[field: SerializeField]
	public Transform MimicPointTransform { get; private set; }

	[field: SerializeField]
	public float MaxDistance { get; private set; }

	public bool Active
	{
		get
		{
			return this._active;
		}
		private set
		{
			if (value != this._active)
			{
				this._active = value;
				if (value)
				{
					this.UpdateMimicPoint();
					MainCameraController.OnUpdated += UpdateIcon;
					FirstPersonMovementModule.OnPositionUpdated += UpdateMimicPoint;
				}
				else
				{
					this.MimicPointTransform.localPosition = Vector3.zero;
					this._mimicPointIcon.enabled = false;
					MainCameraController.OnUpdated -= UpdateIcon;
					FirstPersonMovementModule.OnPositionUpdated -= UpdateMimicPoint;
				}
			}
		}
	}

	public float Distance => Vector3.Distance(base.CastRole.FpcModule.Position, this.MimicPointTransform.position);

	private bool Visible
	{
		get
		{
			if (ReferenceHub.TryGetPovHub(out var hub))
			{
				return hub.IsSCP();
			}
			return false;
		}
	}

	public event Action<Scp939HudTranslation> OnMessageReceived;

	private void UpdateMimicPoint()
	{
		Vector3 position = this._syncPos.Position;
		this.MimicPointTransform.position = position;
		this.UpdateIcon();
		if (NetworkServer.active && !(this.Distance < this.MaxDistance))
		{
			this._syncMessage = RpcStateMsg.DestroyedByDistance;
			base.ServerSendRpc(toAll: true);
		}
	}

	private void UpdateIcon()
	{
		if (this.Visible)
		{
			this._mimicPointIcon.enabled = true;
			this.MimicPointTransform.forward = MainCameraController.CurrentCamera.forward;
		}
		else
		{
			this._mimicPointIcon.enabled = false;
		}
	}

	private void OnHubAdded(ReferenceHub hub)
	{
		if (NetworkServer.active)
		{
			base.ServerSendRpc(hub);
		}
	}

	private void OnDestroy()
	{
		ReferenceHub.OnPlayerAdded -= OnHubAdded;
	}

	protected override void Awake()
	{
		base.Awake();
		ReferenceHub.OnPlayerAdded += OnHubAdded;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Active = false;
		this._cooldown.Clear();
	}

	public void ClientToggle()
	{
		if (this._cooldown.IsReady)
		{
			base.ClientSendCmd();
			this._cooldown.Trigger(0.20000000298023224);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.Active)
		{
			this._syncMessage = RpcStateMsg.RemovedByUser;
			this.Active = false;
		}
		else
		{
			this._syncMessage = RpcStateMsg.PlacedByUser;
			this._syncPos = new RelativePosition(base.CastRole.FpcModule.Position);
			this.Active = true;
		}
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._syncMessage);
		if (this.Active)
		{
			writer.WriteRelativePosition(this._syncPos);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._syncMessage = (RpcStateMsg)reader.ReadByte();
		switch (this._syncMessage)
		{
		case RpcStateMsg.None:
			return;
		case RpcStateMsg.PlacedByUser:
			this._syncPos = reader.ReadRelativePosition();
			this.Active = true;
			break;
		default:
			this.Active = false;
			break;
		}
		this.OnMessageReceived?.Invoke((Scp939HudTranslation)this._syncMessage);
	}
}
