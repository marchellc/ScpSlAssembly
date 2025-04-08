using System;
using Decals;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules.Misc;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.Blood;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using RelativePositioning;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules
{
	public class ImpactEffectsModule : ModuleBase
	{
		public ImpactEffectsModule.ImpactEffectsSettings BaseSettings { get; private set; }

		public ImpactEffectsModule.ImpactEffectsAttachmentOverride[] AttachmentOverrides { get; private set; }

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				this.ClientReadImpactDecal(reader);
				return;
			case 1:
				this.ClientReadTracer(reader, serial, null);
				return;
			case 2:
				this.ClientReadTracer(reader, serial, new int?((int)reader.ReadByte()));
				return;
			case 3:
				this.ClientReadPlayerHit(reader);
				return;
			default:
				return;
			}
		}

		public void ServerProcessHit(RaycastHit hit, Vector3 origin, bool anyDamageDealt)
		{
			int? num = null;
			for (int i = 0; i < this.AttachmentOverrides.Length; i++)
			{
				if (this.AttachmentOverrides[i].GetEnabled(base.Firearm))
				{
					num = new int?(i);
					break;
				}
			}
			ImpactEffectsModule.ImpactEffectsSettings settings = this.GetSettings(num);
			if (!settings.DisableTracers)
			{
				this.ServerSendTracer(hit, origin, num, settings.TracerPrefab);
			}
			Collider collider = hit.collider;
			if (collider.gameObject.layer == 14)
			{
				Vector3 vector = hit.point * 2f - origin;
				this.ServerSendImpactDecal(hit, origin, settings.GlassCrackDecal);
				this.ServerSendImpactDecal(hit, vector, settings.GlassCrackDecal);
				return;
			}
			IDestructible destructible;
			if (!collider.TryGetComponent<IDestructible>(out destructible))
			{
				this.ServerSendImpactDecal(hit, origin, settings.BulletholeDecal);
				return;
			}
			if (anyDamageDealt && !settings.DisableBleeding)
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if (hitboxIdentity != null)
				{
					this.ServerSendPlayerHit(hit, origin, hitboxIdentity);
				}
			}
		}

		private void ServerSendTracer(RaycastHit hit, Vector3 origin, int? overrideId, TracerBase tracerPrefab)
		{
			RelativePosition relDestination = new RelativePosition(hit.point);
			RelativePosition relOrigin = new RelativePosition(origin + Vector3.up * 0.6f);
			this.SendRpc(delegate(NetworkWriter writer)
			{
				if (overrideId != null)
				{
					writer.WriteSubheader(ImpactEffectsModule.RpcType.TracerOverride);
					writer.WriteByte((byte)overrideId.Value);
				}
				else
				{
					writer.WriteSubheader(ImpactEffectsModule.RpcType.TracerDefault);
				}
				writer.WriteRelativePosition(relDestination);
				writer.WriteRelativePosition(relOrigin);
				tracerPrefab.ServerWriteExtraData(this.Firearm, writer);
			}, true);
		}

		private void ClientReadTracer(NetworkReader reader, ushort serial, int? overrideId)
		{
			if (AutosyncItem.Instances.Any(delegate(AutosyncItem x)
			{
				if (x.ItemSerial == serial)
				{
					Firearm firearm = x as Firearm;
					return firearm != null && firearm.HasViewmodel;
				}
				return false;
			}))
			{
				return;
			}
			RelativePosition relativePosition = reader.ReadRelativePosition();
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(relativePosition.WaypointId, out waypointBase))
			{
				return;
			}
			waypointBase.GetWorldspacePosition(relativePosition.Relative);
			Vector3 position = reader.ReadRelativePosition().Position;
			this.GetSettings(overrideId).TracerPrefab.GetFromPool().Fire(relativePosition, serial, position, reader, base.Firearm);
		}

		private void ServerSendImpactDecal(RaycastHit hit, Vector3 origin, DecalPoolType decalType)
		{
			Vector3 vector = hit.point;
			Vector3 vector2 = hit.point + (origin - hit.point).normalized;
			ReferenceHub owner = base.Firearm.Owner;
			PlayerPlacingBulletHoleEventArgs playerPlacingBulletHoleEventArgs = new PlayerPlacingBulletHoleEventArgs(owner, decalType, vector, vector2);
			PlayerEvents.OnPlacingBulletHole(playerPlacingBulletHoleEventArgs);
			if (!playerPlacingBulletHoleEventArgs.IsAllowed)
			{
				return;
			}
			vector = playerPlacingBulletHoleEventArgs.HitPosition;
			vector2 = playerPlacingBulletHoleEventArgs.RaycastStart;
			RelativePosition hitPoint = new RelativePosition(vector);
			RelativePosition startRaycastPoint = new RelativePosition(vector2);
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(ImpactEffectsModule.RpcType.ImpactDecal);
				writer.WriteByte((byte)decalType);
				writer.WriteRelativePosition(hitPoint);
				writer.WriteRelativePosition(startRaycastPoint);
			}, true);
			PlayerEvents.OnPlacedBlood(new PlayerPlacedBloodEventArgs(owner, hit.point, vector2));
		}

		private void ClientReadImpactDecal(NetworkReader reader)
		{
			if (!UserSetting<bool>.Get<PerformanceVideoSetting>(PerformanceVideoSetting.BulletDecalsEnabled))
			{
				return;
			}
			DecalPoolType decalPoolType = (DecalPoolType)reader.ReadByte();
			Vector3 position = reader.ReadRelativePosition().Position;
			Vector3 position2 = reader.ReadRelativePosition().Position;
			this.ClientSpawnDecal(position2, position - position2, decalPoolType);
		}

		private void ServerSendPlayerHit(RaycastHit hit, Vector3 origin, HitboxIdentity hitbox)
		{
			Vector3 hitPointVector = hit.point;
			Vector3 startRaycastVector = hit.point + (origin - hit.point).normalized;
			ReferenceHub owner = base.Firearm.Owner;
			PlayerPlacingBloodEventArgs playerPlacingBloodEventArgs = new PlayerPlacingBloodEventArgs(owner, hitPointVector, startRaycastVector);
			PlayerEvents.OnPlacingBlood(playerPlacingBloodEventArgs);
			if (!playerPlacingBloodEventArgs.IsAllowed)
			{
				return;
			}
			hitPointVector = playerPlacingBloodEventArgs.HitPosition;
			startRaycastVector = playerPlacingBloodEventArgs.RaycastStart;
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(ImpactEffectsModule.RpcType.PlayerHit);
				writer.WriteReferenceHub(hitbox.TargetHub);
				RelativePosition relativePosition = new RelativePosition(hitPointVector);
				RelativePosition relativePosition2 = new RelativePosition(startRaycastVector);
				writer.WriteRelativePosition(relativePosition);
				writer.WriteRelativePosition(relativePosition2);
				writer.WriteByte((byte)hitbox.Index);
			}, true);
			PlayerEvents.OnPlacedBlood(new PlayerPlacedBloodEventArgs(owner, hitPointVector, startRaycastVector));
		}

		private void ClientReadPlayerHit(NetworkReader reader)
		{
			ReferenceHub referenceHub;
			if (!reader.TryReadReferenceHub(out referenceHub))
			{
				return;
			}
			PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
			IFpcRole fpcRole = currentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			Vector3 position = reader.ReadRelativePosition().Position;
			Vector3 position2 = reader.ReadRelativePosition().Position;
			Vector3 normalized = (position - position2).normalized;
			CharacterModel characterModelInstance = fpcRole.FpcModule.CharacterModelInstance;
			HitboxIdentity hitboxIdentity;
			if (characterModelInstance.Hitboxes.TryGet((int)reader.ReadByte(), out hitboxIdentity))
			{
				characterModelInstance.PlayShotEffects(hitboxIdentity, normalized);
			}
			if (UserSetting<bool>.Get<PerformanceVideoSetting>(PerformanceVideoSetting.BloodDecalsEnabled))
			{
				IBleedableRole bleedableRole = currentRole as IBleedableRole;
				if (bleedableRole != null)
				{
					BloodSettings bloodSettings = bleedableRole.BloodSettings;
					if (!bloodSettings.RandomDecalValidate)
					{
						return;
					}
					this.ClientSpawnDecal(position2, normalized, bloodSettings.Decal);
					return;
				}
			}
		}

		private void ClientSpawnDecal(Vector3 raycastOrigin, Vector3 raycastDirection, DecalPoolType decalType)
		{
			RaycastHit raycastHit;
			if (!Physics.Raycast(raycastOrigin, raycastDirection, out raycastHit, 8f, ImpactEffectsModule.ReceivingLayers))
			{
				return;
			}
			Decal decal;
			if (!DecalPoolManager.TryGet(decalType, out decal))
			{
				return;
			}
			decal.SetRandomRotation();
			decal.AttachToSurface(raycastHit);
		}

		private ImpactEffectsModule.ImpactEffectsSettings GetSettings(int? overrideId)
		{
			if (overrideId == null)
			{
				return this.BaseSettings;
			}
			int value = overrideId.Value;
			return this.AttachmentOverrides[value].Settings;
		}

		private static readonly CachedLayerMask ReceivingLayers = new CachedLayerMask(new string[] { "Default", "Glass", "Door" });

		private const float TracerServerHeightOffset = 0.6f;

		private const float ClientSurfaceSeekerRange = 8f;

		private const int GlassLayer = 14;

		private enum RpcType
		{
			ImpactDecal,
			TracerDefault,
			TracerOverride,
			PlayerHit
		}

		[Serializable]
		public class ImpactEffectsSettings
		{
			public bool DisableTracers
			{
				get
				{
					bool flag = this._disableTracers.GetValueOrDefault();
					if (this._disableTracers == null)
					{
						flag = this.TracerPrefab == null;
						this._disableTracers = new bool?(flag);
						return flag;
					}
					return flag;
				}
			}

			private bool? _disableTracers;

			public DecalPoolType BulletholeDecal;

			public DecalPoolType GlassCrackDecal;

			public bool DisableBleeding;

			public TracerBase TracerPrefab;
		}

		[Serializable]
		public class ImpactEffectsAttachmentOverride
		{
			public AttachmentLink Attachment { get; private set; }

			public ImpactEffectsModule.ImpactEffectsSettings Settings { get; private set; }

			public bool GetEnabled(Firearm firearm)
			{
				if (!this._attSet)
				{
					this._attInstance = this.Attachment.GetAttachment(firearm);
					this._attSet = true;
				}
				return this._attInstance.IsEnabled;
			}

			private Attachment _attInstance;

			private bool _attSet;
		}
	}
}
