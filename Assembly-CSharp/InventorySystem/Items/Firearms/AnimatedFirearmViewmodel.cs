using System;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.Firearms
{
	public class AnimatedFirearmViewmodel : StandardAnimatedViemodel
	{
		public event Action OnAttachmentsUpdated;

		public override float ViewmodelCameraFOV
		{
			get
			{
				return base.ViewmodelCameraFOV - this.FovOffset;
			}
		}

		public Firearm ParentFirearm
		{
			get
			{
				return base.ParentItem as Firearm;
			}
		}

		public float FovOffset { get; set; }

		public bool Initialized { get; private set; }

		public Component[] Extensions { get; private set; }

		public AttachmentGameObjectGroup[] Attachments { get; private set; }

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.ParentItem = ply.inventory.CreateItemInstance(id, false) as Firearm;
			uint num;
			bool flag = AttachmentCodeSync.TryGet(id.SerialNumber, out num);
			this.ParentFirearm.ViewModel = this;
			this.ParentFirearm.IsEquipped = true;
			this.ParentFirearm.InitializeSubcomponents();
			this.ParentFirearm.ApplyAttachmentsCode(num, flag);
			base.InitSpectator(ply, id, wasEquipped);
			this.UpdateAttachments(this.ParentFirearm);
			base.AnimatorRebind();
			foreach (SubcomponentBase subcomponentBase in this.ParentFirearm.AllSubcomponents)
			{
				subcomponentBase.SpectatorInit();
				subcomponentBase.EquipUpdate();
			}
			if (!wasEquipped)
			{
				return;
			}
			ISpectatorSyncModule spectatorSyncModule;
			if (this.ParentFirearm.TryGetModule(out spectatorSyncModule, true))
			{
				spectatorSyncModule.SetupViewmodel(this, base.SkipEquipTime);
			}
			else
			{
				this.AnimatorForceUpdate(base.SkipEquipTime, false);
			}
			SubcomponentBase[] array = this.ParentFirearm.AllSubcomponents;
			for (int i = 0; i < array.Length; i++)
			{
				((FirearmSubcomponentBase)array[i]).SpectatorPostprocessSkip();
			}
		}

		public override void InitAny()
		{
			base.InitAny();
			foreach (Component component in this.Extensions)
			{
				IViewmodelExtension viewmodelExtension = component as IViewmodelExtension;
				if (viewmodelExtension != null)
				{
					try
					{
						viewmodelExtension.InitViewmodel(this);
					}
					catch (Exception ex)
					{
						Debug.LogError(string.Concat(new string[]
						{
							"Extension ",
							component.GetType().Name,
							" (",
							component.gameObject.name,
							") failed to init!"
						}));
						Debug.LogException(ex);
					}
				}
			}
			AttachmentsUtils.OnAttachmentsApplied += this.UpdateAttachments;
			this.Initialized = true;
		}

		public bool TryGetExtension<T>(out T extension)
		{
			foreach (Component component in this.Extensions)
			{
				if (component is T)
				{
					T t = component as T;
					extension = t;
					return true;
				}
			}
			extension = default(T);
			return false;
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			this.UpdateAttachments(this.ParentFirearm);
		}

		protected override IItemSwayController GetNewSwayController()
		{
			return new FirearmSway(this._hipSwaySettings, this);
		}

		private void UpdateAttachments(Firearm fa)
		{
			if (fa.ItemSerial != base.ItemId.SerialNumber)
			{
				return;
			}
			foreach (AttachmentGameObjectGroup attachmentGameObjectGroup in this.Attachments)
			{
				attachmentGameObjectGroup.SetActive(false);
			}
			for (int j = 0; j < this.Attachments.Length; j++)
			{
				if (this.ParentFirearm.Attachments[j].IsEnabled)
				{
					this.Attachments[j].SetActive(true);
				}
			}
			Action onAttachmentsUpdated = this.OnAttachmentsUpdated;
			if (onAttachmentsUpdated == null)
			{
				return;
			}
			onAttachmentsUpdated();
		}

		private void Update()
		{
			if (!base.IsSpectator)
			{
				return;
			}
			this.ParentFirearm.EquipUpdate();
		}

		private void OnDestroy()
		{
			AttachmentsUtils.OnAttachmentsApplied -= this.UpdateAttachments;
			Component[] extensions = this.Extensions;
			for (int i = 0; i < extensions.Length; i++)
			{
				IDestroyExtensionReceiver destroyExtensionReceiver = extensions[i] as IDestroyExtensionReceiver;
				if (destroyExtensionReceiver != null)
				{
					destroyExtensionReceiver.OnDestroyExtension();
				}
			}
			if (!base.IsSpectator)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(base.ParentItem.gameObject);
		}

		[SerializeField]
		private GoopSway.GoopSwaySettings _hipSwaySettings;
	}
}
