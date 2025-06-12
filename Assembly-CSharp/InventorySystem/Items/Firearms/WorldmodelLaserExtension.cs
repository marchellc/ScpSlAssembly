using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Thirdperson;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace InventorySystem.Items.Firearms;

[PresetPrefabExtension("Laser Worldmodel", true)]
public class WorldmodelLaserExtension : MonoBehaviour, IWorldmodelExtension
{
	private static readonly CachedLayerMask Mask = new CachedLayerMask("Default", "Hitbox", "Ragdoll", "CCTV", "Grenade", "Door", "InteractableNoPlayerCollision");

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

	private void LateUpdate()
	{
		bool flag = (this._pickupMode ? this.UpdatePickup() : this.UpdateThirdperson());
		this._decal.enabled = flag;
		if (flag)
		{
			this._decalTransform.position = this._lastHit.point;
			float num = (this._range - this._lastHit.distance) * 0.2f;
			for (int i = 0; i < 3; i++)
			{
				num *= num;
			}
			this._decal.fadeFactor = num;
		}
	}

	private void GetOrigin(out Vector3 pos, out Vector3 fwd)
	{
		if (this._hasBarrelTip)
		{
			pos = this._barrelTipExtension.WorldspacePosition;
			fwd = this._barrelTipExtension.WorldspaceDirection;
		}
		else
		{
			fwd = this._originTransform.position;
			pos = this._originTransform.forward;
		}
	}

	private bool UpdatePickup()
	{
		this.GetOrigin(out var pos, out var fwd);
		return this.TryRaycast(pos, fwd, out this._lastHit);
	}

	private bool UpdateThirdperson()
	{
		this.GetOrigin(out var pos, out var fwd);
		if (!this.TryRaycast(pos, fwd, out var hit))
		{
			return false;
		}
		if (hit.distance < 1.3f)
		{
			this._lastHit = hit;
			return true;
		}
		Transform playerCameraReference = this._thirdperson.TargetModel.OwnerHub.PlayerCameraReference;
		Vector3 forward = playerCameraReference.forward;
		Vector3 vector = playerCameraReference.position + forward * 0.8f;
		if (!this.TryRaycast(vector, forward, out var hit2))
		{
			return false;
		}
		float t = Mathf.InverseLerp(1.3f, 3.2f, hit2.distance);
		Vector3 pos2 = Vector3.Lerp(pos, vector, t);
		return this.TryRaycast(pos2, forward, out this._lastHit);
	}

	private bool TryRaycast(Vector3 pos, Vector3 fwd, out RaycastHit hit)
	{
		SetColliders(targetEnabled: false);
		bool flag;
		while (true)
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
		SetColliders(targetEnabled: true);
		return flag;
		void SetColliders(bool targetEnabled)
		{
			int count = this._collidersToDisable.Count;
			for (int i = 0; i < count; i++)
			{
				this._collidersToDisable[i].enabled = targetEnabled;
			}
		}
	}

	public void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		this._originTransform = base.transform;
		this._decalTransform = this._decal.transform;
		this._pickupMode = !this._originTransform.TryGetComponentInParent<FirearmThirdpersonItem>(out this._thirdperson);
		this._range = (this._pickupMode ? 8f : 30f);
		this._hasBarrelTip = worldmodel.TryGetExtension<BarrelTipExtension>(out this._barrelTipExtension);
		if (this._selfColliderInstanceIds == null)
		{
			this._collidersToDisable = new List<Collider>(worldmodel.Colliders.Length);
			this._selfColliderInstanceIds = new HashSet<int>(worldmodel.Colliders.Select((Collider x) => x.GetInstanceID()));
		}
	}
}
