using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.Thirdperson;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerRoles.Visibility;
using RelativePositioning;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AudioModule : ModuleBase
	{
		public static event Action<ItemIdentifier, PlayerRoleBase, PooledAudioSource> OnSoundPlayed;

		public float BaseGunshotRange { get; private set; }

		public float FinalGunshotRange
		{
			get
			{
				return this.BaseGunshotRange * base.Firearm.AttachmentsValue(AttachmentParam.GunshotLoudnessMultiplier);
			}
		}

		private float RandomPitch
		{
			get
			{
				float gunshotSoundRandomization = this._gunshotSoundRandomization;
				float num = global::UnityEngine.Random.Range(-gunshotSoundRandomization, gunshotSoundRandomization);
				return 1f + num;
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
			{
				return;
			}
			this.ClientReceiveThirdperson(serial, referenceHub.roleManager.CurrentRole, reader);
		}

		[ExposedFirearmEvent(UnityEventCallState.EditorAndRuntime)]
		public void PlayQuiet(AudioClip clip)
		{
			this.ProcessEvent(clip, MixerChannel.DefaultSfx, 5f, true, true);
		}

		[ExposedFirearmEvent(UnityEventCallState.EditorAndRuntime)]
		public void PlayNormal(AudioClip clip)
		{
			this.ProcessEvent(clip, MixerChannel.DefaultSfx, 12f, true, true);
		}

		[ExposedFirearmEvent(UnityEventCallState.EditorAndRuntime)]
		public void PlayClientside(AudioClip clip)
		{
			this.ProcessEvent(clip, MixerChannel.DefaultSfx, 5f, false, true);
		}

		public void PlayGunshot(AudioClip clip)
		{
			this.ProcessEvent(clip, MixerChannel.Weapons, this.FinalGunshotRange, true, false);
		}

		internal void RegisterClip(AudioClip clip)
		{
			if (clip == null)
			{
				return;
			}
			if (this._clipToIndex.ContainsKey(clip))
			{
				return;
			}
			this._registeredClips.Add(clip);
			this._clipToIndex[clip] = this._registeredClips.Count - 1;
		}

		protected override void OnInit()
		{
			base.OnInit();
			int count = this._registeredClips.Count;
			foreach (AudioClip audioClip in this._eventClips)
			{
				this._registeredClips.Add(audioClip);
				this._clipToIndex[audioClip] = count++;
			}
		}

		private void ProcessEvent(AudioClip clip, MixerChannel mixerChannel, float audioRange, bool sync, bool applyPitch)
		{
			FirearmEvent currentlyInvokedEvent = FirearmEvent.CurrentlyInvokedEvent;
			float num = ((applyPitch && currentlyInvokedEvent != null) ? currentlyInvokedEvent.LastInvocation.ParamSpeed : 1f);
			if (base.HasViewmodel)
			{
				if (base.IsSpectator && sync)
				{
					return;
				}
				EventManagerModule eventManagerModule;
				if (base.Firearm.TryGetModule(out eventManagerModule, true) && eventManagerModule.SkippingForward)
				{
					return;
				}
				float num2 = num;
				Transform transform;
				if (mixerChannel == MixerChannel.Weapons)
				{
					num2 *= this.RandomPitch;
					transform = base.Firearm.Owner.transform;
				}
				else
				{
					transform = base.Firearm.ViewModel.transform;
				}
				PooledAudioSource pooledAudioSource = AudioSourcePoolManager.Play2DWithParent(clip, transform, 1f, mixerChannel, num2);
				pooledAudioSource.Source.maxDistance = audioRange;
				Action<ItemIdentifier, PlayerRoleBase, PooledAudioSource> onSoundPlayed = AudioModule.OnSoundPlayed;
				if (onSoundPlayed != null)
				{
					onSoundPlayed(new ItemIdentifier(base.Firearm), base.Firearm.Owner.roleManager.CurrentRole, pooledAudioSource);
				}
			}
			if (!base.IsServer || !sync)
			{
				return;
			}
			int num3;
			if (!this._clipToIndex.TryGetValue(clip, out num3))
			{
				return;
			}
			this.ServerSendToNearbyPlayers(num3, mixerChannel, audioRange, num);
		}

		private void ServerSendToNearbyPlayers(int index, MixerChannel channel, float audioRange, float pitch)
		{
			IFpcRole fpcRole = base.Firearm.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			float num = audioRange + 20f;
			float num2 = num * num;
			Vector3 ownPos = fpcRole.FpcModule.Position;
			using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ReferenceHub receiver = enumerator.Current;
					if (!(receiver == base.Firearm.Owner))
					{
						IFpcRole fpcRole2 = receiver.roleManager.CurrentRole as IFpcRole;
						if (fpcRole2 == null || fpcRole2.SqrDistanceTo(ownPos) <= num2)
						{
							this.SendRpc(receiver, delegate(NetworkWriter writer)
							{
								ICustomVisibilityRole customVisibilityRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
								bool flag = customVisibilityRole == null || customVisibilityRole.VisibilityController.ValidateVisibility(this.Firearm.Owner);
								this.ServerSend(writer, index, pitch, channel, audioRange, ownPos, flag);
							});
						}
					}
				}
			}
		}

		private void ServerSend(NetworkWriter writer, int index, float pitch, MixerChannel channel, float range, Vector3 shooterPosition, bool shooterVisible)
		{
			writer.WriteByte((byte)index);
			writer.WriteByte((byte)channel);
			writer.WriteUShort(Misc.RoundToUShort(range * 20f));
			writer.WriteByte(Misc.RoundToByte(Mathf.InverseLerp(0.3f, 3f, pitch) * 255f));
			if (shooterVisible)
			{
				return;
			}
			writer.WriteRelativePosition(new RelativePosition(shooterPosition));
		}

		private void ClientReceiveThirdperson(ushort serial, PlayerRoleBase shooter, NetworkReader reader)
		{
			byte b = reader.ReadByte();
			MixerChannel mixerChannel = (MixerChannel)reader.ReadByte();
			float num = (float)reader.ReadUShort() / 20f;
			float num2 = Mathf.Lerp(0.3f, 3f, (float)reader.ReadByte() / 255f);
			AudioClip audioClip;
			if (!this._registeredClips.TryGet((int)b, out audioClip))
			{
				return;
			}
			if (mixerChannel == MixerChannel.Weapons)
			{
				num2 *= this.RandomPitch;
			}
			IFpcRole fpcRole = shooter as IFpcRole;
			PooledAudioSource pooledAudioSource;
			if (fpcRole != null && fpcRole.FpcModule.Motor.IsInvisible && reader.Remaining > 0)
			{
				RelativePosition relativePosition = reader.ReadRelativePosition();
				pooledAudioSource = AudioSourcePoolManager.PlayAtPosition(audioClip, relativePosition, num, 1f, FalloffType.Exponential, mixerChannel, num2);
			}
			else
			{
				pooledAudioSource = AudioSourcePoolManager.PlayOnTransform(audioClip, this.GetAudioSourceParent(mixerChannel, shooter), num, 1f, FalloffType.Exponential, mixerChannel, num2);
			}
			if (mixerChannel == MixerChannel.Weapons)
			{
				float num3 = MainCameraController.CurrentCamera.position.y - pooledAudioSource.FastTransform.position.y;
				float num4 = Mathf.Clamp01(num3 * num3 / 5000f);
				pooledAudioSource.Source.volume *= 1f - num4;
			}
			Action<ItemIdentifier, PlayerRoleBase, PooledAudioSource> onSoundPlayed = AudioModule.OnSoundPlayed;
			if (onSoundPlayed == null)
			{
				return;
			}
			onSoundPlayed(new ItemIdentifier(base.Firearm.ItemTypeId, serial), shooter, pooledAudioSource);
		}

		private Transform GetAudioSourceParent(MixerChannel mixerChannel, PlayerRoleBase shooter)
		{
			Transform transform = shooter.transform;
			if (mixerChannel != MixerChannel.Weapons)
			{
				IFpcRole fpcRole = shooter as IFpcRole;
				if (fpcRole != null)
				{
					AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
					if (animatedCharacterModel == null)
					{
						return transform;
					}
					InventorySubcontroller inventorySubcontroller;
					if (!animatedCharacterModel.TryGetSubcontroller<InventorySubcontroller>(out inventorySubcontroller))
					{
						return transform;
					}
					FirearmThirdpersonItem firearmThirdpersonItem;
					if (!inventorySubcontroller.TryGetCurrentInstance<FirearmThirdpersonItem>(out firearmThirdpersonItem))
					{
						return transform;
					}
					return firearmThirdpersonItem.transform;
				}
			}
			return transform;
		}

		private const float SyncMinPitch = 0.3f;

		private const float SyncMaxPitch = 3f;

		private const float NearRange = 5f;

		private const float MidRange = 12f;

		private const float MaxHeightLoudnessSqr = 5000f;

		private const float SendDistanceBuffer = 20f;

		private const float SyncRangeAccuracy = 20f;

		[SerializeField]
		private float _gunshotSoundRandomization;

		[SerializeField]
		private AudioClip[] _eventClips;

		private readonly List<AudioClip> _registeredClips = new List<AudioClip>(64);

		private readonly Dictionary<AudioClip, int> _clipToIndex = new Dictionary<AudioClip, int>();
	}
}
