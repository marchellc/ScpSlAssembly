using InventorySystem.Drawers;
using InventorySystem.Items.Pickups;
using PlayerRoles.Ragdolls;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace InventorySystem.Items.DebugTools;

public class RagdollMover : ItemBase, IItemAlertDrawer, IItemDrawer, ICustomRADisplay, IItemDescription, IItemNametag
{
	private static readonly CachedLayerMask DetectionMask = new CachedLayerMask("Default", "Ragdoll");

	private const float MaxRaycastDis = 10f;

	[SerializeField]
	private float _proportionalForce;

	[SerializeField]
	private float _maxForce;

	private Rigidbody _hitRb;

	private Vector3 _hitLocalPos;

	private float _hitDist;

	private bool _hasHit;

	public override float Weight => 1f;

	public AlertContent Alert => new AlertContent($"Press and hold ${new ReadableKeyCode(this.TriggerKey)}$ to move a ragdoll bone.\nClient-side only - changes are not synchronized with other players.");

	public string DisplayName => "Ragdoll Mover";

	public bool CanBeDisplayed => false;

	public string Description => "Debug Tool\n(no official support)";

	public string Name => this.DisplayName;

	private KeyCode TriggerKey => NewInput.GetKey(ActionName.Shoot);

	private KeyCode FreezeKey => NewInput.GetKey(ActionName.Zoom);

	private Transform Cam => base.Owner.PlayerCameraReference;

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (this.IsLocalPlayer)
		{
			if (Input.GetKeyDown(this.TriggerKey))
			{
				this._hasHit = this.TryFind();
			}
			if (this._hasHit && Input.GetKey(this.TriggerKey))
			{
				this.UpdateMoving();
			}
		}
	}

	private bool TryFind()
	{
		if (!Physics.Raycast(this.Cam.position, this.Cam.forward, out var hitInfo, 10f, RagdollMover.DetectionMask))
		{
			return false;
		}
		this._hitRb = hitInfo.rigidbody;
		if (this._hitRb == null)
		{
			return false;
		}
		this._hitRb.isKinematic = false;
		this._hitDist = hitInfo.distance;
		this._hitLocalPos = this._hitRb.transform.InverseTransformPoint(hitInfo.point);
		BasicRagdoll comp;
		return this._hitRb.transform.TryGetComponentInParent<BasicRagdoll>(out comp);
	}

	private void UpdateMoving()
	{
		if (this._hitRb == null)
		{
			this._hasHit = false;
			return;
		}
		if (Input.GetKeyDown(this.FreezeKey))
		{
			this._hitRb.isKinematic = !this._hitRb.isKinematic;
		}
		Transform transform = this._hitRb.transform;
		Vector3 vector = this.Cam.position + this.Cam.forward * this._hitDist;
		Vector3 vector2 = transform.TransformPoint(this._hitLocalPos);
		Vector3 vector3 = vector - vector2;
		float num = vector3.magnitude * this._proportionalForce;
		if (num < this._maxForce)
		{
			this._hitRb.AddForceAtPosition(vector3 * num, vector, ForceMode.Force);
		}
		else
		{
			transform.position += vector3;
		}
	}

	public override ItemPickupBase ServerDropItem(bool spawn)
	{
		base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		return null;
	}
}
