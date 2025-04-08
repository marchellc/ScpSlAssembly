using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Autosync
{
	public abstract class ModularAutosyncItem : AutosyncItem, IExternalMobilityControllerItem, IZoomModifyingItem, ILightEmittingItem, IAmmoDropPreventer
	{
		public object DesignatedMobilityControllerClass
		{
			get
			{
				return this._modifiersCombiner;
			}
		}

		public bool IsHolstering
		{
			get
			{
				return this._holsterRequestTimer.Busy;
			}
		}

		public SubcomponentBase[] AllSubcomponents { get; protected set; }

		public AutosyncInstantiationStatus InstantiationStatus { get; set; }

		public bool HasOwner
		{
			get
			{
				return this.InstantiationStatus == AutosyncInstantiationStatus.InventoryInstance;
			}
		}

		public virtual bool HasViewmodel
		{
			get
			{
				return this.ViewModel != null;
			}
		}

		public bool IsSpectator
		{
			get
			{
				return this.HasViewmodel && this.ViewModel.IsSpectator;
			}
		}

		public bool PrimaryActionBlocked
		{
			get
			{
				return this.HasOwner && base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			}
		}

		public bool ItemUsageBlocked
		{
			get
			{
				return this.HasOwner && base.Owner.HasBlock(BlockedInteraction.ItemUsage);
			}
		}

		public bool IsServer { get; private set; }

		internal override bool IsLocalPlayer
		{
			get
			{
				return this.HasOwner && base.IsLocalPlayer;
			}
		}

		public float ZoomAmount
		{
			get
			{
				return this._modifiersCombiner.ZoomAmount;
			}
		}

		public float SensitivityScale
		{
			get
			{
				return this._modifiersCombiner.SensitivityScale;
			}
		}

		public bool IsEmittingLight
		{
			get
			{
				return this._modifiersCombiner.IsEmittingLight;
			}
		}

		public virtual void InitializeSubcomponents()
		{
			this._modifiersCombiner = new AutosyncModifiersCombiner(this);
			byte b = 0;
			while ((int)b < this.AllSubcomponents.Length)
			{
				SubcomponentBase subcomponentBase = this.AllSubcomponents[(int)b];
				try
				{
					subcomponentBase.Init(this, b);
				}
				catch (Exception ex)
				{
					if (subcomponentBase == null)
					{
						Debug.LogError("Null subcomponent on " + this.ItemTypeId.ToString() + " with index " + b.ToString());
					}
					else
					{
						Debug.LogError("Subcomponent " + subcomponentBase.name + " failed to init!");
					}
					Debug.LogException(ex);
				}
				b += 1;
			}
		}

		public sealed override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
			{
				x.ServerProcessCmd(reader);
			}, delegate
			{
				this.ServerProcessMainCmd(reader);
			}, true);
		}

		public sealed override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
			{
				x.ClientProcessRpcInstance(reader);
			}, delegate
			{
				this.ClientProcessMainRpcInstance(reader);
			}, false);
		}

		public sealed override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
			{
				x.ClientProcessRpcTemplate(reader, serial);
			}, delegate
			{
				this.ClientProcessMainRpcTemplate(reader, serial);
			}, false);
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			base.OnAdded(pickup);
			this.IsServer = NetworkServer.active;
			this.InstantiationStatus = AutosyncInstantiationStatus.InventoryInstance;
			this.InitializeSubcomponents();
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnAdded();
			}
		}

		public override void OnEquipped()
		{
			base.OnEquipped();
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnEquipped();
			}
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			this._holsterRequestTimer.Reset();
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnHolstered();
			}
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].EquipUpdate();
			}
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnRemoved(pickup);
			}
		}

		public override void OnHolsterRequestSent()
		{
			base.OnHolsterRequestSent();
			this._holsterRequestTimer.Trigger();
		}

		internal override void OnTemplateReloaded(bool wasEverLoaded)
		{
			base.OnTemplateReloaded(wasEverLoaded);
			this.InstantiationStatus = AutosyncInstantiationStatus.Template;
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnTemplateReloaded(this, wasEverLoaded);
			}
			if (wasEverLoaded)
			{
				return;
			}
			this.InitializeSubcomponents();
			CustomNetworkManager.OnClientReady += this.OnClientReady;
			StaticUnityMethods.OnUpdate += this.OnTemplateUpdate;
			ModularAutosyncItem.AllTemplates.Add(this);
		}

		protected virtual void OnClientReady()
		{
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].OnClientReady();
			}
		}

		protected virtual void OnTemplateUpdate()
		{
			SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].TemplateUpdate();
			}
		}

		protected virtual void ServerProcessMainCmd(NetworkReader reader)
		{
		}

		protected virtual void ClientProcessMainRpcInstance(NetworkReader reader)
		{
		}

		protected virtual void ClientProcessMainRpcTemplate(NetworkReader reader, ushort serial)
		{
		}

		public bool TryGetSubcomponentFromId(int id, out SubcomponentBase subcomponent)
		{
			if (this._subcomponentsByIdCache.TryGetValue(id, out subcomponent))
			{
				return true;
			}
			foreach (SubcomponentBase subcomponentBase in this.AllSubcomponents)
			{
				if (subcomponentBase.UniqueComponentId == id)
				{
					subcomponent = subcomponentBase;
					this._subcomponentsByIdCache[id] = subcomponentBase;
					return true;
				}
			}
			return false;
		}

		public bool TryGetSubcomponent<T>(out T ret)
		{
			foreach (SubcomponentBase subcomponentBase in this.AllSubcomponents)
			{
				if (subcomponentBase is T)
				{
					T t = subcomponentBase as T;
					ret = t;
					return true;
				}
			}
			ret = default(T);
			return false;
		}

		public bool ValidateAmmoDrop(ItemType id)
		{
			return this._modifiersCombiner.ValidateAmmoDrop(id);
		}

		private void DecodeAndProcessMessage(NetworkReader reader, Action<IAutosyncReceiver> interpreter, Action main, bool checkCmd = false)
		{
			byte b = reader.ReadByte();
			if (b == 255)
			{
				main();
				return;
			}
			SubcomponentBase subcomponentBase;
			if (!this.AllSubcomponents.TryGet((int)b, out subcomponentBase))
			{
				return;
			}
			if (checkCmd && !base.IsEquipped && !subcomponentBase.AllowCmdsWhileHolstered)
			{
				return;
			}
			interpreter(subcomponentBase);
		}

		private static void OnHubAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ModularAutosyncItem.NewPlayerSyncModuleTypes.Clear();
			foreach (ModularAutosyncItem modularAutosyncItem in ModularAutosyncItem.AllTemplates)
			{
				foreach (SubcomponentBase subcomponentBase in modularAutosyncItem.AllSubcomponents)
				{
					Type type = subcomponentBase.GetType();
					bool flag = ModularAutosyncItem.NewPlayerSyncModuleTypes.Add(type);
					subcomponentBase.ServerOnPlayerConnected(hub, flag);
				}
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(ModularAutosyncItem.OnHubAdded));
		}

		private AutosyncModifiersCombiner _modifiersCombiner;

		private readonly Dictionary<int, SubcomponentBase> _subcomponentsByIdCache = new Dictionary<int, SubcomponentBase>();

		private readonly ClientRequestTimer _holsterRequestTimer = new ClientRequestTimer();

		private static readonly HashSet<Type> NewPlayerSyncModuleTypes = new HashSet<Type>();

		private static readonly List<ModularAutosyncItem> AllTemplates = new List<ModularAutosyncItem>();

		public const byte MainSyncHeader = 255;
	}
}
