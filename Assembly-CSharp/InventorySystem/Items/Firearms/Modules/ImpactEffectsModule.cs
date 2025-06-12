using System;
using Decals;
using DrawableLine;
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

namespace InventorySystem.Items.Firearms.Modules;

public class ImpactEffectsModule : ModuleBase
{
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
		private bool? _disableTracers;

		public DecalPoolType BulletholeDecal;

		public DecalPoolType GlassCrackDecal;

		public bool DisableBleeding;

		public TracerBase TracerPrefab;

		public bool DisableTracers
		{
			get
			{
				bool valueOrDefault = this._disableTracers == true;
				if (!this._disableTracers.HasValue)
				{
					valueOrDefault = this.TracerPrefab == null;
					this._disableTracers = valueOrDefault;
					return valueOrDefault;
				}
				return valueOrDefault;
			}
		}
	}

	[Serializable]
	public class ImpactEffectsAttachmentOverride
	{
		private Attachment _attInstance;

		private bool _attSet;

		[field: SerializeField]
		public AttachmentLink Attachment { get; private set; }

		[field: SerializeField]
		public ImpactEffectsSettings Settings { get; private set; }

		public bool GetEnabled(Firearm firearm)
		{
			if (!this._attSet)
			{
				this._attInstance = this.Attachment.GetAttachment(firearm);
				this._attSet = true;
			}
			return this._attInstance.IsEnabled;
		}
	}

	private static readonly CachedLayerMask ReceivingLayers = new CachedLayerMask("Default", "Glass", "Door");

	private const float TracerServerHeightOffset = 0.6f;

	private const float ClientSurfaceSeekerRange = 8f;

	private const int GlassLayer = 14;

	[field: SerializeField]
	public ImpactEffectsSettings BaseSettings { get; private set; }

	[field: SerializeField]
	public ImpactEffectsAttachmentOverride[] AttachmentOverrides { get; private set; }

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.ImpactDecal:
			this.ClientReadImpactDecal(reader);
			break;
		case RpcType.TracerDefault:
			this.ClientReadTracer(reader, serial, null);
			break;
		case RpcType.TracerOverride:
			this.ClientReadTracer(reader, serial, reader.ReadByte());
			break;
		case RpcType.PlayerHit:
			this.ClientReadPlayerHit(reader);
			break;
		}
	}

	public void ServerProcessHit(RaycastHit hit, Vector3 origin, bool anyDamageDealt)
	{
		int? overrideId = null;
		for (int i = 0; i < this.AttachmentOverrides.Length; i++)
		{
			if (this.AttachmentOverrides[i].GetEnabled(base.Firearm))
			{
				overrideId = i;
				break;
			}
		}
		ImpactEffectsSettings settings = this.GetSettings(overrideId);
		if (!settings.DisableTracers)
		{
			this.ServerSendTracer(hit, origin, overrideId, settings.TracerPrefab);
		}
		DrawableLines.GenerateLine(5f, Color.yellow, origin, hit.point);
		Collider collider = hit.collider;
		IDestructible component;
		if (collider.gameObject.layer == 14)
		{
			Vector3 origin2 = hit.point * 2f - origin;
			this.ServerSendImpactDecal(hit, origin, settings.GlassCrackDecal);
			this.ServerSendImpactDecal(hit, origin2, settings.GlassCrackDecal);
		}
		else if (!collider.TryGetComponent<IDestructible>(out component))
		{
			this.ServerSendImpactDecal(hit, origin, settings.BulletholeDecal);
		}
		else if (anyDamageDealt && !settings.DisableBleeding && component is HitboxIdentity hitbox)
		{
			this.ServerSendPlayerHit(hit, origin, hitbox);
		}
	}

	private void ServerSendTracer(RaycastHit hit, Vector3 origin, int? overrideId, TracerBase tracerPrefab)
	{
		RelativePosition relDestination = new RelativePosition(hit.point);
		RelativePosition relOrigin = new RelativePosition(origin + Vector3.up * 0.6f);
		this.SendRpc(delegate(NetworkWriter writer)
		{
			if (overrideId.HasValue)
			{
				writer.WriteSubheader(RpcType.TracerOverride);
				writer.WriteByte((byte)overrideId.Value);
			}
			else
			{
				writer.WriteSubheader(RpcType.TracerDefault);
			}
			writer.WriteRelativePosition(relDestination);
			writer.WriteRelativePosition(relOrigin);
			tracerPrefab.ServerWriteExtraData(base.Firearm, writer);
		});
	}

	private void ClientReadTracer(NetworkReader reader, ushort serial, int? overrideId)
	{
		if (!AutosyncItem.Instances.Any((AutosyncItem x) => x.ItemSerial == serial && x is Firearm firearm && firearm.HasViewmodel))
		{
			RelativePosition hitPosition = reader.ReadRelativePosition();
			if (WaypointBase.TryGetWaypoint(hitPosition.WaypointId, out var wp))
			{
				wp.GetWorldspacePosition(hitPosition.Relative);
				Vector3 position = reader.ReadRelativePosition().Position;
				this.GetSettings(overrideId).TracerPrefab.GetFromPool().Fire(hitPosition, serial, position, reader, base.Firearm);
			}
		}
	}

	private void ServerSendImpactDecal(RaycastHit hit, Vector3 origin, DecalPoolType decalType)
	{
		Vector3 point = hit.point;
		Vector3 startRaycast = hit.point + (origin - hit.point).normalized;
		ReferenceHub owner = base.Firearm.Owner;
		PlayerPlacingBulletHoleEventArgs e = new PlayerPlacingBulletHoleEventArgs(owner, decalType, point, startRaycast);
		PlayerEvents.OnPlacingBulletHole(e);
		if (e.IsAllowed)
		{
			point = e.HitPosition;
			startRaycast = e.RaycastStart;
			RelativePosition hitPoint = new RelativePosition(point);
			RelativePosition startRaycastPoint = new RelativePosition(startRaycast);
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(RpcType.ImpactDecal);
				writer.WriteByte((byte)decalType);
				writer.WriteRelativePosition(hitPoint);
				writer.WriteRelativePosition(startRaycastPoint);
			});
			PlayerEvents.OnPlacedBulletHole(new PlayerPlacedBulletHoleEventArgs(owner, decalType, point, startRaycast));
		}
	}

	private void ClientReadImpactDecal(NetworkReader reader)
	{
		if (UserSetting<bool>.Get(PerformanceVideoSetting.BulletDecalsEnabled))
		{
			DecalPoolType decalType = (DecalPoolType)reader.ReadByte();
			Vector3 position = reader.ReadRelativePosition().Position;
			Vector3 position2 = reader.ReadRelativePosition().Position;
			this.ClientSpawnDecal(position2, position - position2, decalType);
		}
	}

	private void ServerSendPlayerHit(RaycastHit hit, Vector3 origin, HitboxIdentity hitbox)
	{
		Vector3 hitPointVector = hit.point;
		Vector3 startRaycastVector = hit.point + (origin - hit.point).normalized;
		ReferenceHub targetHub = hitbox.TargetHub;
		ReferenceHub owner = base.Firearm.Owner;
		PlayerPlacingBloodEventArgs e = new PlayerPlacingBloodEventArgs(targetHub, owner, hitPointVector, startRaycastVector);
		PlayerEvents.OnPlacingBlood(e);
		if (e.IsAllowed)
		{
			hitPointVector = e.HitPosition;
			startRaycastVector = e.RaycastStart;
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(RpcType.PlayerHit);
				writer.WriteReferenceHub(hitbox.TargetHub);
				RelativePosition msg = new RelativePosition(hitPointVector);
				RelativePosition msg2 = new RelativePosition(startRaycastVector);
				writer.WriteRelativePosition(msg);
				writer.WriteRelativePosition(msg2);
				writer.WriteByte((byte)hitbox.Index);
				writer.WriteRoleType(hitbox.TargetHub.GetRoleId());
			});
			PlayerEvents.OnPlacedBlood(new PlayerPlacedBloodEventArgs(targetHub, owner, hitPointVector, startRaycastVector));
		}
	}

	private void ClientReadPlayerHit(NetworkReader reader)
	{
		if (!reader.TryReadReferenceHub(out var hub))
		{
			return;
		}
		Vector3 position = reader.ReadRelativePosition().Position;
		Vector3 position2 = reader.ReadRelativePosition().Position;
		Vector3 normalized = (position - position2).normalized;
		byte index = reader.ReadByte();
		if (hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			CharacterModel characterModelInstance = fpcRole.FpcModule.CharacterModelInstance;
			if (characterModelInstance.Hitboxes.TryGet(index, out var element))
			{
				characterModelInstance.PlayShotEffects(element, normalized);
			}
		}
		if (UserSetting<bool>.Get(PerformanceVideoSetting.BloodDecalsEnabled) && PlayerRoleLoader.TryGetRoleTemplate<IBleedableRole>(reader.ReadRoleType(), out var result))
		{
			BloodSettings bloodSettings = result.BloodSettings;
			if (bloodSettings.RandomDecalValidate)
			{
				this.ClientSpawnDecal(position2, normalized, bloodSettings.Decal);
			}
		}
	}

	private void ClientSpawnDecal(Vector3 raycastOrigin, Vector3 raycastDirection, DecalPoolType decalType)
	{
		if (Physics.Raycast(raycastOrigin, raycastDirection, out var hitInfo, 8f, ImpactEffectsModule.ReceivingLayers) && DecalPoolManager.TryGet(decalType, out var decal))
		{
			decal.SetRandomRotation();
			decal.AttachToSurface(hitInfo);
		}
	}

	private ImpactEffectsSettings GetSettings(int? overrideId)
	{
		if (!overrideId.HasValue)
		{
			return this.BaseSettings;
		}
		int value = overrideId.Value;
		return this.AttachmentOverrides[value].Settings;
	}
}
