using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules
{
	public class EventBasedEquipperModule : ModuleBase, IEquipperModule, IBusyIndicatorModule
	{
		public float DisplayBaseEquipTime { get; private set; }

		public bool RandomizeOnEquip { get; private set; }

		public bool IsBusy
		{
			get
			{
				return (!this.IsEquipped && !base.IsSpectator) || base.Firearm.IsHolstering;
			}
		}

		public bool IsEquipped { get; private set; }

		public override bool AllowCmdsWhileHolstered
		{
			get
			{
				return true;
			}
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			this.Randomize();
			this.ApplyFirstTimeAnims();
			if (base.IsLocalPlayer)
			{
				EventBasedEquipperModule.ClientAlreadyEquippedSerials.Add(base.ItemSerial);
			}
		}

		internal override void SpectatorInit()
		{
			base.SpectatorInit();
			this.Randomize();
			this.ApplyFirstTimeAnims();
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			this.ServerUpdateSeed();
			if (!base.IsLocalPlayer || EventBasedEquipperModule.ClientAlreadyEquippedSerials.Contains(base.ItemSerial))
			{
				return;
			}
			this.SendCmd(null);
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this.IsEquipped = false;
			this.ServerUpdateSeed();
			if (base.IsServer && EventBasedEquipperModule.SyncFirstEquips.Contains(base.ItemSerial))
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(EventBasedEquipperModule.RpcType.FirstTimeFalse);
				}, true);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			EventBasedEquipperModule.NextSeeds.Clear();
			EventBasedEquipperModule.SyncFirstEquips.Clear();
			EventBasedEquipperModule.ClientAlreadyEquippedSerials.Clear();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
		{
			base.ServerOnPlayerConnected(hub, firstModule);
			if (!firstModule)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(EventBasedEquipperModule.RpcType.ResyncAllFirstTime);
				EventBasedEquipperModule.SyncFirstEquips.ForEach(delegate(ushort s)
				{
					writer.WriteUShort(s);
				});
			}, true);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(EventBasedEquipperModule.RpcType.FirstTimeTrue);
			}, true);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				EventBasedEquipperModule.NextSeeds[serial] = reader.ReadUShort();
				return;
			case 1:
				EventBasedEquipperModule.SyncFirstEquips.Add(serial);
				return;
			case 2:
				EventBasedEquipperModule.SyncFirstEquips.Remove(serial);
				return;
			case 3:
				EventBasedEquipperModule.SyncFirstEquips.Clear();
				while (reader.Remaining > 0)
				{
					EventBasedEquipperModule.SyncFirstEquips.Add(reader.ReadUShort());
				}
				return;
			default:
				return;
			}
		}

		private void ServerUpdateSeed()
		{
			if (!base.IsServer || !this.RandomizeOnEquip)
			{
				return;
			}
			int rand = global::UnityEngine.Random.Range(0, 65535);
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(EventBasedEquipperModule.RpcType.SeedSync);
				x.WriteUShort((ushort)rand);
			}, true);
		}

		private void Randomize()
		{
			if (!this.RandomizeOnEquip)
			{
				return;
			}
			double num = new global::System.Random((int)EventBasedEquipperModule.NextSeeds.GetValueOrDefault(base.ItemSerial, base.ItemSerial)).NextDouble();
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, (float)num, false);
		}

		private void ApplyFirstTimeAnims()
		{
			if (!EventBasedEquipperModule.SyncFirstEquips.Contains(base.ItemSerial))
			{
				return;
			}
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.FirstTimePickup, true, true);
		}

		[ExposedFirearmEvent]
		public void Equip()
		{
			this.IsEquipped = true;
		}

		private static readonly HashSet<ushort> ClientAlreadyEquippedSerials = new HashSet<ushort>();

		private static readonly HashSet<ushort> SyncFirstEquips = new HashSet<ushort>();

		private static readonly Dictionary<ushort, ushort> NextSeeds = new Dictionary<ushort, ushort>();

		private enum RpcType
		{
			SeedSync,
			FirstTimeTrue,
			FirstTimeFalse,
			ResyncAllFirstTime
		}
	}
}
