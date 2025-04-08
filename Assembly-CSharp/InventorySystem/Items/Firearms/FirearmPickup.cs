using System;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms
{
	public class FirearmPickup : CollisionDetectionPickup, IPickupDistributorTrigger
	{
		public ItemIdentifier CurId
		{
			get
			{
				return new ItemIdentifier(this.Info.ItemId, this.Info.Serial);
			}
		}

		public Firearm Template
		{
			get
			{
				if (!this._templateSet)
				{
					if (!this.Info.ItemId.TryGetTemplate(out this._cachedTemplate))
					{
						throw new InvalidOperationException(string.Format("Unable to create a pickup for {0} - this ID is not defined as a firearm.", this.Info.ItemId));
					}
					this._templateSet = true;
				}
				return this._cachedTemplate;
			}
		}

		public FirearmWorldmodel Worldmodel
		{
			get
			{
				if (!this._instanceSpawned)
				{
					this.SpawnInstance();
				}
				return this._worldmodelInstance;
			}
		}

		public void OnDistributed()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			AttachmentCodeSync.ServerSetCode(this.Info.Serial, AttachmentsUtils.GetRandomAttachmentsCode(this.Info.ItemId));
			SubcomponentBase[] allSubcomponents = this.Template.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessMapgenDistribution(this);
			}
		}

		protected override void Start()
		{
			base.Start();
			AttachmentCodeSync.OnReceived += this.OnAttachmentsReceived;
			this.SpawnInstance();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			AttachmentCodeSync.OnReceived -= this.OnAttachmentsReceived;
		}

		private void SpawnInstance()
		{
			if (this._instanceSpawned)
			{
				return;
			}
			this._worldmodelInstance = global::UnityEngine.Object.Instantiate<FirearmWorldmodel>(this.Template.WorldModel, base.transform);
			this._worldmodelInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			this._worldmodelInstance.Setup(this.CurId, FirearmWorldmodelType.Pickup);
			this._instanceSpawned = true;
		}

		private void OnAttachmentsReceived(ushort serial, uint attId)
		{
			if (serial != this.Info.Serial || !this._instanceSpawned)
			{
				return;
			}
			this._worldmodelInstance.Setup(this.CurId, FirearmWorldmodelType.Pickup, attId);
		}

		public override bool Weaved()
		{
			return true;
		}

		private FirearmWorldmodel _worldmodelInstance;

		private Firearm _cachedTemplate;

		private bool _templateSet;

		private bool _instanceSpawned;
	}
}
