using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Extensions;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class DisruptorAudioModule : AudioModule
{
	private readonly struct TrackedSource
	{
		public readonly AudioPoolSession Session;

		public readonly ushort Serial;

		public TrackedSource(PooledAudioSource src, ushort serial)
		{
			this.Session = new AudioPoolSession(src);
			this.Serial = serial;
		}
	}

	[Serializable]
	public struct FiringModeAudio
	{
		public const int TotalClips = 4;

		public const float ActionDistance = 15f;

		public AudioClip ClipActionNormal;

		public AudioClip ClipActionLast;

		public AudioClip ClipFiringNormal;

		public AudioClip ClipFiringLast;

		public readonly void Cache(int startIndex)
		{
			DisruptorAudioModule._allClipsGlobalCache[startIndex] = this.ClipActionNormal;
			DisruptorAudioModule._allClipsGlobalCache[startIndex + 1] = this.ClipActionLast;
			DisruptorAudioModule._allClipsGlobalCache[startIndex + 2] = this.ClipFiringNormal;
			DisruptorAudioModule._allClipsGlobalCache[startIndex + 3] = this.ClipFiringLast;
		}

		public readonly void Play(AudioModule audioModule, bool last)
		{
			audioModule.PlayGunshot(last ? this.ClipFiringLast : this.ClipFiringNormal);
			audioModule.PlayCustom(last ? this.ClipActionLast : this.ClipActionNormal, MixerChannel.NoDucking, 15f, applyPitch: false);
		}
	}

	private static readonly Queue<TrackedSource> TrackedSources = new Queue<TrackedSource>();

	private static AudioClip[] _allClipsGlobalCache;

	private ItemType _disruptorType;

	[SerializeField]
	private FiringModeAudio _singleShotAudio;

	[SerializeField]
	private FiringModeAudio _rapidFireAudio;

	private AudioClip[] AllClips
	{
		get
		{
			if (DisruptorAudioModule._allClipsGlobalCache == null)
			{
				DisruptorAudioModule._allClipsGlobalCache = new AudioClip[8];
				this._singleShotAudio.Cache(0);
				this._rapidFireAudio.Cache(4);
			}
			return DisruptorAudioModule._allClipsGlobalCache;
		}
	}

	private void ProcessSound(ItemIdentifier id, PlayerRoleBase role, PooledAudioSource newSource)
	{
		if (id.TypeId != this._disruptorType)
		{
			return;
		}
		AudioClip clip = newSource.Source.clip;
		AudioClip[] allClips = this.AllClips;
		for (int i = 0; i < allClips.Length; i++)
		{
			if (!(allClips[i] != clip))
			{
				newSource.FastTransform.SetParent(null, worldPositionStays: false);
				TrackedSource trackedSource = new TrackedSource(newSource, id.SerialNumber);
				DisruptorAudioModule.TrackedSources.Enqueue(trackedSource);
				this.TrackOwner(trackedSource);
				break;
			}
		}
	}

	private void TrackOwner(TrackedSource trackedSource)
	{
		ushort serial = trackedSource.Serial;
		Transform fastTransform = trackedSource.Session.HandledInstance.FastTransform;
		AudioSource source = trackedSource.Session.Source;
		if (BarrelTipExtension.TryFindWorldmodelBarrelTip(serial, out var foundExtension))
		{
			fastTransform.position = foundExtension.WorldspacePosition;
			source.Set3D();
			return;
		}
		foreach (AutosyncItem instance in AutosyncItem.Instances)
		{
			if (instance.ItemSerial == serial)
			{
				source.Set2D();
				return;
			}
		}
		if (InventoryExtensions.TryGetHubHoldingSerial(serial, out var hub))
		{
			fastTransform.position = hub.transform.position;
			source.Set3D();
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		this.AllClips.ForEach(base.RegisterClip);
		this._disruptorType = base.Firearm.ItemTypeId;
	}

	internal override void OnTemplateReloaded(ModularAutosyncItem template, bool wasEverLoaded)
	{
		base.OnTemplateReloaded(template, wasEverLoaded);
		if (!wasEverLoaded)
		{
			AudioModule.OnSoundPlayed += ProcessSound;
		}
	}

	internal override void TemplateUpdate()
	{
		base.TemplateUpdate();
		int count = DisruptorAudioModule.TrackedSources.Count;
		for (int i = 0; i < count; i++)
		{
			if (!DisruptorAudioModule.TrackedSources.TryDequeue(out var result))
			{
				break;
			}
			if (result.Session.SameSession)
			{
				this.TrackOwner(result);
				DisruptorAudioModule.TrackedSources.Enqueue(result);
			}
		}
	}

	public void PlayDisruptorShot(bool single, bool last)
	{
		if (single)
		{
			this._singleShotAudio.Play(this, last);
		}
		else
		{
			this._rapidFireAudio.Play(this, last);
		}
	}

	public void StopDisruptorShot()
	{
		foreach (TrackedSource trackedSource in DisruptorAudioModule.TrackedSources)
		{
			if (trackedSource.Serial == base.Firearm.ItemSerial && trackedSource.Session.SameSession)
			{
				trackedSource.Session.Source.Stop();
			}
		}
	}
}
