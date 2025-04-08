using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicPointController : StandardSubroutine<Scp939Role>
	{
		public event Action<Scp939HudTranslation> OnMessageReceived;

		public Transform MimicPointTransform { get; private set; }

		public float MaxDistance { get; private set; }

		public bool Active
		{
			get
			{
				return this._active;
			}
			private set
			{
				if (value == this._active)
				{
					return;
				}
				this._active = value;
				if (value)
				{
					this.UpdateMimicPoint();
					MainCameraController.OnUpdated += this.UpdateIcon;
					FirstPersonMovementModule.OnPositionUpdated += this.UpdateMimicPoint;
					return;
				}
				this.MimicPointTransform.localPosition = Vector3.zero;
				this._mimicPointIcon.enabled = false;
				MainCameraController.OnUpdated -= this.UpdateIcon;
				FirstPersonMovementModule.OnPositionUpdated -= this.UpdateMimicPoint;
			}
		}

		public float Distance
		{
			get
			{
				return Vector3.Distance(base.CastRole.FpcModule.Position, this.MimicPointTransform.position);
			}
		}

		private bool Visible
		{
			get
			{
				ReferenceHub referenceHub;
				return ReferenceHub.TryGetPovHub(out referenceHub) && referenceHub.IsSCP(true);
			}
		}

		private void UpdateMimicPoint()
		{
			Vector3 position = this._syncPos.Position;
			this.MimicPointTransform.position = position;
			this.UpdateIcon();
			if (!NetworkServer.active)
			{
				return;
			}
			if (this.Distance < this.MaxDistance)
			{
				return;
			}
			this._syncMessage = MimicPointController.RpcStateMsg.DestroyedByDistance;
			base.ServerSendRpc(true);
		}

		private void UpdateIcon()
		{
			if (this.Visible)
			{
				this._mimicPointIcon.enabled = true;
				this.MimicPointTransform.forward = MainCameraController.CurrentCamera.forward;
				return;
			}
			this._mimicPointIcon.enabled = false;
		}

		private void OnHubAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.ServerSendRpc(hub);
		}

		private void OnDestroy()
		{
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnHubAdded));
		}

		protected override void Awake()
		{
			base.Awake();
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnHubAdded));
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Active = false;
			this._cooldown.Clear();
		}

		public void ClientToggle()
		{
			if (!this._cooldown.IsReady)
			{
				return;
			}
			base.ClientSendCmd();
			this._cooldown.Trigger(0.20000000298023224);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (this.Active)
			{
				this._syncMessage = MimicPointController.RpcStateMsg.RemovedByUser;
				this.Active = false;
			}
			else
			{
				this._syncMessage = MimicPointController.RpcStateMsg.PlacedByUser;
				this._syncPos = new RelativePosition(base.CastRole.FpcModule.Position);
				this.Active = true;
			}
			base.ServerSendRpc(true);
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
			this._syncMessage = (MimicPointController.RpcStateMsg)reader.ReadByte();
			MimicPointController.RpcStateMsg syncMessage = this._syncMessage;
			if (syncMessage == MimicPointController.RpcStateMsg.None)
			{
				return;
			}
			if (syncMessage != MimicPointController.RpcStateMsg.PlacedByUser)
			{
				this.Active = false;
			}
			else
			{
				this._syncPos = reader.ReadRelativePosition();
				this.Active = true;
			}
			Action<Scp939HudTranslation> onMessageReceived = this.OnMessageReceived;
			if (onMessageReceived == null)
			{
				return;
			}
			onMessageReceived((Scp939HudTranslation)this._syncMessage);
		}

		[SerializeField]
		private Renderer _mimicPointIcon;

		private bool _active;

		private RelativePosition _syncPos;

		private MimicPointController.RpcStateMsg _syncMessage;

		private readonly AbilityCooldown _cooldown = new AbilityCooldown();

		private const float CooldownDuration = 0.2f;

		private enum RpcStateMsg
		{
			None,
			PlacedByUser = 25,
			RemovedByUser,
			DestroyedByDistance
		}
	}
}
