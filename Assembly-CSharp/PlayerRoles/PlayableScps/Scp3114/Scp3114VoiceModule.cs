using System;
using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114VoiceModule : StandardScpVoiceModule
	{
		public event Action OnHintTriggered;

		public override bool IsSpeaking
		{
			get
			{
				return base.IsSpeaking || this._proximityPlayback.MaxSamples > 0;
			}
		}

		private bool IsDisguised
		{
			get
			{
				return this.ScpRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active;
			}
		}

		private Scp3114Role ScpRole
		{
			get
			{
				return base.Role as Scp3114Role;
			}
		}

		private bool AnyHumansNearby
		{
			get
			{
				Vector3 cameraPosition = this.ScpRole.CameraPosition;
				foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
				{
					HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
					if (humanRole != null && (humanRole.CameraPosition - cameraPosition).sqrMagnitude <= 320f && Physics.Linecast(cameraPosition, humanRole.CameraPosition, VisionInformation.VisionLayerMask))
					{
						return true;
					}
				}
				return false;
			}
		}

		private void UpdateHint(bool primary, bool alt)
		{
			if (!this.IsDisguised)
			{
				return;
			}
			if (alt && this.AnyHumansNearby)
			{
				this._hintAlreadyDisplayed = true;
				return;
			}
			if (primary && this.AnyHumansNearby)
			{
				this._hintAlreadyDisplayed = true;
				Action onHintTriggered = this.OnHintTriggered;
				if (onHintTriggered == null)
				{
					return;
				}
				onHintTriggered();
			}
		}

		protected override VoiceChatChannel ProcessInputs(bool primary, bool alt)
		{
			if (!this._hintAlreadyDisplayed)
			{
				this.UpdateHint(primary, alt);
			}
			if (primary)
			{
				return this.PrimaryChannel;
			}
			if (alt && this.IsDisguised)
			{
				return VoiceChatChannel.Proximity;
			}
			return VoiceChatChannel.None;
		}

		protected override void ProcessSamples(float[] data, int len)
		{
			if (base.CurrentChannel == VoiceChatChannel.Proximity)
			{
				this._proximityPlayback.Buffer.Write(data, len);
				return;
			}
			base.ProcessSamples(data, len);
		}

		public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
		{
			if ((channel != VoiceChatChannel.Proximity || !this.IsDisguised) && channel != this.PrimaryChannel)
			{
				return VoiceChatChannel.None;
			}
			return channel;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._hintAlreadyDisplayed = false;
		}

		[SerializeField]
		private SingleBufferPlayback _proximityPlayback;

		private bool _hintAlreadyDisplayed;

		private const VoiceChatChannel ProximityChannel = VoiceChatChannel.Proximity;

		private const float HumanRangeSqr = 320f;
	}
}
