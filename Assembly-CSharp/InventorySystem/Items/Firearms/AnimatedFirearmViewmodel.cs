using System;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.Firearms;

public class AnimatedFirearmViewmodel : StandardAnimatedViemodel
{
	[SerializeField]
	private GoopSway.GoopSwaySettings _hipSwaySettings;

	public override float ViewmodelCameraFOV => base.ViewmodelCameraFOV - this.FovOffset;

	public Firearm ParentFirearm => base.ParentItem as Firearm;

	public float FovOffset { get; set; }

	public bool Initialized { get; private set; }

	[field: SerializeField]
	public Component[] Extensions { get; private set; }

	[field: SerializeField]
	[field: LabelledAttachmentArray]
	public AttachmentGameObjectGroup[] Attachments { get; private set; }

	public event Action OnAttachmentsUpdated;

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.ParentItem = ply.inventory.CreateItemInstance(id, updateViewmodel: false) as Firearm;
		uint code;
		bool reValidate = AttachmentCodeSync.TryGet(id.SerialNumber, out code);
		this.ParentFirearm.ViewModel = this;
		this.ParentFirearm.IsEquipped = true;
		this.ParentFirearm.InitializeSubcomponents();
		this.ParentFirearm.ApplyAttachmentsCode(code, reValidate);
		base.InitSpectator(ply, id, wasEquipped);
		this.UpdateAttachments(this.ParentFirearm);
		base.AnimatorRebind();
		SubcomponentBase[] allSubcomponents = this.ParentFirearm.AllSubcomponents;
		foreach (SubcomponentBase obj in allSubcomponents)
		{
			obj.SpectatorInit();
			obj.EquipUpdate();
		}
		if (wasEquipped)
		{
			if (this.ParentFirearm.TryGetModule<ISpectatorSyncModule>(out var module))
			{
				module.SetupViewmodel(this, base.SkipEquipTime);
			}
			else
			{
				this.AnimatorForceUpdate(base.SkipEquipTime, fastMode: false);
			}
			allSubcomponents = this.ParentFirearm.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				((FirearmSubcomponentBase)allSubcomponents[i]).SpectatorPostprocessSkip();
			}
		}
	}

	public override void InitAny()
	{
		base.InitAny();
		Component[] extensions = this.Extensions;
		foreach (Component component in extensions)
		{
			if (component is IViewmodelExtension viewmodelExtension)
			{
				try
				{
					viewmodelExtension.InitViewmodel(this);
				}
				catch (Exception exception)
				{
					Debug.LogError("Extension " + component.GetType().Name + " (" + component.gameObject.name + ") failed to init!");
					Debug.LogException(exception);
				}
			}
		}
		AttachmentsUtils.OnAttachmentsApplied += UpdateAttachments;
		this.Initialized = true;
	}

	public bool TryGetExtension<T>(out T extension)
	{
		Component[] extensions = this.Extensions;
		for (int i = 0; i < extensions.Length; i++)
		{
			if (extensions[i] is T val)
			{
				extension = val;
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
		AttachmentGameObjectGroup[] attachments = this.Attachments;
		foreach (AttachmentGameObjectGroup attachmentGameObjectGroup in attachments)
		{
			attachmentGameObjectGroup.SetActive(state: false);
		}
		for (int j = 0; j < this.Attachments.Length; j++)
		{
			if (this.ParentFirearm.Attachments[j].IsEnabled)
			{
				this.Attachments[j].SetActive(state: true);
			}
		}
		this.OnAttachmentsUpdated?.Invoke();
	}

	private void Update()
	{
		if (base.IsSpectator)
		{
			this.ParentFirearm.EquipUpdate();
		}
	}

	private void OnDestroy()
	{
		AttachmentsUtils.OnAttachmentsApplied -= UpdateAttachments;
		Component[] extensions = this.Extensions;
		for (int i = 0; i < extensions.Length; i++)
		{
			if (extensions[i] is IDestroyExtensionReceiver destroyExtensionReceiver)
			{
				destroyExtensionReceiver.OnDestroyExtension();
			}
		}
		if (base.IsSpectator)
		{
			UnityEngine.Object.Destroy(base.ParentItem.gameObject);
		}
	}
}
