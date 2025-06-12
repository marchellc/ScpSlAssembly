using CameraShaking;
using InventorySystem;
using InventorySystem.Items;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl;

public class FpcSpectatableModule : SpectatableModuleBase, IViewmodelRole
{
	private FirstPersonMovementModule FpcModule => (base.MainRole as IFpcRole).FpcModule;

	public ItemViewmodelBase SpawnedViewmodel { get; private set; }

	public override Vector3 CameraPosition
	{
		get
		{
			if (!(base.MainRole is ICameraController cameraController))
			{
				return base.TargetHub.PlayerCameraReference.position;
			}
			return cameraController.CameraPosition;
		}
	}

	public override Vector3 CameraRotation
	{
		get
		{
			if (base.MainRole is ICameraController cameraController)
			{
				float z = ((cameraController is IAdvancedCameraController advancedCameraController) ? advancedCameraController.RollRotation : 0f);
				return new Vector3(cameraController.VerticalRotation, cameraController.HorizontalRotation, z);
			}
			return base.TargetHub.PlayerCameraReference.rotation.eulerAngles;
		}
	}

	internal override void OnBeganSpectating()
	{
		this.FpcModule.CharacterModelInstance.SetVisibility(newState: false);
		Inventory.OnCurrentItemChanged += OnCurrentItemChanged;
		ItemIdentifier curItem = base.TargetHub.inventory.CurItem;
		this.OnCurrentItemChanged(base.TargetHub, curItem, curItem);
		SharedHandsController.SetRoleGloves(base.TargetHub.GetRoleId());
		if (this.FpcModule.CharacterModelInstance is AnimatedCharacterModel model)
		{
			CameraShakeController.AddEffect(new HeadbobShake(model));
		}
	}

	internal override void OnStoppedSpectating()
	{
		CharacterModel characterModelInstance = this.FpcModule.CharacterModelInstance;
		ReferenceHub ownerHub = characterModelInstance.OwnerHub;
		if (!(ownerHub != null) || !ownerHub.isLocalPlayer)
		{
			characterModelInstance.SetVisibility(newState: true);
		}
		Inventory.OnCurrentItemChanged -= OnCurrentItemChanged;
		if (this.SpawnedViewmodel != null)
		{
			Object.Destroy(this.SpawnedViewmodel.gameObject);
		}
		SharedHandsController.UpdateInstance(null);
	}

	private void OnCurrentItemChanged(ReferenceHub hub, ItemIdentifier oldItem, ItemIdentifier newItem)
	{
		if (base.MainRole.TryGetOwner(out var hub2) && !(hub != hub2))
		{
			if (this.SpawnedViewmodel != null)
			{
				Object.Destroy(this.SpawnedViewmodel.gameObject);
			}
			if (InventoryItemLoader.TryGetItem<ItemBase>(newItem.TypeId, out var result) && result.ViewModel != null)
			{
				this.SpawnedViewmodel = Object.Instantiate(result.ViewModel, SharedHandsController.Singleton.transform);
				SharedHandsController.UpdateInstance(this.SpawnedViewmodel);
				this.SpawnedViewmodel.InitSpectator(hub, newItem, oldItem == newItem);
			}
			else
			{
				this.SpawnedViewmodel = null;
				SharedHandsController.UpdateInstance(null);
			}
		}
	}

	public bool TryGetViewmodelFov(out float fov)
	{
		if (this.SpawnedViewmodel != null)
		{
			fov = this.SpawnedViewmodel.ViewmodelCameraFOV;
			return true;
		}
		if (base.MainRole is IViewmodelRole viewmodelRole)
		{
			return viewmodelRole.TryGetViewmodelFov(out fov);
		}
		fov = 0f;
		return false;
	}
}
