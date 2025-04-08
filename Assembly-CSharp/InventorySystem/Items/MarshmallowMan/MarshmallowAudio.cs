using System;
using System.Collections.Generic;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.MarshmallowMan
{
	public class MarshmallowAudio : MonoBehaviour
	{
		public void Setup(ushort serial, Transform audioParent)
		{
			this._trackedSerial = serial;
			this._audioParent = audioParent;
		}

		private void OnEnable()
		{
			if (!MarshmallowAudio._eventsSet)
			{
				MarshmallowAudio.SetupEvents();
			}
			MarshmallowAudio.Instances.Add(this);
		}

		private void OnDisable()
		{
			MarshmallowAudio.Instances.Remove(this);
		}

		private void PlaySound(AudioClip clip, float range)
		{
			AudioSourcePoolManager.PlayOnTransform(clip, this._audioParent, range, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f);
		}

		private static void SetupEvents()
		{
			MarshmallowAudio._eventsSet = true;
			AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Combine(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(MarshmallowAudio.OnFootstepPlayed));
			MarshmallowItem.OnHit += MarshmallowAudio.OnHit;
			MarshmallowItem.OnHolsterRequested += MarshmallowAudio.OnHolsterRequested;
			MarshmallowItem.OnSwing += MarshmallowAudio.OnSwing;
		}

		private static void OnSwing(ushort serial)
		{
			MarshmallowAudio marshmallowAudio;
			if (!MarshmallowAudio.TryGetInstance(serial, out marshmallowAudio))
			{
				return;
			}
			marshmallowAudio.PlaySound(marshmallowAudio._swingClips.RandomItem<AudioClip>(), 15f);
		}

		private static void OnHolsterRequested(ushort serial)
		{
			MarshmallowAudio marshmallowAudio;
			if (!MarshmallowAudio.TryGetInstance(serial, out marshmallowAudio))
			{
				return;
			}
			marshmallowAudio.PlaySound(marshmallowAudio._holsterClip, 15f);
		}

		private static void OnHit(ushort serial)
		{
			MarshmallowAudio marshmallowAudio;
			if (!MarshmallowAudio.TryGetInstance(serial, out marshmallowAudio))
			{
				return;
			}
			marshmallowAudio.PlaySound(marshmallowAudio._hitClips.RandomItem<AudioClip>(), 15f);
		}

		private static void OnFootstepPlayed(AnimatedCharacterModel model, float loudness)
		{
			MarshmallowAudio marshmallowAudio;
			if (!MarshmallowAudio.TryGetInstance(model.OwnerHub.inventory.CurItem.SerialNumber, out marshmallowAudio))
			{
				return;
			}
			IFpcRole fpcRole = model.OwnerHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			if (fpcRole.FpcModule.CharacterModelInstance != model)
			{
				return;
			}
			marshmallowAudio.PlaySound(marshmallowAudio._footstepsClips.RandomItem<AudioClip>(), loudness);
		}

		private static bool TryGetInstance(ushort serial, out MarshmallowAudio inst)
		{
			return MarshmallowAudio.Instances.TryGetFirst((MarshmallowAudio x) => x._trackedSerial == serial, out inst);
		}

		[SerializeField]
		private AudioClip _holsterClip;

		[SerializeField]
		private AudioClip[] _hitClips;

		[SerializeField]
		private AudioClip[] _swingClips;

		[SerializeField]
		private AudioClip[] _footstepsClips;

		private ushort _trackedSerial;

		private Transform _audioParent;

		private static readonly List<MarshmallowAudio> Instances = new List<MarshmallowAudio>();

		private static bool _eventsSet;

		private const float StandardRange = 15f;
	}
}
