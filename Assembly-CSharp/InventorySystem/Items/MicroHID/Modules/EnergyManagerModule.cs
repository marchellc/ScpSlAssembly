using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class EnergyManagerModule : MicroHidModuleBase
	{
		public float Energy
		{
			get
			{
				return EnergyManagerModule.GetEnergy(base.ItemSerial);
			}
		}

		public static float GetEnergy(ushort serial)
		{
			return EnergyManagerModule.SyncEnergy.GetValueOrDefault(serial, 1f);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			if (NetworkServer.active)
			{
				return;
			}
			while (reader.Remaining > 0)
			{
				ushort num = reader.ReadUShort();
				byte b = reader.ReadByte();
				EnergyManagerModule.SyncEnergy[num] = this.DecodeEnergy(b);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			EnergyManagerModule.SyncEnergy.Clear();
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (!base.IsServer)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteUShort(base.ItemSerial);
				x.WriteByte(this.EncodeEnergy(this.Energy));
			}, true);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsServer)
			{
				return;
			}
			float num = this.Energy;
			MicroHidPhase phase = base.MicroHid.CycleController.Phase;
			if (phase == MicroHidPhase.Standby)
			{
				return;
			}
			FiringModeControllerModule firingModeControllerModule;
			if (!base.MicroHid.CycleController.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			switch (phase)
			{
			case MicroHidPhase.WindingUp:
				num -= firingModeControllerModule.DrainRateWindUp * Time.deltaTime;
				break;
			case MicroHidPhase.WoundUpSustain:
				num -= firingModeControllerModule.DrainRateSustain * Time.deltaTime;
				break;
			case MicroHidPhase.Firing:
				num -= firingModeControllerModule.DrainRateFiring * Time.deltaTime;
				break;
			}
			this.ServerSetEnergy(base.ItemSerial, num);
		}

		private byte EncodeEnergy(float energy)
		{
			return (byte)(energy * 255f);
		}

		private float DecodeEnergy(byte compressed)
		{
			return (float)compressed / 255f;
		}

		public void ServerSetEnergy(ushort serial, float amount)
		{
			amount = Mathf.Clamp01(amount);
			EnergyManagerModule.SyncEnergy[serial] = amount;
			byte energyCompressed = this.EncodeEnergy(amount);
			if (energyCompressed == this._serverLastSentEnergy)
			{
				return;
			}
			this._serverLastSentEnergy = energyCompressed;
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteUShort(serial);
				x.WriteByte(energyCompressed);
			}, true);
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
			base.ServerOnPlayerConnected(hub, firstSubcomponent);
			if (!firstSubcomponent)
			{
				return;
			}
			this.SendRpc(hub, delegate(NetworkWriter x)
			{
				foreach (KeyValuePair<ushort, float> keyValuePair in EnergyManagerModule.SyncEnergy)
				{
					x.WriteUShort(keyValuePair.Key);
					x.WriteByte(this.EncodeEnergy(keyValuePair.Value));
				}
			});
		}

		private static readonly Dictionary<ushort, float> SyncEnergy = new Dictionary<ushort, float>();

		private byte _serverLastSentEnergy;
	}
}
