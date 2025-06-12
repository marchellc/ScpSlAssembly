using System;
using AudioPooling;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;
using UserSettings;
using UserSettings.AudioSettings;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public abstract class Scp127VoiceTriggerBase : ModuleBase
{
	public delegate void ServerPlayCallback(Scp127VoiceTriggerBase trigger, Action<NetworkWriter> extraData, AudioClip clip, byte priority);

	protected enum VoiceLinePriority : byte
	{
		Low,
		Normal,
		High,
		VeryHigh
	}

	private static readonly CachedUserSetting<float> CachedVolumeSetting = new CachedUserSetting<float>(MixerAudioSettings.VolumeSliderSetting.Scp127Voice);

	private ServerPlayCallback _voicePlayCallback;

	private bool _eventsSet;

	protected virtual float DefaultAudioRange => 12f;

	protected virtual float DefaultAudioVolume => 0.8f;

	protected virtual float DefaultAudioStereo => 0.2f;

	protected virtual MixerChannel DefaultAudioMixerChannel => MixerChannel.DefaultSfx;

	protected virtual bool CheckEnemyLineOfSight => true;

	protected virtual bool RequireFriendship => true;

	protected bool HasFriendship => Scp127VoiceLineManagerModule.HasFriendship(base.ItemSerial, base.Item.Owner);

	protected float UserSettingsVoiceScale => Scp127VoiceTriggerBase.CachedVolumeSetting.Value;

	protected void ServerPlayVoiceLine(AudioClip clip, Action<NetworkWriter> extraData = null, VoiceLinePriority priority = VoiceLinePriority.Normal)
	{
		if (base.IsServer && (!this.RequireFriendship || this.HasFriendship))
		{
			if (this._voicePlayCallback == null)
			{
				string text = "Attempting to play a voice line on an unregistered Scp127VoiceTriggerBase - '" + base.name + "'.";
				string text2 = "Make sure this component is assigned to a child object of Scp127VoiceLineManagerModule.";
				throw new InvalidOperationException(text + " " + text2);
			}
			this._voicePlayCallback(this, extraData, clip, (byte)priority);
		}
	}

	protected void ServerPlayVoiceLineFromCollection(Scp127VoiceLineCollection collection, Action<NetworkWriter> extraData = null, VoiceLinePriority priority = VoiceLinePriority.Normal)
	{
		if (collection.TryGetRandom(base.Firearm.Owner.GetRoleId(), out var voiceLine))
		{
			this.ServerPlayVoiceLine(voiceLine, extraData, priority);
		}
	}

	protected virtual bool ValidateReceivingOwner(ReferenceHub owner)
	{
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return false;
		}
		if (this.CheckEnemyLineOfSight && HitboxIdentity.IsEnemy(hub, owner))
		{
			Vector3 position = owner.GetPosition();
			Vector3 lastCamPosition = CullingCamera.LastCamPosition;
			if (!CullingCamera.CheckBoundsVisibility(new Bounds(position, Vector3.one * 0.05f)))
			{
				return false;
			}
			if (Physics.Linecast(lastCamPosition, position, PlayerRolesUtils.LineOfSightMask))
			{
				return false;
			}
		}
		return true;
	}

	protected virtual void OnDestroy()
	{
		if (this._eventsSet)
		{
			this._eventsSet = false;
			this.UnregisterEvents();
		}
	}

	protected virtual bool ValidateReceivingWorldmodel(FirearmWorldmodel worldmodel)
	{
		return true;
	}

	protected virtual void RegisterEvents()
	{
	}

	protected virtual void UnregisterEvents()
	{
	}

	public void RegisterManager(ServerPlayCallback playCallback)
	{
		this._voicePlayCallback = playCallback;
		base.MarkAsSubmodule();
	}

	public virtual void OnFriendshipCreated()
	{
	}

	public virtual AudioPoolSession? OnVoiceLineRequested(ushort serial, AudioClip clip, NetworkReader extraData)
	{
		Transform trackedTransform;
		if (Scp127VoiceLineManagerModule.TryFindOwner(serial, out var owner))
		{
			if (!this.ValidateReceivingOwner(owner))
			{
				return null;
			}
			trackedTransform = owner.transform;
		}
		else
		{
			if (!FirearmWorldmodel.Instances.TryGetValue(serial, out var value))
			{
				return null;
			}
			if (!this.ValidateReceivingWorldmodel(value))
			{
				return null;
			}
			trackedTransform = value.transform;
		}
		float volume = this.DefaultAudioVolume * this.UserSettingsVoiceScale;
		PooledAudioSource pooledAudioSource = AudioSourcePoolManager.PlayOnTransform(clip, trackedTransform, this.DefaultAudioRange, volume, FalloffType.Exponential, this.DefaultAudioMixerChannel);
		pooledAudioSource.Source.panStereo = this.DefaultAudioStereo;
		return new AudioPoolSession(pooledAudioSource);
	}

	public virtual void OnVoiceLineRejected(ushort serial, AudioClip clip, NetworkReader extraData)
	{
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (!this._eventsSet)
		{
			this._eventsSet = true;
			this.RegisterEvents();
		}
	}

	internal override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (this._eventsSet)
		{
			this._eventsSet = false;
			this.UnregisterEvents();
		}
	}
}
