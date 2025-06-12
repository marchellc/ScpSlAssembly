using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules;

public class EventBasedEquipperModule : ModuleBase, IEquipperModule, IBusyIndicatorModule
{
	private enum RpcType
	{
		SeedSync,
		FirstTimeTrue,
		FirstTimeFalse,
		ResyncAllFirstTime
	}

	private static readonly HashSet<ushort> ClientAlreadyEquippedSerials = new HashSet<ushort>();

	private static readonly HashSet<ushort> SyncFirstEquips = new HashSet<ushort>();

	private static readonly Dictionary<ushort, ushort> NextSeeds = new Dictionary<ushort, ushort>();

	[field: SerializeField]
	public float DisplayBaseEquipTime { get; private set; }

	[field: SerializeField]
	public bool RandomizeOnEquip { get; private set; }

	public bool IsBusy
	{
		get
		{
			if (this.IsEquipped || base.IsSpectator)
			{
				return base.Firearm.IsHolstering;
			}
			return true;
		}
	}

	public bool IsEquipped { get; private set; }

	public override bool AllowCmdsWhileHolstered => true;

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
		if (base.IsLocalPlayer && !EventBasedEquipperModule.ClientAlreadyEquippedSerials.Contains(base.ItemSerial))
		{
			this.SendCmd();
		}
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
				x.WriteSubheader(RpcType.FirstTimeFalse);
			});
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
			writer.WriteSubheader(RpcType.ResyncAllFirstTime);
			EventBasedEquipperModule.SyncFirstEquips.ForEach(delegate(ushort s)
			{
				writer.WriteUShort(s);
			});
		});
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteSubheader(RpcType.FirstTimeTrue);
		});
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.SeedSync:
			EventBasedEquipperModule.NextSeeds[serial] = reader.ReadUShort();
			break;
		case RpcType.FirstTimeTrue:
			EventBasedEquipperModule.SyncFirstEquips.Add(serial);
			break;
		case RpcType.FirstTimeFalse:
			EventBasedEquipperModule.SyncFirstEquips.Remove(serial);
			break;
		case RpcType.ResyncAllFirstTime:
			EventBasedEquipperModule.SyncFirstEquips.Clear();
			while (reader.Remaining > 0)
			{
				EventBasedEquipperModule.SyncFirstEquips.Add(reader.ReadUShort());
			}
			break;
		}
	}

	private void ServerUpdateSeed()
	{
		if (base.IsServer && this.RandomizeOnEquip)
		{
			int rand = UnityEngine.Random.Range(0, 65535);
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(RpcType.SeedSync);
				x.WriteUShort((ushort)rand);
			});
		}
	}

	private void Randomize()
	{
		if (this.RandomizeOnEquip)
		{
			double num = new System.Random(EventBasedEquipperModule.NextSeeds.GetValueOrDefault(base.ItemSerial, base.ItemSerial)).NextDouble();
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, (float)num);
		}
	}

	private void ApplyFirstTimeAnims()
	{
		if (EventBasedEquipperModule.SyncFirstEquips.Contains(base.ItemSerial))
		{
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.FirstTimePickup, b: true, checkIfExists: true);
		}
	}

	[ExposedFirearmEvent]
	public void Equip()
	{
		this.IsEquipped = true;
	}
}
