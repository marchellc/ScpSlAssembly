using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using InventorySystem.GUI;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using NetworkManagerUtils.Dummies;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using RoundRestarting;
using UnityEngine;

namespace InventorySystem;

public class Inventory : NetworkBehaviour, IStaminaModifier, IMovementSpeedModifier, IRootDummyActionProvider
{
	public const int MaxSlots = 8;

	public InventoryInfo UserInventory = new InventoryInfo();

	[SyncVar(hook = "OnItemUpdated")]
	public ItemIdentifier CurItem = ItemIdentifier.None;

	public bool SendItemsNextFrame;

	public bool SendAmmoNextFrame;

	private ItemIdentifier _prevCurItem;

	internal ReferenceHub _hub;

	private ItemBase _curInstance;

	private float _staminaModifier;

	private float _movementLimiter;

	private float _movementMultiplier;

	private bool _sprintingDisabled;

	private readonly Stopwatch _lastEquipSw = Stopwatch.StartNew();

	[SyncVar]
	private float _syncStaminaModifier;

	[SyncVar]
	private float _syncMovementLimiter;

	[SyncVar]
	private float _syncMovementMultiplier;

	[HideInInspector]
	public ItemBase CurInstance
	{
		get
		{
			return this._curInstance;
		}
		set
		{
			if (value == this._curInstance)
			{
				return;
			}
			ItemBase curInstance = this._curInstance;
			this._curInstance = value;
			bool flag = this._curInstance == null;
			if (curInstance != null)
			{
				curInstance.OnHolstered();
				curInstance.IsEquipped = false;
				if (base.isLocalPlayer)
				{
					curInstance.ViewModel.gameObject.SetActive(value: false);
					if (flag)
					{
						SharedHandsController.UpdateInstance(null);
					}
				}
			}
			if (this._curInstance != null)
			{
				if (base.isLocalPlayer)
				{
					this._curInstance.ViewModel.gameObject.SetActive(value: true);
					SharedHandsController.UpdateInstance(this._curInstance.ViewModel);
					this._curInstance.ViewModel.OnEquipped();
				}
				this._curInstance.OnEquipped();
				this._curInstance.IsEquipped = true;
			}
		}
	}

	public float LastItemSwitch => (float)this._lastEquipSw.Elapsed.TotalSeconds;

	private Transform ItemWorkspace => SharedHandsController.Singleton.transform;

	public bool StaminaModifierActive => true;

	public bool MovementModifierActive => true;

	public float StaminaUsageMultiplier
	{
		get
		{
			if (!this.IsObserver)
			{
				return this._staminaModifier;
			}
			return this._syncStaminaModifier;
		}
	}

	public float StaminaRegenMultiplier => 1f;

	public bool SprintingDisabled
	{
		get
		{
			if (!this.IsObserver)
			{
				return this._sprintingDisabled;
			}
			return false;
		}
	}

	public float MovementSpeedMultiplier
	{
		get
		{
			if (!this.IsObserver)
			{
				return this._movementMultiplier;
			}
			return this._syncMovementMultiplier;
		}
	}

	public float MovementSpeedLimit
	{
		get
		{
			if (!this.IsObserver)
			{
				return this._movementLimiter;
			}
			return this._syncMovementLimiter;
		}
	}

	private bool IsObserver
	{
		get
		{
			if (!NetworkServer.active)
			{
				return !base.isLocalPlayer;
			}
			return false;
		}
	}

	private bool HasViewmodel
	{
		get
		{
			if (base.isLocalPlayer && this.CurInstance != null)
			{
				return this.CurInstance.ViewModel != null;
			}
			return false;
		}
	}

	public bool DummyActionsDirty { get; set; }

	public ItemIdentifier NetworkCurItem
	{
		get
		{
			return this.CurItem;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.CurItem, 1uL, OnItemUpdated);
		}
	}

	public float Network_syncStaminaModifier
	{
		get
		{
			return this._syncStaminaModifier;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncStaminaModifier, 2uL, null);
		}
	}

	public float Network_syncMovementLimiter
	{
		get
		{
			return this._syncMovementLimiter;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncMovementLimiter, 4uL, null);
		}
	}

	public float Network_syncMovementMultiplier
	{
		get
		{
			return this._syncMovementMultiplier;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncMovementMultiplier, 8uL, null);
		}
	}

	public static event Action<ReferenceHub> OnItemsModified;

	public static event Action<ReferenceHub> OnAmmoModified;

	public static event Action OnServerStarted;

	public static event Action OnLocalClientStarted;

	public static event Action<ReferenceHub, ItemIdentifier, ItemIdentifier> OnCurrentItemChanged;

	private void OnItemUpdated(ItemIdentifier prev, ItemIdentifier cur)
	{
		if (prev != cur)
		{
			this._lastEquipSw.Restart();
		}
	}

	private void Awake()
	{
		this._hub = ReferenceHub.GetHub(base.gameObject);
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		if (!NetworkServer.active || RoundRestart.IsRoundRestarting)
		{
			return;
		}
		HashSet<ushort> hashSet = HashSetPool<ushort>.Shared.Rent();
		foreach (KeyValuePair<ushort, ItemBase> item in this.UserInventory.Items)
		{
			hashSet.Add(item.Key);
		}
		foreach (ushort item2 in hashSet)
		{
			this.ServerRemoveItem(item2, null);
		}
		HashSetPool<ushort>.Shared.Return(hashSet);
	}

	private void Start()
	{
		if ((base.isLocalPlayer || NetworkServer.active) && base.isLocalPlayer)
		{
			if (NetworkServer.active)
			{
				Inventory.OnServerStarted();
				CustomNetworkManager.InvokeOnClientReady();
			}
			Inventory.OnLocalClientStarted?.Invoke();
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			if (this.SendItemsNextFrame)
			{
				this.DummyActionsDirty = true;
				this.SendItemsNextFrame = false;
				Inventory.OnItemsModified?.Invoke(this._hub);
				this.ServerSendItems();
			}
			if (this.SendAmmoNextFrame)
			{
				this.DummyActionsDirty = true;
				this.SendAmmoNextFrame = false;
				Inventory.OnAmmoModified?.Invoke(this._hub);
				this.ServerSendAmmo();
			}
		}
		this.UpdateCurItem();
		if (!this.IsObserver)
		{
			this.UpdateObserverItems();
			this.RefreshModifiers();
			if (this.HasViewmodel && Input.GetKeyDown(NewInput.GetKey(ActionName.ThrowItem)) && this.CurInstance.AllowDropping && InventoryGuiController.ItemsSafeForInteraction)
			{
				this.CmdDropItem(this.CurItem.SerialNumber, tryThrow: true);
			}
		}
	}

	private void UpdateCurItem()
	{
		if (this._prevCurItem == this.CurItem)
		{
			return;
		}
		this.DummyActionsDirty = true;
		if (base.isLocalPlayer)
		{
			if (this.CurItem.TypeId != ItemType.None)
			{
				if (!this.UserInventory.Items.TryGetValue(this.CurItem.SerialNumber, out var value))
				{
					return;
				}
				this.CurInstance = value;
			}
			else
			{
				this.CurInstance = null;
			}
		}
		Inventory.OnCurrentItemChanged?.Invoke(this._hub, this._prevCurItem, this.CurItem);
		this._prevCurItem = new ItemIdentifier(this.CurItem.TypeId, this.CurItem.SerialNumber);
	}

	private void UpdateObserverItems()
	{
		List<ushort> list = ListPool<ushort>.Shared.Rent();
		foreach (KeyValuePair<ushort, ItemBase> item in this.UserInventory.Items)
		{
			list.Add(item.Key);
		}
		foreach (ushort item2 in list)
		{
			if (this.UserInventory.Items.TryGetValue(item2, out var value) && value.enabled)
			{
				if (value.IsEquipped)
				{
					value.EquipUpdate();
				}
				value.AlwaysUpdate();
			}
		}
		ListPool<ushort>.Shared.Return(list);
	}

	private void RefreshModifiers()
	{
		this._staminaModifier = 1f;
		this._movementLimiter = float.MaxValue;
		this._movementMultiplier = 1f;
		this._sprintingDisabled = false;
		foreach (KeyValuePair<ushort, ItemBase> item in this.UserInventory.Items)
		{
			object mobilityController = item.Value.GetMobilityController();
			if (mobilityController is IStaminaModifier { StaminaModifierActive: not false } staminaModifier)
			{
				this._staminaModifier *= staminaModifier.StaminaUsageMultiplier;
				this._sprintingDisabled |= staminaModifier.SprintingDisabled;
			}
			if (mobilityController is IMovementSpeedModifier { MovementModifierActive: not false } movementSpeedModifier)
			{
				this._movementLimiter = Mathf.Min(this._movementLimiter, movementSpeedModifier.MovementSpeedLimit);
				this._movementMultiplier *= movementSpeedModifier.MovementSpeedMultiplier;
			}
		}
		if (NetworkServer.active)
		{
			this.Network_syncStaminaModifier = this._staminaModifier;
			this.Network_syncMovementMultiplier = this._movementMultiplier;
			this.Network_syncMovementLimiter = this._movementLimiter;
		}
	}

	[Server]
	public void ServerSelectItem(ushort itemSerial)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSelectItem(System.UInt16)' called when server was not active");
		}
		else
		{
			if (itemSerial == this.CurItem.SerialNumber)
			{
				return;
			}
			ItemBase value = null;
			ItemBase value2 = null;
			bool flag = this.CurItem.SerialNumber == 0 || (this.UserInventory.Items.TryGetValue(this.CurItem.SerialNumber, out value) && this.CurInstance != null);
			if (itemSerial == 0 || this.UserInventory.Items.TryGetValue(itemSerial, out value2))
			{
				if ((this.CurItem.SerialNumber != 0 && flag && !value.AllowHolster) || (itemSerial != 0 && !value2.AllowEquip))
				{
					return;
				}
				PlayerChangingItemEventArgs e = new PlayerChangingItemEventArgs(this._hub, value, value2);
				PlayerEvents.OnChangingItem(e);
				if (!e.IsAllowed)
				{
					return;
				}
				if (itemSerial == 0)
				{
					this.NetworkCurItem = ItemIdentifier.None;
					if (!base.isLocalPlayer)
					{
						this.CurInstance = null;
					}
				}
				else
				{
					this.NetworkCurItem = new ItemIdentifier(value2.ItemTypeId, itemSerial);
					if (!base.isLocalPlayer)
					{
						this.CurInstance = value2;
					}
				}
				PlayerEvents.OnChangedItem(new PlayerChangedItemEventArgs(this._hub, value, value2));
			}
			else if (!flag)
			{
				this.NetworkCurItem = ItemIdentifier.None;
				if (!base.isLocalPlayer)
				{
					this.CurInstance = null;
				}
			}
		}
	}

	public void ClientSelectItem(ushort itemSerial)
	{
		if (this.CurInstance != null && this.CurInstance.ItemSerial != itemSerial)
		{
			this.CurInstance.OnHolsterRequestSent();
		}
		this.CmdSelectItem(itemSerial);
	}

	public void ClientDropItem(ushort itemSerial, bool tryThrow)
	{
		if (this.CurInstance != null && this.CurInstance.ItemSerial == itemSerial)
		{
			this.CurInstance.OnHolsterRequestSent();
		}
		this.CmdDropItem(itemSerial, tryThrow);
	}

	[Server]
	private void ServerSendItems()
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSendItems()' called when server was not active");
		}
		else
		{
			if (base.isLocalPlayer)
			{
				return;
			}
			HashSet<ItemIdentifier> hashSet = HashSetPool<ItemIdentifier>.Shared.Rent();
			foreach (KeyValuePair<ushort, ItemBase> item in this.UserInventory.Items)
			{
				hashSet.Add(new ItemIdentifier(item.Value.ItemTypeId, item.Key));
			}
			this.TargetRefreshItems(hashSet.ToArray());
			HashSetPool<ItemIdentifier>.Shared.Return(hashSet);
		}
	}

	[Server]
	private void ServerSendAmmo()
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSendAmmo()' called when server was not active");
		}
		else
		{
			if (base.isLocalPlayer)
			{
				return;
			}
			List<byte> list = ListPool<byte>.Shared.Rent();
			List<ushort> list2 = ListPool<ushort>.Shared.Rent();
			foreach (KeyValuePair<ItemType, ushort> item in this.UserInventory.ReserveAmmo)
			{
				list.Add((byte)item.Key);
				list2.Add(item.Value);
			}
			this.TargetRefreshAmmo(list.ToArray(), list2.ToArray());
			ListPool<byte>.Shared.Return(list);
			ListPool<ushort>.Shared.Return(list2);
		}
	}

	[TargetRpc]
	private void TargetRefreshItems(ItemIdentifier[] ids)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_InventorySystem_002EItems_002EItemIdentifier_005B_005D(writer, ids);
		this.SendTargetRPCInternal(null, "System.Void InventorySystem.Inventory::TargetRefreshItems(InventorySystem.Items.ItemIdentifier[])", 924996253, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	private void TargetRefreshAmmo(byte[] keys, ushort[] values)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBytesAndSize(keys);
		GeneratedNetworkCode._Write_System_002EUInt16_005B_005D(writer, values);
		this.SendTargetRPCInternal(null, "System.Void InventorySystem.Inventory::TargetRefreshAmmo(System.Byte[],System.UInt16[])", 1974569553, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[Command]
	private void CmdSelectItem(ushort itemSerial)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteUShort(itemSerial);
		base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdSelectItem(System.UInt16)", 599991551, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[Command(channel = 4)]
	private void CmdConfirmAcquisition(ushort[] itemSerials)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_System_002EUInt16_005B_005D(writer, itemSerials);
		base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdConfirmAcquisition(System.UInt16[])", 1903294111, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[Command(channel = 4)]
	private void CmdDropItem(ushort itemSerial, bool tryThrow)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteUShort(itemSerial);
		writer.WriteBool(tryThrow);
		base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdDropItem(System.UInt16,System.Boolean)", -146885871, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[Command(channel = 4)]
	public void CmdDropAmmo(byte ammoType, ushort amount)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		NetworkWriterExtensions.WriteByte(writer, ammoType);
		writer.WriteUShort(amount);
		base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdDropAmmo(System.Byte,System.UInt16)", 1230737334, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	public ItemBase CreateItemInstance(ItemIdentifier identifier, bool updateViewmodel)
	{
		if (!InventoryItemLoader.AvailableItems.TryGetValue(identifier.TypeId, out var value))
		{
			return null;
		}
		ItemBase itemBase = UnityEngine.Object.Instantiate(value, this.ItemWorkspace);
		itemBase.transform.localPosition = Vector3.zero;
		itemBase.transform.localRotation = Quaternion.identity;
		itemBase.Owner = this._hub;
		itemBase.ItemSerial = identifier.SerialNumber;
		if (updateViewmodel && itemBase.ViewModel != null)
		{
			ItemViewmodelBase itemViewmodelBase = UnityEngine.Object.Instantiate(itemBase.ViewModel, itemBase.transform);
			itemViewmodelBase.transform.localPosition = Vector3.zero;
			itemViewmodelBase.transform.localRotation = Quaternion.identity;
			itemViewmodelBase.InitLocal(itemBase);
			itemViewmodelBase.gameObject.SetActive(value: false);
			itemBase.ViewModel = itemViewmodelBase;
		}
		return itemBase;
	}

	public bool DestroyItemInstance(ushort targetInstance, ItemPickupBase pickup, out ItemBase foundItem)
	{
		if (!this.UserInventory.Items.TryGetValue(targetInstance, out foundItem))
		{
			return false;
		}
		foundItem.OnRemoved(pickup);
		if (this.CurInstance == foundItem)
		{
			this.CurInstance = null;
		}
		UnityEngine.Object.Destroy(foundItem.gameObject);
		return true;
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder, Action<string> categoryAdder)
	{
		this.DummyActionsDirty = false;
		foreach (KeyValuePair<ushort, ItemBase> item in this.UserInventory.Items)
		{
			ItemBase value = item.Value;
			IDummyActionProvider[] componentsInChildren = value.GetComponentsInChildren<IDummyActionProvider>();
			categoryAdder($"{value.ItemTypeId} (#{value.ItemSerial})");
			this.PopulateDummnyAction(value, componentsInChildren, actionAdder);
			categoryAdder($"{value.ItemTypeId} (ANY)");
			this.PopulateDummnyAction(value, componentsInChildren, actionAdder);
		}
	}

	private void PopulateDummnyAction(ItemBase ib, IDummyActionProvider[] providers, Action<DummyAction> actionAdder)
	{
		if (ib.AllowEquip && !ib.IsEquipped)
		{
			actionAdder(new DummyAction("Equip", delegate
			{
				this.ServerSelectItem(ib.ItemSerial);
			}));
		}
		if (ib.AllowHolster && ib.IsEquipped)
		{
			actionAdder(new DummyAction("Holster", delegate
			{
				this.ServerSelectItem(0);
			}));
		}
		if (ib.AllowDropping)
		{
			actionAdder(new DummyAction("Drop", delegate
			{
				ib.ServerDropItem(spawn: true);
			}));
		}
		for (int num = 0; num < providers.Length; num++)
		{
			providers[num].PopulateDummyActions(actionAdder);
		}
	}

	static Inventory()
	{
		Inventory.OnItemsModified = delegate
		{
		};
		Inventory.OnAmmoModified = delegate
		{
		};
		Inventory.OnCurrentItemChanged = delegate
		{
		};
		RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdSelectItem(System.UInt16)", InvokeUserCode_CmdSelectItem__UInt16, requiresAuthority: true);
		RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdConfirmAcquisition(System.UInt16[])", InvokeUserCode_CmdConfirmAcquisition__UInt16_005B_005D, requiresAuthority: true);
		RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdDropItem(System.UInt16,System.Boolean)", InvokeUserCode_CmdDropItem__UInt16__Boolean, requiresAuthority: true);
		RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdDropAmmo(System.Byte,System.UInt16)", InvokeUserCode_CmdDropAmmo__Byte__UInt16, requiresAuthority: true);
		RemoteProcedureCalls.RegisterRpc(typeof(Inventory), "System.Void InventorySystem.Inventory::TargetRefreshItems(InventorySystem.Items.ItemIdentifier[])", InvokeUserCode_TargetRefreshItems__ItemIdentifier_005B_005D);
		RemoteProcedureCalls.RegisterRpc(typeof(Inventory), "System.Void InventorySystem.Inventory::TargetRefreshAmmo(System.Byte[],System.UInt16[])", InvokeUserCode_TargetRefreshAmmo__Byte_005B_005D__UInt16_005B_005D);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetRefreshItems__ItemIdentifier_005B_005D(ItemIdentifier[] ids)
	{
		Queue<ItemIdentifier> queue = new Queue<ItemIdentifier>();
		List<ushort> list = this.UserInventory.Items.Keys.ToList();
		int num = 0;
		for (int i = 0; i < ids.Length; i++)
		{
			ItemIdentifier item = ids[i];
			if (!this.UserInventory.Items.Keys.Contains(item.SerialNumber))
			{
				queue.Enqueue(item);
			}
			if (list.Contains(item.SerialNumber))
			{
				list.Remove(item.SerialNumber);
			}
		}
		while (list.Count > 0)
		{
			this.DestroyItemInstance(list[0], null, out var _);
			this.UserInventory.Items.Remove(list[0]);
			list.RemoveAt(0);
			num++;
		}
		List<ushort> list2 = ListPool<ushort>.Shared.Rent();
		while (queue.Count > 0)
		{
			ItemIdentifier itemIdentifier = queue.Dequeue();
			ItemBase itemBase = this.CreateItemInstance(itemIdentifier, updateViewmodel: true);
			this.UserInventory.Items[itemIdentifier.SerialNumber] = itemBase;
			itemBase.OnAdded(null);
			if (itemBase is IAcquisitionConfirmationTrigger)
			{
				list2.Add(itemIdentifier.SerialNumber);
			}
			if (itemIdentifier == this.CurItem)
			{
				this.CurInstance = itemBase;
			}
			num++;
		}
		if (list2.Count > 0)
		{
			this.CmdConfirmAcquisition(list2.ToArray());
		}
		ListPool<ushort>.Shared.Return(list2);
		if (num > 0 && base.isLocalPlayer)
		{
			Inventory.OnItemsModified?.Invoke(this._hub);
		}
	}

	protected static void InvokeUserCode_TargetRefreshItems__ItemIdentifier_005B_005D(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("TargetRPC TargetRefreshItems called on server.");
		}
		else
		{
			((Inventory)obj).UserCode_TargetRefreshItems__ItemIdentifier_005B_005D(GeneratedNetworkCode._Read_InventorySystem_002EItems_002EItemIdentifier_005B_005D(reader));
		}
	}

	protected void UserCode_TargetRefreshAmmo__Byte_005B_005D__UInt16_005B_005D(byte[] keys, ushort[] values)
	{
		if (keys.Length == values.Length)
		{
			this.UserInventory.ReserveAmmo.Clear();
			for (int i = 0; i < keys.Length; i++)
			{
				this.UserInventory.ReserveAmmo[(ItemType)keys[i]] = values[i];
			}
			Inventory.OnAmmoModified?.Invoke(this._hub);
		}
	}

	protected static void InvokeUserCode_TargetRefreshAmmo__Byte_005B_005D__UInt16_005B_005D(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("TargetRPC TargetRefreshAmmo called on server.");
		}
		else
		{
			((Inventory)obj).UserCode_TargetRefreshAmmo__Byte_005B_005D__UInt16_005B_005D(reader.ReadBytesAndSize(), GeneratedNetworkCode._Read_System_002EUInt16_005B_005D(reader));
		}
	}

	protected void UserCode_CmdSelectItem__UInt16(ushort itemSerial)
	{
		if (!this._hub.interCoordinator.AnyBlocker(BlockedInteraction.OpenInventory))
		{
			this.ServerSelectItem(itemSerial);
		}
	}

	protected static void InvokeUserCode_CmdSelectItem__UInt16(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogError("Command CmdSelectItem called on client.");
		}
		else
		{
			((Inventory)obj).UserCode_CmdSelectItem__UInt16(reader.ReadUShort());
		}
	}

	protected void UserCode_CmdConfirmAcquisition__UInt16_005B_005D(ushort[] itemSerials)
	{
		foreach (ushort key in itemSerials)
		{
			if (this.UserInventory.Items.TryGetValue(key, out var value) && value is IAcquisitionConfirmationTrigger { AcquisitionAlreadyReceived: false } acquisitionConfirmationTrigger)
			{
				acquisitionConfirmationTrigger.ServerConfirmAcqusition();
				acquisitionConfirmationTrigger.AcquisitionAlreadyReceived = true;
			}
		}
	}

	protected static void InvokeUserCode_CmdConfirmAcquisition__UInt16_005B_005D(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogError("Command CmdConfirmAcquisition called on client.");
		}
		else
		{
			((Inventory)obj).UserCode_CmdConfirmAcquisition__UInt16_005B_005D(GeneratedNetworkCode._Read_System_002EUInt16_005B_005D(reader));
		}
	}

	protected void UserCode_CmdDropItem__UInt16__Boolean(ushort itemSerial, bool tryThrow)
	{
		if (!this.UserInventory.Items.TryGetValue(itemSerial, out var value) || !value.AllowDropping)
		{
			return;
		}
		PlayerDroppingItemEventArgs e = new PlayerDroppingItemEventArgs(this._hub, value, tryThrow);
		PlayerEvents.OnDroppingItem(e);
		if (!e.IsAllowed)
		{
			return;
		}
		tryThrow = e.Throw;
		ItemPickupBase itemPickupBase = this.ServerDropItem(itemSerial);
		PlayerDroppedItemEventArgs e2 = new PlayerDroppedItemEventArgs(this._hub, itemPickupBase, tryThrow);
		PlayerEvents.OnDroppedItem(e2);
		tryThrow = e2.Throw;
		this.SendItemsNextFrame = true;
		if (!tryThrow || itemPickupBase == null || !itemPickupBase.TryGetComponent<Rigidbody>(out var component))
		{
			return;
		}
		PlayerThrowingItemEventArgs e3 = new PlayerThrowingItemEventArgs(this._hub, itemPickupBase, component);
		PlayerEvents.OnThrowingItem(e3);
		if (e3.IsAllowed)
		{
			Vector3 velocity = this._hub.GetVelocity();
			Vector3 linearVelocity = velocity / 3f + this._hub.PlayerCameraReference.forward * 6f * (Mathf.Clamp01(Mathf.InverseLerp(7f, 0.1f, component.mass)) + 0.3f);
			linearVelocity.x = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(linearVelocity.x)) * (float)((!(linearVelocity.x < 0f)) ? 1 : (-1));
			linearVelocity.y = Mathf.Max(Mathf.Abs(velocity.y), Mathf.Abs(linearVelocity.y)) * (float)((!(linearVelocity.y < 0f)) ? 1 : (-1));
			linearVelocity.z = Mathf.Max(Mathf.Abs(velocity.z), Mathf.Abs(linearVelocity.z)) * (float)((!(linearVelocity.z < 0f)) ? 1 : (-1));
			component.position = this._hub.PlayerCameraReference.position;
			component.linearVelocity = linearVelocity;
			component.angularVelocity = Vector3.Lerp(value.ThrowSettings.RandomTorqueA, value.ThrowSettings.RandomTorqueB, UnityEngine.Random.value);
			float magnitude = component.angularVelocity.magnitude;
			if (magnitude > component.maxAngularVelocity)
			{
				component.maxAngularVelocity = magnitude;
			}
			PlayerEvents.OnThrewItem(new PlayerThrewItemEventArgs(this._hub, itemPickupBase, component));
		}
	}

	protected static void InvokeUserCode_CmdDropItem__UInt16__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogError("Command CmdDropItem called on client.");
		}
		else
		{
			((Inventory)obj).UserCode_CmdDropItem__UInt16__Boolean(reader.ReadUShort(), reader.ReadBool());
		}
	}

	protected void UserCode_CmdDropAmmo__Byte__UInt16(byte ammoType, ushort amount)
	{
		this.ServerDropAmmo((ItemType)ammoType, amount, checkMinimals: true);
	}

	protected static void InvokeUserCode_CmdDropAmmo__Byte__UInt16(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogError("Command CmdDropAmmo called on client.");
		}
		else
		{
			((Inventory)obj).UserCode_CmdDropAmmo__Byte__UInt16(NetworkReaderExtensions.ReadByte(reader), reader.ReadUShort());
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EItemIdentifier(writer, this.CurItem);
			writer.WriteFloat(this._syncStaminaModifier);
			writer.WriteFloat(this._syncMovementLimiter);
			writer.WriteFloat(this._syncMovementMultiplier);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EItemIdentifier(writer, this.CurItem);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteFloat(this._syncStaminaModifier);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteFloat(this._syncMovementLimiter);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteFloat(this._syncMovementMultiplier);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.CurItem, OnItemUpdated, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EItemIdentifier(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncStaminaModifier, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._syncMovementLimiter, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._syncMovementMultiplier, null, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.CurItem, OnItemUpdated, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EItemIdentifier(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncStaminaModifier, null, reader.ReadFloat());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncMovementLimiter, null, reader.ReadFloat());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncMovementMultiplier, null, reader.ReadFloat());
		}
	}
}
