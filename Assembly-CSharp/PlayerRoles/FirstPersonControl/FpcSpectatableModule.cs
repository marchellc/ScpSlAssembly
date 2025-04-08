using System;
using CameraShaking;
using InventorySystem;
using InventorySystem.Items;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcSpectatableModule : SpectatableModuleBase, IViewmodelRole
	{
		private FirstPersonMovementModule FpcModule
		{
			get
			{
				return (base.MainRole as IFpcRole).FpcModule;
			}
		}

		public ItemViewmodelBase SpawnedViewmodel { get; private set; }

		public override Vector3 CameraPosition
		{
			get
			{
				ICameraController cameraController = base.MainRole as ICameraController;
				if (cameraController == null)
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
				ICameraController cameraController = base.MainRole as ICameraController;
				if (cameraController != null)
				{
					IAdvancedCameraController advancedCameraController = cameraController as IAdvancedCameraController;
					float num = ((advancedCameraController != null) ? advancedCameraController.RollRotation : 0f);
					return new Vector3(cameraController.VerticalRotation, cameraController.HorizontalRotation, num);
				}
				return base.TargetHub.PlayerCameraReference.rotation.eulerAngles;
			}
		}

		internal override void OnBeganSpectating()
		{
			this.FpcModule.CharacterModelInstance.SetVisibility(false);
			Inventory.OnCurrentItemChanged += this.OnCurrentItemChanged;
			ItemIdentifier curItem = base.TargetHub.inventory.CurItem;
			this.OnCurrentItemChanged(base.TargetHub, curItem, curItem);
			SharedHandsController.SetRoleGloves(base.TargetHub.GetRoleId());
			AnimatedCharacterModel animatedCharacterModel = this.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			CameraShakeController.AddEffect(new HeadbobShake(animatedCharacterModel));
		}

		internal override void OnStoppedSpectating()
		{
			CharacterModel characterModelInstance = this.FpcModule.CharacterModelInstance;
			ReferenceHub ownerHub = characterModelInstance.OwnerHub;
			if (!(ownerHub != null) || !ownerHub.isLocalPlayer)
			{
				characterModelInstance.SetVisibility(true);
			}
			Inventory.OnCurrentItemChanged -= this.OnCurrentItemChanged;
			if (this.SpawnedViewmodel != null)
			{
				global::UnityEngine.Object.Destroy(this.SpawnedViewmodel.gameObject);
			}
			SharedHandsController.UpdateInstance(null);
		}

		private void OnCurrentItemChanged(ReferenceHub hub, ItemIdentifier oldItem, ItemIdentifier newItem)
		{
			ReferenceHub referenceHub;
			if (!base.MainRole.TryGetOwner(out referenceHub) || hub != referenceHub)
			{
				return;
			}
			if (this.SpawnedViewmodel != null)
			{
				global::UnityEngine.Object.Destroy(this.SpawnedViewmodel.gameObject);
			}
			ItemBase itemBase;
			if (InventoryItemLoader.TryGetItem<ItemBase>(newItem.TypeId, out itemBase) && itemBase.ViewModel != null)
			{
				this.SpawnedViewmodel = global::UnityEngine.Object.Instantiate<ItemViewmodelBase>(itemBase.ViewModel, SharedHandsController.Singleton.transform);
				SharedHandsController.UpdateInstance(this.SpawnedViewmodel);
				this.SpawnedViewmodel.InitSpectator(hub, newItem, oldItem == newItem);
				return;
			}
			this.SpawnedViewmodel = null;
			SharedHandsController.UpdateInstance(null);
		}

		public bool TryGetViewmodelFov(out float fov)
		{
			if (this.SpawnedViewmodel != null)
			{
				fov = this.SpawnedViewmodel.ViewmodelCameraFOV;
				return true;
			}
			IViewmodelRole viewmodelRole = base.MainRole as IViewmodelRole;
			if (viewmodelRole != null)
			{
				return viewmodelRole.TryGetViewmodelFov(out fov);
			}
			fov = 0f;
			return false;
		}
	}
}
