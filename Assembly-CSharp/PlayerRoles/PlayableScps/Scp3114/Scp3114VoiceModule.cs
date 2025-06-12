using System;
using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114VoiceModule : StandardScpVoiceModule
{
	[SerializeField]
	private SingleBufferPlayback[] _proximityPlaybacks;

	private bool _hintAlreadyDisplayed;

	private const VoiceChatChannel ProximityChannel = VoiceChatChannel.Proximity;

	private const float HumanRangeSqr = 320f;

	public override bool IsSpeaking
	{
		get
		{
			if (!base.IsSpeaking)
			{
				return this._proximityPlaybacks[0].MaxSamples > 0;
			}
			return true;
		}
	}

	private bool IsDisguised => this.ScpRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active;

	private Scp3114Role ScpRole => base.Role as Scp3114Role;

	private bool AnyHumansNearby
	{
		get
		{
			Vector3 cameraPosition = this.ScpRole.CameraPosition;
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.roleManager.CurrentRole is HumanRole humanRole && !((humanRole.CameraPosition - cameraPosition).sqrMagnitude > 320f) && Physics.Linecast(cameraPosition, humanRole.CameraPosition, VisionInformation.VisionLayerMask))
				{
					return true;
				}
			}
			return false;
		}
	}

	public event Action OnHintTriggered;

	private void UpdateHint(bool primary, bool alt)
	{
		if (this.IsDisguised)
		{
			if (alt && this.AnyHumansNearby)
			{
				this._hintAlreadyDisplayed = true;
			}
			else if (primary && this.AnyHumansNearby)
			{
				this._hintAlreadyDisplayed = true;
				this.OnHintTriggered?.Invoke();
			}
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
			SingleBufferPlayback[] proximityPlaybacks = this._proximityPlaybacks;
			for (int i = 0; i < proximityPlaybacks.Length; i++)
			{
				proximityPlaybacks[i].Buffer.Write(data, len);
			}
		}
		else
		{
			base.ProcessSamples(data, len);
		}
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
}
