using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Extensions;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DisruptorAudioModule : AudioModule
	{
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
					newSource.FastTransform.SetParent(null, false);
					DisruptorAudioModule.TrackedSource trackedSource = new DisruptorAudioModule.TrackedSource(newSource, id.SerialNumber);
					DisruptorAudioModule.TrackedSources.Enqueue(trackedSource);
					this.TrackOwner(trackedSource);
					return;
				}
			}
		}

		private void TrackOwner(DisruptorAudioModule.TrackedSource trackedSource)
		{
			ushort serial = trackedSource.Serial;
			Transform fastTransform = trackedSource.Session.HandledInstance.FastTransform;
			AudioSource source = trackedSource.Session.Source;
			BarrelTipExtension barrelTipExtension;
			if (BarrelTipExtension.TryFindWorldmodelBarrelTip(serial, out barrelTipExtension))
			{
				fastTransform.position = barrelTipExtension.WorldspacePosition;
				source.Set3D();
				return;
			}
			using (HashSet<AutosyncItem>.Enumerator enumerator = AutosyncItem.Instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.ItemSerial == serial)
					{
						source.Set2D();
						return;
					}
				}
			}
			ReferenceHub referenceHub;
			if (InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
			{
				fastTransform.position = referenceHub.transform.position;
				source.Set3D();
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			this.AllClips.ForEach(new Action<AudioClip>(base.RegisterClip));
			this._disruptorType = base.Firearm.ItemTypeId;
		}

		internal override void OnTemplateReloaded(ModularAutosyncItem template, bool wasEverLoaded)
		{
			base.OnTemplateReloaded(template, wasEverLoaded);
			if (wasEverLoaded)
			{
				return;
			}
			AudioModule.OnSoundPlayed += this.ProcessSound;
		}

		internal override void TemplateUpdate()
		{
			base.TemplateUpdate();
			int count = DisruptorAudioModule.TrackedSources.Count;
			int num = 0;
			DisruptorAudioModule.TrackedSource trackedSource;
			while (num < count && DisruptorAudioModule.TrackedSources.TryDequeue(out trackedSource))
			{
				if (trackedSource.Session.SameSession)
				{
					this.TrackOwner(trackedSource);
					DisruptorAudioModule.TrackedSources.Enqueue(trackedSource);
				}
				num++;
			}
		}

		public void PlayDisruptorShot(bool single, bool last)
		{
			if (single)
			{
				this._singleShotAudio.Play(this, last);
				return;
			}
			this._rapidFireAudio.Play(this, last);
		}

		private static readonly Queue<DisruptorAudioModule.TrackedSource> TrackedSources = new Queue<DisruptorAudioModule.TrackedSource>();

		private static AudioClip[] _allClipsGlobalCache;

		private ItemType _disruptorType;

		[SerializeField]
		private DisruptorAudioModule.FiringModeAudio _singleShotAudio;

		[SerializeField]
		private DisruptorAudioModule.FiringModeAudio _rapidFireAudio;

		private readonly struct TrackedSource
		{
			public TrackedSource(PooledAudioSource src, ushort serial)
			{
				this.Session = new AudioPoolSession(src);
				this.Serial = serial;
			}

			public readonly AudioPoolSession Session;

			public readonly ushort Serial;
		}

		[Serializable]
		public struct FiringModeAudio
		{
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
				audioModule.PlayNormal(last ? this.ClipActionLast : this.ClipActionNormal);
			}

			public const int TotalClips = 4;

			public AudioClip ClipActionNormal;

			public AudioClip ClipActionLast;

			public AudioClip ClipFiringNormal;

			public AudioClip ClipFiringLast;
		}
	}
}
