using System;
using Footprinting;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms
{
	public class Firearm : ModularAutosyncItem, IItemNametag, ICustomCrosshairItem, IDisarmingItem, ICustomSearchCompletorItem
	{
		public event Action OnServerAnimatorMove;

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Firearm;
			}
		}

		public override float Weight
		{
			get
			{
				return this.TotalWeightKg();
			}
		}

		public float Length
		{
			get
			{
				return this.TotalLengthInches();
			}
		}

		public FirearmWorldmodel WorldModel { get; set; }

		public FirearmCategory FirearmCategory { get; private set; }

		public float BaseWeight { get; private set; }

		public float BaseLength { get; private set; }

		public Texture BodyIconTexture { get; set; }

		public Animator ServerSideAnimator { get; private set; }

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public AnimatedFirearmViewmodel ClientViewmodelInstance
		{
			get
			{
				if (!this.HasViewmodel)
				{
					return null;
				}
				return this.ViewModel as AnimatedFirearmViewmodel;
			}
		}

		public override bool HasViewmodel
		{
			get
			{
				AnimatedFirearmViewmodel animatedFirearmViewmodel = this.ViewModel as AnimatedFirearmViewmodel;
				return animatedFirearmViewmodel != null && animatedFirearmViewmodel != null && animatedFirearmViewmodel.Initialized;
			}
		}

		public Footprint Footprint
		{
			get
			{
				if (!this._footprintCacheSet)
				{
					this._footprintCacheSet = true;
					this._cachedFootprint = new Footprint(base.Owner);
				}
				return this._cachedFootprint;
			}
		}

		public ModuleBase[] Modules
		{
			get
			{
				if (this._modules == null)
				{
					this._modules = base.AllSubcomponents.OfType<SubcomponentBase, ModuleBase>();
				}
				return this._modules;
			}
		}

		public Attachment[] Attachments
		{
			get
			{
				if (this._attachments == null)
				{
					this._attachments = base.AllSubcomponents.OfType<SubcomponentBase, Attachment>();
				}
				return this._attachments;
			}
		}

		public bool ServerIsPersonal
		{
			get
			{
				ItemAddReason serverAddReason = base.ServerAddReason;
				return serverAddReason == ItemAddReason.StartingItem || serverAddReason == ItemAddReason.AdminCommand || serverAddReason == ItemAddReason.Scp2536;
			}
		}

		internal override bool IsLocalPlayer
		{
			get
			{
				return base.HasOwner && base.IsLocalPlayer;
			}
		}

		public Type CrosshairType
		{
			get
			{
				if (DebugStatsCrosshair.Enabled)
				{
					return typeof(DebugStatsCrosshair);
				}
				ICustomCrosshairItem customCrosshairItem;
				if (this.TryGetModule(out customCrosshairItem, true))
				{
					return customCrosshairItem.CrosshairType;
				}
				return null;
			}
		}

		public bool AllowDisarming
		{
			get
			{
				return true;
			}
		}

		public override void InitializeSubcomponents()
		{
			this.ServerSideAnimator = base.GetComponent<Animator>();
			this.ServerSideAnimator.enabled = false;
			base.InitializeSubcomponents();
			uint num;
			AttachmentCodeSync.TryGet(base.ItemSerial, out num);
			if (base.IsServer)
			{
				if (this.ServerIsPersonal)
				{
					num = AttachmentsServerHandler.ServerGetReceivedPlayerPreference(this);
				}
				this.ApplyAttachmentsCode(num, true);
				this.ServerResendAttachmentCode();
				return;
			}
			this.ApplyAttachmentsCode(num, true);
		}

		public override void ServerConfirmAcqusition()
		{
			base.ServerConfirmAcqusition();
			this.ServerResendAttachmentCode();
		}

		public override void OnEquipped()
		{
			base.OnEquipped();
			this.ServerSideAnimator.enabled = base.IsServer;
			if (base.IsServer)
			{
				this.ServerResendAttachmentCode();
			}
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			if (!base.IsServer)
			{
				return;
			}
			this.ServerSideAnimator.Rebind();
			this.ServerSideAnimator.enabled = false;
			base.OwnerInventory.RemoveEverythingExceedingLimits(false, true);
		}

		protected virtual void OnAnimatorMove()
		{
			Action onServerAnimatorMove = this.OnServerAnimatorMove;
			if (onServerAnimatorMove == null)
			{
				return;
			}
			onServerAnimatorMove();
		}

		public SearchCompletor GetCustomSearchCompletor(ReferenceHub hub, ItemPickupBase ipb, ItemBase ib, double disSqrt)
		{
			return new FirearmSearchCompletor(hub, ipb as FirearmPickup, ib, disSqrt);
		}

		protected override void Awake()
		{
			base.Awake();
			AttachmentCodeSync.OnReceived += this.OnAttachmentCodeReceived;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			AttachmentCodeSync.OnReceived -= this.OnAttachmentCodeReceived;
		}

		private void OnAttachmentCodeReceived(ushort serial, uint attCode)
		{
			if (serial != base.ItemSerial || NetworkServer.active)
			{
				return;
			}
			this.ApplyAttachmentsCode(attCode, false);
		}

		private Footprint _cachedFootprint;

		private bool _footprintCacheSet;

		private ModuleBase[] _modules;

		private Attachment[] _attachments;
	}
}
