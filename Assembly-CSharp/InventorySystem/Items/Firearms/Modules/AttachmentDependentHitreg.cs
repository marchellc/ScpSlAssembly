using System;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class AttachmentDependentHitreg : ModuleBase, IHitregModule, ICustomCrosshairItem, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
{
	[Serializable]
	private class AttachmentOverride
	{
		[SerializeField]
		private Attachment _attachment;

		[SerializeField]
		private ModuleBase _module;

		public IHitregModule Hitreg { get; private set; }

		public bool IsEnabled => this._attachment.IsEnabled;

		public void Init()
		{
			this._module.MarkAsSubmodule();
			this.Hitreg = this._module as IHitregModule;
		}
	}

	[SerializeField]
	private ModuleBase _primaryHitreg;

	[SerializeField]
	private AttachmentOverride[] _overrideHitregs;

	private bool _targetCacheSet;

	private IHitregModule _cachedTargetModule;

	public float DisplayDamage => this.TargetModule.DisplayDamage;

	public float DisplayPenetration => this.TargetModule.DisplayPenetration;

	public Type CrosshairType
	{
		get
		{
			if (!(this.TargetModule is ICustomCrosshairItem customCrosshairItem))
			{
				return null;
			}
			return customCrosshairItem.CrosshairType;
		}
	}

	public DisplayInaccuracyValues DisplayInaccuracy
	{
		get
		{
			if (!(this.TargetModule is IDisplayableInaccuracyProviderModule displayableInaccuracyProviderModule))
			{
				return default(DisplayInaccuracyValues);
			}
			return displayableInaccuracyProviderModule.DisplayInaccuracy;
		}
	}

	public float Inaccuracy
	{
		get
		{
			if (!(this.TargetModule is IInaccuracyProviderModule inaccuracyProviderModule))
			{
				return 0f;
			}
			return inaccuracyProviderModule.Inaccuracy;
		}
	}

	private IHitregModule TargetModule
	{
		get
		{
			if (this._targetCacheSet)
			{
				return this._cachedTargetModule;
			}
			this._cachedTargetModule = this._primaryHitreg as IHitregModule;
			AttachmentOverride[] overrideHitregs = this._overrideHitregs;
			foreach (AttachmentOverride attachmentOverride in overrideHitregs)
			{
				if (attachmentOverride.IsEnabled)
				{
					this._cachedTargetModule = attachmentOverride.Hitreg;
					break;
				}
			}
			this._targetCacheSet = true;
			return this._cachedTargetModule;
		}
	}

	public event Action ServerOnFired;

	public void Fire(ReferenceHub primaryTarget, ShotEvent shotData)
	{
		this.TargetModule.Fire(primaryTarget, shotData);
	}

	protected override void OnInit()
	{
		base.OnInit();
		this._primaryHitreg.MarkAsSubmodule();
		(this._primaryHitreg as IHitregModule).ServerOnFired += delegate
		{
			this.ServerOnFired?.Invoke();
		};
		AttachmentOverride[] overrideHitregs = this._overrideHitregs;
		foreach (AttachmentOverride obj in overrideHitregs)
		{
			obj.Init();
			obj.Hitreg.ServerOnFired += delegate
			{
				this.ServerOnFired?.Invoke();
			};
		}
	}

	internal override void OnAttachmentsApplied()
	{
		base.OnAttachmentsApplied();
		this._targetCacheSet = false;
		if (base.IsLocalPlayer && base.Firearm.IsEquipped)
		{
			CrosshairController.Refresh(base.Firearm.Owner, base.ItemSerial);
		}
	}
}
