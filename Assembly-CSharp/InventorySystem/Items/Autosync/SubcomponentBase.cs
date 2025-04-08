using System;
using System.Linq;
using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Autosync
{
	public abstract class SubcomponentBase : MonoBehaviour, IAutosyncReceiver, IIdentifierProvider
	{
		public virtual bool AllowCmdsWhileHolstered
		{
			get
			{
				return false;
			}
		}

		public int UniqueComponentId { get; private set; }

		public bool Initialized { get; private set; }

		public ModularAutosyncItem Item { get; private set; }

		public byte SyncId { get; private set; }

		protected bool IsLocalPlayer
		{
			get
			{
				return this.Item.IsLocalPlayer;
			}
		}

		protected bool IsSpectator
		{
			get
			{
				return this.Item.IsSpectator;
			}
		}

		protected ushort ItemSerial
		{
			get
			{
				return this.Item.ItemSerial;
			}
		}

		protected bool IsServer
		{
			get
			{
				return this.Item.IsServer;
			}
		}

		protected bool HasViewmodel
		{
			get
			{
				return this.Item.HasViewmodel;
			}
		}

		protected bool PrimaryActionBlocked
		{
			get
			{
				return this.Item.PrimaryActionBlocked;
			}
		}

		protected bool ItemUsageBlocked
		{
			get
			{
				return this.Item.ItemUsageBlocked;
			}
		}

		public ItemIdentifier ItemId
		{
			get
			{
				return this.Item.ItemId;
			}
		}

		private bool GetActionFunc(Func<KeyCode, bool> func, ActionName action)
		{
			return InventoryGuiController.ItemsSafeForInteraction && func(NewInput.GetKey(action, KeyCode.None));
		}

		protected bool GetActionDown(ActionName action)
		{
			return this.GetActionFunc(new Func<KeyCode, bool>(Input.GetKeyDown), action);
		}

		protected bool GetAction(ActionName action)
		{
			return this.GetActionFunc(new Func<KeyCode, bool>(Input.GetKey), action);
		}

		protected virtual void SendRpc(Action<NetworkWriter> writerFunc = null, bool toAll = true)
		{
			if (toAll)
			{
				NetworkWriter networkWriter;
				using (new AutosyncRpc(this.ItemId, out networkWriter))
				{
					networkWriter.WriteByte(this.SyncId);
					if (writerFunc == null)
					{
						return;
					}
					writerFunc(networkWriter);
					return;
				}
			}
			this.SendRpc(this.Item.Owner, writerFunc);
		}

		protected virtual void SendRpc(Func<ReferenceHub, bool> condition, Action<NetworkWriter> writerFunc = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(this.ItemId, condition, out networkWriter))
			{
				networkWriter.WriteByte(this.SyncId);
				if (writerFunc != null)
				{
					writerFunc(networkWriter);
				}
			}
		}

		protected virtual void SendRpc(ReferenceHub targetPlayer, Action<NetworkWriter> writerFunc = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(this.ItemId, targetPlayer, out networkWriter))
			{
				networkWriter.WriteByte(this.SyncId);
				if (writerFunc != null)
				{
					writerFunc(networkWriter);
				}
			}
		}

		protected virtual void SendCmd(Action<NetworkWriter> writerFunc = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncCmd(this.ItemId, out networkWriter))
			{
				networkWriter.WriteByte(this.SyncId);
				if (writerFunc != null)
				{
					writerFunc(networkWriter);
				}
			}
		}

		protected virtual void OnValidate()
		{
			if (this.UniqueComponentId != 0)
			{
				return;
			}
			ModularAutosyncItem modularAutosyncItem;
			if (!base.transform.TryGetComponentInParent(out modularAutosyncItem))
			{
				return;
			}
			SubcomponentBase[] componentsInChildren = modularAutosyncItem.GetComponentsInChildren<SubcomponentBase>();
			int newId = base.GetInstanceID();
			while (newId == 0 || componentsInChildren.Any((SubcomponentBase x) => x.UniqueComponentId == newId))
			{
				int newId2 = newId;
				newId = newId2 + 1;
			}
			this.UniqueComponentId = newId;
		}

		internal void Init(ModularAutosyncItem item, byte syncIndex)
		{
			this.Item = item;
			this.SyncId = syncIndex;
			this.Initialized = true;
			this.OnInit();
		}

		internal virtual void SpectatorInit()
		{
		}

		internal virtual void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
		}

		internal virtual void ServerProcessMapgenDistribution(ItemPickupBase pickup)
		{
		}

		internal virtual void ServerProcessScp914Creation(ushort serial, Scp914KnobSetting knobSetting, Scp914Result scp914Result, ItemType itemType)
		{
		}

		internal virtual void OnClientReady()
		{
		}

		internal virtual void TemplateUpdate()
		{
		}

		internal virtual void OnAdded()
		{
		}

		internal virtual void OnEquipped()
		{
		}

		internal virtual void EquipUpdate()
		{
		}

		internal virtual void OnHolstered()
		{
		}

		internal virtual void OnRemoved(ItemPickupBase pickup)
		{
		}

		internal virtual void OnTemplateReloaded(ModularAutosyncItem template, bool wasEverLoaded)
		{
		}

		public virtual void ServerProcessCmd(NetworkReader reader)
		{
		}

		public virtual void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
		}

		public virtual void ClientProcessRpcInstance(NetworkReader reader)
		{
		}

		protected virtual void OnInit()
		{
		}
	}
}
