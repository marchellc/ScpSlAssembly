using System;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AttachmentDependentHitreg : ModuleBase, IHitregModule, ICustomCrosshairItem, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
	{
		public event Action ServerOnFired;

		public float DisplayDamage
		{
			get
			{
				return this.TargetModule.DisplayDamage;
			}
		}

		public float DisplayPenetration
		{
			get
			{
				return this.TargetModule.DisplayPenetration;
			}
		}

		public Type CrosshairType
		{
			get
			{
				ICustomCrosshairItem customCrosshairItem = this.TargetModule as ICustomCrosshairItem;
				if (customCrosshairItem == null)
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
				IDisplayableInaccuracyProviderModule displayableInaccuracyProviderModule = this.TargetModule as IDisplayableInaccuracyProviderModule;
				if (displayableInaccuracyProviderModule == null)
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
				IInaccuracyProviderModule inaccuracyProviderModule = this.TargetModule as IInaccuracyProviderModule;
				if (inaccuracyProviderModule == null)
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
				foreach (AttachmentDependentHitreg.AttachmentOverride attachmentOverride in this._overrideHitregs)
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
				Action serverOnFired = this.ServerOnFired;
				if (serverOnFired == null)
				{
					return;
				}
				serverOnFired();
			};
			foreach (AttachmentDependentHitreg.AttachmentOverride attachmentOverride in this._overrideHitregs)
			{
				attachmentOverride.Init();
				attachmentOverride.Hitreg.ServerOnFired += delegate
				{
					Action serverOnFired2 = this.ServerOnFired;
					if (serverOnFired2 == null)
					{
						return;
					}
					serverOnFired2();
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

		[SerializeField]
		private ModuleBase _primaryHitreg;

		[SerializeField]
		private AttachmentDependentHitreg.AttachmentOverride[] _overrideHitregs;

		private bool _targetCacheSet;

		private IHitregModule _cachedTargetModule;

		[Serializable]
		private class AttachmentOverride
		{
			public IHitregModule Hitreg { get; private set; }

			public bool IsEnabled
			{
				get
				{
					return this._attachment.IsEnabled;
				}
			}

			public void Init()
			{
				this._module.MarkAsSubmodule();
				this.Hitreg = this._module as IHitregModule;
			}

			[SerializeField]
			private Attachment _attachment;

			[SerializeField]
			private ModuleBase _module;
		}
	}
}
