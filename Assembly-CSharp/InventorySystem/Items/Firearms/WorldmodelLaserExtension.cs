using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Thirdperson;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace InventorySystem.Items.Firearms
{
	[PresetPrefabExtension("Laser Worldmodel", true)]
	public class WorldmodelLaserExtension : MonoBehaviour, IWorldmodelExtension
	{
		private void LateUpdate()
		{
			bool flag = (this._pickupMode ? this.UpdatePickup() : this.UpdateThirdperson());
			this._decal.enabled = flag;
			if (!flag)
			{
				return;
			}
			this._decalTransform.position = this._lastHit.point;
			float num = (this._range - this._lastHit.distance) * 0.2f;
			for (int i = 0; i < 3; i++)
			{
				num *= num;
			}
			this._decal.fadeFactor = num;
		}

		private void GetOrigin(out Vector3 pos, out Vector3 fwd)
		{
			if (this._hasBarrelTip)
			{
				pos = this._barrelTipExtension.WorldspacePosition;
				fwd = this._barrelTipExtension.WorldspaceDirection;
				return;
			}
			fwd = this._originTransform.position;
			pos = this._originTransform.forward;
		}

		private bool UpdatePickup()
		{
			Vector3 vector;
			Vector3 vector2;
			this.GetOrigin(out vector, out vector2);
			return this.TryRaycast(vector, vector2, out this._lastHit);
		}

		private bool UpdateThirdperson()
		{
			Vector3 vector;
			Vector3 vector2;
			this.GetOrigin(out vector, out vector2);
			RaycastHit raycastHit;
			if (!this.TryRaycast(vector, vector2, out raycastHit))
			{
				return false;
			}
			if (raycastHit.distance < 1.3f)
			{
				this._lastHit = raycastHit;
				return true;
			}
			Transform playerCameraReference = this._thirdperson.TargetModel.OwnerHub.PlayerCameraReference;
			Vector3 forward = playerCameraReference.forward;
			Vector3 vector3 = playerCameraReference.position + forward * 0.8f;
			RaycastHit raycastHit2;
			if (!this.TryRaycast(vector3, forward, out raycastHit2))
			{
				return false;
			}
			float num = Mathf.InverseLerp(1.3f, 3.2f, raycastHit2.distance);
			Vector3 vector4 = Vector3.Lerp(vector, vector3, num);
			return this.TryRaycast(vector4, forward, out this._lastHit);
		}

		private bool TryRaycast(Vector3 pos, Vector3 fwd, out RaycastHit hit)
		{
			this.<TryRaycast>g__SetColliders|24_0(false);
			bool flag;
			for (;;)
			{
				flag = Physics.Raycast(pos, fwd, out hit, this._range, WorldmodelLaserExtension.Mask);
				if (!flag || hit.distance > 1.5f || !this._selfColliderInstanceIds.Contains(hit.colliderInstanceID))
				{
					break;
				}
				Collider collider = hit.collider;
				collider.enabled = false;
				this._collidersToDisable.Add(collider);
			}
			this.<TryRaycast>g__SetColliders|24_0(true);
			return flag;
		}

		public void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			this._originTransform = base.transform;
			this._decalTransform = this._decal.transform;
			this._pickupMode = !this._originTransform.TryGetComponentInParent(out this._thirdperson);
			this._range = (this._pickupMode ? 8f : 30f);
			this._hasBarrelTip = worldmodel.TryGetExtension<BarrelTipExtension>(out this._barrelTipExtension);
			if (this._selfColliderInstanceIds != null)
			{
				return;
			}
			this._collidersToDisable = new List<Collider>(worldmodel.Colliders.Length);
			this._selfColliderInstanceIds = new HashSet<int>(worldmodel.Colliders.Select((Collider x) => x.GetInstanceID()));
		}

		[CompilerGenerated]
		private void <TryRaycast>g__SetColliders|24_0(bool targetEnabled)
		{
			int count = this._collidersToDisable.Count;
			for (int i = 0; i < count; i++)
			{
				this._collidersToDisable[i].enabled = targetEnabled;
			}
		}

		private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[] { "Default", "Hitbox", "Ragdoll", "CCTV", "Grenade", "Door", "InteractableNoPlayerCollision" });

		private const float LaserPickupRange = 8f;

		private const float LaserThirdpersonRange = 30f;

		private const float FadeDistance = 5f;

		private const float ThirdpersonCamBiasMin = 1.3f;

		private const float ThirdpersonCamBiasMax = 3.2f;

		private const float CameraFwdOffset = 0.8f;

		private const float PotentialCollisionDistance = 1.5f;

		private const int FadePower = 3;

		private FirearmThirdpersonItem _thirdperson;

		private HashSet<int> _selfColliderInstanceIds;

		private List<Collider> _collidersToDisable;

		private Transform _originTransform;

		private Transform _decalTransform;

		private bool _pickupMode;

		private RaycastHit _lastHit;

		private float _range;

		private bool _hasBarrelTip;

		private BarrelTipExtension _barrelTipExtension;

		[SerializeField]
		private DecalProjector _decal;
	}
}
