using System;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms;

public class FirearmPickup : CollisionDetectionPickup, IPickupDistributorTrigger
{
	private FirearmWorldmodel _worldmodelInstance;

	private Firearm _cachedTemplate;

	private bool _templateSet;

	private bool _instanceSpawned;

	public ItemIdentifier CurId => new ItemIdentifier(base.Info.ItemId, base.Info.Serial);

	public Firearm Template
	{
		get
		{
			if (!this._templateSet)
			{
				if (!base.Info.ItemId.TryGetTemplate<Firearm>(out this._cachedTemplate))
				{
					throw new InvalidOperationException($"Unable to create a pickup for {base.Info.ItemId} - this ID is not defined as a firearm.");
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
		if (NetworkServer.active)
		{
			AttachmentCodeSync.ServerSetCode(base.Info.Serial, AttachmentsUtils.GetRandomAttachmentsCode(base.Info.ItemId));
			SubcomponentBase[] allSubcomponents = this.Template.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessMapgenDistribution(this);
			}
		}
	}

	public override PickupSearchCompletor GetPickupSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		if (!InventoryItemLoader.TryGetItem<ItemBase>(base.Info.ItemId, out var result))
		{
			return null;
		}
		return new FirearmSearchCompletor(coordinator.Hub, this, result, sqrDistance);
	}

	protected override void Start()
	{
		base.Start();
		AttachmentCodeSync.OnReceived += OnAttachmentsReceived;
		this.SpawnInstance();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		AttachmentCodeSync.OnReceived -= OnAttachmentsReceived;
	}

	private void SpawnInstance()
	{
		if (!this._instanceSpawned)
		{
			this._worldmodelInstance = UnityEngine.Object.Instantiate(this.Template.WorldModel, base.transform);
			this._worldmodelInstance.transform.ResetLocalPose();
			this._worldmodelInstance.Setup(this.CurId, FirearmWorldmodelType.Pickup);
			this._instanceSpawned = true;
		}
	}

	private void OnAttachmentsReceived(ushort serial, uint attId)
	{
		if (serial == base.Info.Serial && this._instanceSpawned)
		{
			this._worldmodelInstance.Setup(this.CurId, FirearmWorldmodelType.Pickup, attId);
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
