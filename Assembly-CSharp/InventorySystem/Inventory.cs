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
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem
{
	public class Inventory : NetworkBehaviour, IStaminaModifier, IMovementSpeedModifier
	{
		public static event Action<ReferenceHub> OnItemsModified;

		public static event Action<ReferenceHub> OnAmmoModified;

		public static event Action OnServerStarted;

		public static event Action OnLocalClientStarted;

		public static event Action<ReferenceHub, ItemIdentifier, ItemIdentifier> OnCurrentItemChanged;

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
						curInstance.ViewModel.gameObject.SetActive(false);
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
						this._curInstance.ViewModel.gameObject.SetActive(true);
						SharedHandsController.UpdateInstance(this._curInstance.ViewModel);
						this._curInstance.ViewModel.OnEquipped();
					}
					this._curInstance.OnEquipped();
					this._curInstance.IsEquipped = true;
				}
			}
		}

		public float LastItemSwitch
		{
			get
			{
				return (float)this._lastEquipSw.Elapsed.TotalSeconds;
			}
		}

		private Transform ItemWorkspace
		{
			get
			{
				return SharedHandsController.Singleton.transform;
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return true;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return true;
			}
		}

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

		public float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return !this.IsObserver && this._sprintingDisabled;
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
				return !NetworkServer.active && !base.isLocalPlayer;
			}
		}

		private bool HasViewmodel
		{
			get
			{
				return base.isLocalPlayer && this.CurInstance != null && this.CurInstance.ViewModel != null;
			}
		}

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
			if (!NetworkServer.active)
			{
				return;
			}
			HashSet<ushort> hashSet = HashSetPool<ushort>.Shared.Rent();
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in this.UserInventory.Items)
			{
				hashSet.Add(keyValuePair.Key);
			}
			foreach (ushort num in hashSet)
			{
				this.ServerRemoveItem(num, null);
			}
			HashSetPool<ushort>.Shared.Return(hashSet);
		}

		private void Start()
		{
			if (!base.isLocalPlayer && !NetworkServer.active)
			{
				return;
			}
			if (base.isLocalPlayer)
			{
				if (NetworkServer.active)
				{
					Inventory.OnServerStarted();
					CustomNetworkManager.InvokeOnClientReady();
				}
				Action onLocalClientStarted = Inventory.OnLocalClientStarted;
				if (onLocalClientStarted == null)
				{
					return;
				}
				onLocalClientStarted();
			}
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				if (this.SendItemsNextFrame)
				{
					this.SendItemsNextFrame = false;
					Action<ReferenceHub> onItemsModified = Inventory.OnItemsModified;
					if (onItemsModified != null)
					{
						onItemsModified(this._hub);
					}
					this.ServerSendItems();
				}
				if (this.SendAmmoNextFrame)
				{
					this.SendAmmoNextFrame = false;
					Action<ReferenceHub> onAmmoModified = Inventory.OnAmmoModified;
					if (onAmmoModified != null)
					{
						onAmmoModified(this._hub);
					}
					this.ServerSendAmmo();
				}
			}
			if (this._prevCurItem != this.CurItem)
			{
				if (base.isLocalPlayer)
				{
					ItemBase itemBase;
					if (this.UserInventory.Items.TryGetValue(this._prevCurItem.SerialNumber, out itemBase))
					{
						this.CurInstance = null;
					}
					ItemBase itemBase2;
					if (this.UserInventory.Items.TryGetValue(this.CurItem.SerialNumber, out itemBase2))
					{
						this.CurInstance = itemBase2;
					}
				}
				Action<ReferenceHub, ItemIdentifier, ItemIdentifier> onCurrentItemChanged = Inventory.OnCurrentItemChanged;
				if (onCurrentItemChanged != null)
				{
					onCurrentItemChanged(this._hub, this._prevCurItem, this.CurItem);
				}
				this._prevCurItem = new ItemIdentifier(this.CurItem.TypeId, this.CurItem.SerialNumber);
			}
			if (this.IsObserver)
			{
				return;
			}
			if (this.CurInstance != null && this.CurInstance.enabled)
			{
				this.CurInstance.EquipUpdate();
			}
			foreach (ItemBase itemBase3 in this.UserInventory.Items.Values)
			{
				if (itemBase3.enabled)
				{
					itemBase3.AlwaysUpdate();
				}
			}
			this.RefreshModifiers();
			if (!this.HasViewmodel || !Input.GetKeyDown(NewInput.GetKey(ActionName.ThrowItem, KeyCode.None)))
			{
				return;
			}
			if (!this.CurInstance.AllowDropping || !InventoryGuiController.ItemsSafeForInteraction)
			{
				return;
			}
			this.CmdDropItem(this.CurItem.SerialNumber, true);
		}

		private void RefreshModifiers()
		{
			this._staminaModifier = 1f;
			this._movementLimiter = float.MaxValue;
			this._movementMultiplier = 1f;
			this._sprintingDisabled = false;
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in this.UserInventory.Items)
			{
				object mobilityController = keyValuePair.Value.GetMobilityController();
				IStaminaModifier staminaModifier = mobilityController as IStaminaModifier;
				if (staminaModifier != null && staminaModifier.StaminaModifierActive)
				{
					this._staminaModifier *= staminaModifier.StaminaUsageMultiplier;
					this._sprintingDisabled |= staminaModifier.SprintingDisabled;
				}
				IMovementSpeedModifier movementSpeedModifier = mobilityController as IMovementSpeedModifier;
				if (movementSpeedModifier != null && movementSpeedModifier.MovementModifierActive)
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
				global::UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSelectItem(System.UInt16)' called when server was not active");
				return;
			}
			if (itemSerial == this.CurItem.SerialNumber)
			{
				return;
			}
			ItemBase itemBase = null;
			ItemBase itemBase2 = null;
			bool flag = this.CurItem.SerialNumber == 0 || (this.UserInventory.Items.TryGetValue(this.CurItem.SerialNumber, out itemBase) && this.CurInstance != null);
			if (itemSerial != 0 && !this.UserInventory.Items.TryGetValue(itemSerial, out itemBase2))
			{
				if (!flag)
				{
					this.NetworkCurItem = ItemIdentifier.None;
					if (!base.isLocalPlayer)
					{
						this.CurInstance = null;
					}
				}
				return;
			}
			if (this.CurItem.SerialNumber > 0 && flag && !itemBase.AllowHolster)
			{
				return;
			}
			if (itemSerial != 0 && !itemBase2.AllowEquip)
			{
				return;
			}
			PlayerChangingItemEventArgs playerChangingItemEventArgs = new PlayerChangingItemEventArgs(this._hub, itemBase, itemBase2);
			PlayerEvents.OnChangingItem(playerChangingItemEventArgs);
			if (!playerChangingItemEventArgs.IsAllowed)
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
				this.NetworkCurItem = new ItemIdentifier(itemBase2.ItemTypeId, itemSerial);
				if (!base.isLocalPlayer)
				{
					this.CurInstance = itemBase2;
				}
			}
			PlayerEvents.OnChangedItem(new PlayerChangedItemEventArgs(this._hub, itemBase, itemBase2));
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
				global::UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSendItems()' called when server was not active");
				return;
			}
			if (base.isLocalPlayer)
			{
				return;
			}
			HashSet<ItemIdentifier> hashSet = HashSetPool<ItemIdentifier>.Shared.Rent();
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in this.UserInventory.Items)
			{
				hashSet.Add(new ItemIdentifier(keyValuePair.Value.ItemTypeId, keyValuePair.Key));
			}
			this.TargetRefreshItems(hashSet.ToArray<ItemIdentifier>());
			HashSetPool<ItemIdentifier>.Shared.Return(hashSet);
		}

		[Server]
		private void ServerSendAmmo()
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogWarning("[Server] function 'System.Void InventorySystem.Inventory::ServerSendAmmo()' called when server was not active");
				return;
			}
			if (base.isLocalPlayer)
			{
				return;
			}
			List<byte> list = ListPool<byte>.Shared.Rent();
			List<ushort> list2 = ListPool<ushort>.Shared.Rent();
			foreach (KeyValuePair<ItemType, ushort> keyValuePair in this.UserInventory.ReserveAmmo)
			{
				list.Add((byte)keyValuePair.Key);
				list2.Add(keyValuePair.Value);
			}
			this.TargetRefreshAmmo(list.ToArray(), list2.ToArray());
			ListPool<byte>.Shared.Return(list);
			ListPool<ushort>.Shared.Return(list2);
		}

		[TargetRpc]
		private void TargetRefreshItems(ItemIdentifier[] ids)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.ItemIdentifier[](networkWriterPooled, ids);
			this.SendTargetRPCInternal(null, "System.Void InventorySystem.Inventory::TargetRefreshItems(InventorySystem.Items.ItemIdentifier[])", 924996253, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[TargetRpc]
		private void TargetRefreshAmmo(byte[] keys, ushort[] values)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteBytesAndSize(keys);
			global::Mirror.GeneratedNetworkCode._Write_System.UInt16[](networkWriterPooled, values);
			this.SendTargetRPCInternal(null, "System.Void InventorySystem.Inventory::TargetRefreshAmmo(System.Byte[],System.UInt16[])", 1974569553, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Command]
		private void CmdSelectItem(ushort itemSerial)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteUShort(itemSerial);
			base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdSelectItem(System.UInt16)", 599991551, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Command(channel = 4)]
		private void CmdConfirmAcquisition(ushort[] itemSerials)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			global::Mirror.GeneratedNetworkCode._Write_System.UInt16[](networkWriterPooled, itemSerials);
			base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdConfirmAcquisition(System.UInt16[])", 1903294111, networkWriterPooled, 4, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Command(channel = 4)]
		private void CmdDropItem(ushort itemSerial, bool tryThrow)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteUShort(itemSerial);
			networkWriterPooled.WriteBool(tryThrow);
			base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdDropItem(System.UInt16,System.Boolean)", -146885871, networkWriterPooled, 4, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Command(channel = 4)]
		public void CmdDropAmmo(byte ammoType, ushort amount)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteByte(ammoType);
			networkWriterPooled.WriteUShort(amount);
			base.SendCommandInternal("System.Void InventorySystem.Inventory::CmdDropAmmo(System.Byte,System.UInt16)", 1230737334, networkWriterPooled, 4, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public ItemBase CreateItemInstance(ItemIdentifier identifier, bool updateViewmodel)
		{
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(identifier.TypeId, out itemBase))
			{
				return null;
			}
			ItemBase itemBase2 = global::UnityEngine.Object.Instantiate<ItemBase>(itemBase, this.ItemWorkspace);
			itemBase2.transform.localPosition = Vector3.zero;
			itemBase2.transform.localRotation = Quaternion.identity;
			itemBase2.Owner = this._hub;
			itemBase2.ItemSerial = identifier.SerialNumber;
			if (updateViewmodel && itemBase2.ViewModel != null)
			{
				ItemViewmodelBase itemViewmodelBase = global::UnityEngine.Object.Instantiate<ItemViewmodelBase>(itemBase2.ViewModel, itemBase2.transform);
				itemViewmodelBase.transform.localPosition = Vector3.zero;
				itemViewmodelBase.transform.localRotation = Quaternion.identity;
				itemViewmodelBase.InitLocal(itemBase2);
				itemViewmodelBase.gameObject.SetActive(false);
				itemBase2.ViewModel = itemViewmodelBase;
			}
			return itemBase2;
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
			global::UnityEngine.Object.Destroy(foundItem.gameObject);
			return true;
		}

		static Inventory()
		{
			Inventory.OnItemsModified = delegate(ReferenceHub userHub)
			{
			};
			Inventory.OnAmmoModified = delegate(ReferenceHub userHub)
			{
			};
			Inventory.OnCurrentItemChanged = delegate(ReferenceHub userHub, ItemIdentifier prevItem, ItemIdentifier newItem)
			{
			};
			RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdSelectItem(System.UInt16)", new RemoteCallDelegate(Inventory.InvokeUserCode_CmdSelectItem__UInt16), true);
			RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdConfirmAcquisition(System.UInt16[])", new RemoteCallDelegate(Inventory.InvokeUserCode_CmdConfirmAcquisition__UInt16[]), true);
			RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdDropItem(System.UInt16,System.Boolean)", new RemoteCallDelegate(Inventory.InvokeUserCode_CmdDropItem__UInt16__Boolean), true);
			RemoteProcedureCalls.RegisterCommand(typeof(Inventory), "System.Void InventorySystem.Inventory::CmdDropAmmo(System.Byte,System.UInt16)", new RemoteCallDelegate(Inventory.InvokeUserCode_CmdDropAmmo__Byte__UInt16), true);
			RemoteProcedureCalls.RegisterRpc(typeof(Inventory), "System.Void InventorySystem.Inventory::TargetRefreshItems(InventorySystem.Items.ItemIdentifier[])", new RemoteCallDelegate(Inventory.InvokeUserCode_TargetRefreshItems__ItemIdentifier[]));
			RemoteProcedureCalls.RegisterRpc(typeof(Inventory), "System.Void InventorySystem.Inventory::TargetRefreshAmmo(System.Byte[],System.UInt16[])", new RemoteCallDelegate(Inventory.InvokeUserCode_TargetRefreshAmmo__Byte[]__UInt16[]));
		}

		public override bool Weaved()
		{
			return true;
		}

		public ItemIdentifier NetworkCurItem
		{
			get
			{
				return this.CurItem;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<ItemIdentifier>(value, ref this.CurItem, 1UL, new Action<ItemIdentifier, ItemIdentifier>(this.OnItemUpdated));
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
				base.GeneratedSyncVarSetter<float>(value, ref this._syncStaminaModifier, 2UL, null);
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
				base.GeneratedSyncVarSetter<float>(value, ref this._syncMovementLimiter, 4UL, null);
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
				base.GeneratedSyncVarSetter<float>(value, ref this._syncMovementMultiplier, 8UL, null);
			}
		}

		protected void UserCode_TargetRefreshItems__ItemIdentifier[](ItemIdentifier[] ids)
		{
			Queue<ItemIdentifier> queue = new Queue<ItemIdentifier>();
			List<ushort> list = this.UserInventory.Items.Keys.ToList<ushort>();
			int num = 0;
			foreach (ItemIdentifier itemIdentifier in ids)
			{
				if (!this.UserInventory.Items.Keys.Contains(itemIdentifier.SerialNumber))
				{
					queue.Enqueue(itemIdentifier);
				}
				if (list.Contains(itemIdentifier.SerialNumber))
				{
					list.Remove(itemIdentifier.SerialNumber);
				}
			}
			while (list.Count > 0)
			{
				ItemBase itemBase;
				this.DestroyItemInstance(list[0], null, out itemBase);
				this.UserInventory.Items.Remove(list[0]);
				list.RemoveAt(0);
				num++;
			}
			List<ushort> list2 = ListPool<ushort>.Shared.Rent();
			while (queue.Count > 0)
			{
				ItemIdentifier itemIdentifier2 = queue.Dequeue();
				ItemBase itemBase2 = this.CreateItemInstance(itemIdentifier2, true);
				this.UserInventory.Items[itemIdentifier2.SerialNumber] = itemBase2;
				itemBase2.OnAdded(null);
				if (itemBase2 is IAcquisitionConfirmationTrigger)
				{
					list2.Add(itemIdentifier2.SerialNumber);
				}
				if (itemIdentifier2 == this.CurItem)
				{
					this.CurInstance = itemBase2;
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
				Action<ReferenceHub> onItemsModified = Inventory.OnItemsModified;
				if (onItemsModified == null)
				{
					return;
				}
				onItemsModified(this._hub);
			}
		}

		protected static void InvokeUserCode_TargetRefreshItems__ItemIdentifier[](NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				global::UnityEngine.Debug.LogError("TargetRPC TargetRefreshItems called on server.");
				return;
			}
			((Inventory)obj).UserCode_TargetRefreshItems__ItemIdentifier[](global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.ItemIdentifier[](reader));
		}

		protected void UserCode_TargetRefreshAmmo__Byte[]__UInt16[](byte[] keys, ushort[] values)
		{
			if (keys.Length != values.Length)
			{
				return;
			}
			this.UserInventory.ReserveAmmo.Clear();
			for (int i = 0; i < keys.Length; i++)
			{
				this.UserInventory.ReserveAmmo[(ItemType)keys[i]] = values[i];
			}
			Action<ReferenceHub> onAmmoModified = Inventory.OnAmmoModified;
			if (onAmmoModified == null)
			{
				return;
			}
			onAmmoModified(this._hub);
		}

		protected static void InvokeUserCode_TargetRefreshAmmo__Byte[]__UInt16[](NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				global::UnityEngine.Debug.LogError("TargetRPC TargetRefreshAmmo called on server.");
				return;
			}
			((Inventory)obj).UserCode_TargetRefreshAmmo__Byte[]__UInt16[](reader.ReadBytesAndSize(), global::Mirror.GeneratedNetworkCode._Read_System.UInt16[](reader));
		}

		protected void UserCode_CmdSelectItem__UInt16(ushort itemSerial)
		{
			if (this._hub.interCoordinator.AnyBlocker(BlockedInteraction.OpenInventory))
			{
				return;
			}
			this.ServerSelectItem(itemSerial);
		}

		protected static void InvokeUserCode_CmdSelectItem__UInt16(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogError("Command CmdSelectItem called on client.");
				return;
			}
			((Inventory)obj).UserCode_CmdSelectItem__UInt16(reader.ReadUShort());
		}

		protected void UserCode_CmdConfirmAcquisition__UInt16[](ushort[] itemSerials)
		{
			foreach (ushort num in itemSerials)
			{
				ItemBase itemBase;
				if (this.UserInventory.Items.TryGetValue(num, out itemBase))
				{
					IAcquisitionConfirmationTrigger acquisitionConfirmationTrigger = itemBase as IAcquisitionConfirmationTrigger;
					if (acquisitionConfirmationTrigger != null && !acquisitionConfirmationTrigger.AcquisitionAlreadyReceived)
					{
						acquisitionConfirmationTrigger.ServerConfirmAcqusition();
						acquisitionConfirmationTrigger.AcquisitionAlreadyReceived = true;
					}
				}
			}
		}

		protected static void InvokeUserCode_CmdConfirmAcquisition__UInt16[](NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogError("Command CmdConfirmAcquisition called on client.");
				return;
			}
			((Inventory)obj).UserCode_CmdConfirmAcquisition__UInt16[](global::Mirror.GeneratedNetworkCode._Read_System.UInt16[](reader));
		}

		protected void UserCode_CmdDropItem__UInt16__Boolean(ushort itemSerial, bool tryThrow)
		{
			ItemBase itemBase;
			if (!this.UserInventory.Items.TryGetValue(itemSerial, out itemBase) || !itemBase.AllowDropping)
			{
				return;
			}
			PlayerDroppingItemEventArgs playerDroppingItemEventArgs = new PlayerDroppingItemEventArgs(this._hub, itemBase);
			PlayerEvents.OnDroppingItem(playerDroppingItemEventArgs);
			if (!playerDroppingItemEventArgs.IsAllowed)
			{
				return;
			}
			ItemPickupBase itemPickupBase = this.ServerDropItem(itemSerial);
			PlayerEvents.OnDroppedItem(new PlayerDroppedItemEventArgs(this._hub, itemPickupBase));
			this.SendItemsNextFrame = true;
			Rigidbody rigidbody;
			if (!tryThrow || itemPickupBase == null || !itemPickupBase.TryGetComponent<Rigidbody>(out rigidbody))
			{
				return;
			}
			PlayerThrowingItemEventArgs playerThrowingItemEventArgs = new PlayerThrowingItemEventArgs(this._hub, itemPickupBase, rigidbody);
			PlayerEvents.OnThrowingItem(playerThrowingItemEventArgs);
			if (!playerThrowingItemEventArgs.IsAllowed)
			{
				return;
			}
			Vector3 velocity = this._hub.GetVelocity();
			Vector3 vector = velocity / 3f + this._hub.PlayerCameraReference.forward * 6f * (Mathf.Clamp01(Mathf.InverseLerp(7f, 0.1f, rigidbody.mass)) + 0.3f);
			vector.x = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(vector.x)) * (float)((vector.x < 0f) ? (-1) : 1);
			vector.y = Mathf.Max(Mathf.Abs(velocity.y), Mathf.Abs(vector.y)) * (float)((vector.y < 0f) ? (-1) : 1);
			vector.z = Mathf.Max(Mathf.Abs(velocity.z), Mathf.Abs(vector.z)) * (float)((vector.z < 0f) ? (-1) : 1);
			rigidbody.position = this._hub.PlayerCameraReference.position;
			rigidbody.velocity = vector;
			rigidbody.angularVelocity = Vector3.Lerp(itemBase.ThrowSettings.RandomTorqueA, itemBase.ThrowSettings.RandomTorqueB, global::UnityEngine.Random.value);
			float magnitude = rigidbody.angularVelocity.magnitude;
			if (magnitude > rigidbody.maxAngularVelocity)
			{
				rigidbody.maxAngularVelocity = magnitude;
			}
			PlayerEvents.OnThrewItem(new PlayerThrewItemEventArgs(this._hub, itemPickupBase, rigidbody));
		}

		protected static void InvokeUserCode_CmdDropItem__UInt16__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogError("Command CmdDropItem called on client.");
				return;
			}
			((Inventory)obj).UserCode_CmdDropItem__UInt16__Boolean(reader.ReadUShort(), reader.ReadBool());
		}

		protected void UserCode_CmdDropAmmo__Byte__UInt16(byte ammoType, ushort amount)
		{
			this.ServerDropAmmo((ItemType)ammoType, amount, true);
		}

		protected static void InvokeUserCode_CmdDropAmmo__Byte__UInt16(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				global::UnityEngine.Debug.LogError("Command CmdDropAmmo called on client.");
				return;
			}
			((Inventory)obj).UserCode_CmdDropAmmo__Byte__UInt16(reader.ReadByte(), reader.ReadUShort());
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.ItemIdentifier(writer, this.CurItem);
				writer.WriteFloat(this._syncStaminaModifier);
				writer.WriteFloat(this._syncMovementLimiter);
				writer.WriteFloat(this._syncMovementMultiplier);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.ItemIdentifier(writer, this.CurItem);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteFloat(this._syncStaminaModifier);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteFloat(this._syncMovementLimiter);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteFloat(this._syncMovementMultiplier);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<ItemIdentifier>(ref this.CurItem, new Action<ItemIdentifier, ItemIdentifier>(this.OnItemUpdated), global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.ItemIdentifier(reader));
				base.GeneratedSyncVarDeserialize<float>(ref this._syncStaminaModifier, null, reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this._syncMovementLimiter, null, reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this._syncMovementMultiplier, null, reader.ReadFloat());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<ItemIdentifier>(ref this.CurItem, new Action<ItemIdentifier, ItemIdentifier>(this.OnItemUpdated), global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.ItemIdentifier(reader));
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._syncStaminaModifier, null, reader.ReadFloat());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._syncMovementLimiter, null, reader.ReadFloat());
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._syncMovementMultiplier, null, reader.ReadFloat());
			}
		}

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
	}
}
