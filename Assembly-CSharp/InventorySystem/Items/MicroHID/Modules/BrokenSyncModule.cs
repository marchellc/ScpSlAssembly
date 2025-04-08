using System;
using System.Collections.Generic;
using AudioPooling;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class BrokenSyncModule : MicroHidModuleBase
	{
		public static event Action<ushort> OnBroken;

		public bool Broken
		{
			get
			{
				return BrokenSyncModule.GetBroken(base.ItemSerial);
			}
		}

		public static bool GetBroken(ushort serial)
		{
			return BrokenSyncModule.BrokenSerialTimes.ContainsKey(serial);
		}

		public static bool TryGetBrokenElapsed(ushort serial, out float elapsed)
		{
			double num;
			if (BrokenSyncModule.BrokenSerialTimes.TryGetValue(serial, out num))
			{
				elapsed = (float)(NetworkTime.time - num);
				return true;
			}
			elapsed = 0f;
			return false;
		}

		public void ServerSetBroken(ushort serial, bool broken)
		{
			if (broken)
			{
				BrokenSyncModule.BrokenSerialTimes[serial] = NetworkTime.time;
			}
			else
			{
				BrokenSyncModule.BrokenSerialTimes.Remove(serial);
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				BrokenSyncModule.RpcType rpcType = (broken ? BrokenSyncModule.RpcType.BreakSpecific : BrokenSyncModule.RpcType.UnbreakSpecific);
				writer.WriteByte((byte)rpcType);
				writer.WriteUShort(serial);
			}, true);
		}

		public void ServerSetBroken()
		{
			if (!base.IsServer)
			{
				throw new InvalidOperationException("Attempting to set broken when NetworkServer is not active.");
			}
			this.ServerSetBroken(base.ItemSerial, true);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			if (NetworkServer.active)
			{
				return;
			}
			switch (reader.ReadByte())
			{
			case 0:
			{
				serial = reader.ReadUShort();
				bool flag = BrokenSyncModule.BrokenSerialTimes.ContainsKey(serial);
				BrokenSyncModule.BrokenSerialTimes[serial] = NetworkTime.time;
				if (!flag)
				{
					Action<ushort> onBroken = BrokenSyncModule.OnBroken;
					if (onBroken != null)
					{
						onBroken(serial);
					}
					CycleController cycleController = CycleSyncModule.GetCycleController(serial);
					AudioController controller = AudioManagerModule.GetController(serial);
					MicroHidPhase phase = cycleController.Phase;
					if (phase == MicroHidPhase.WoundUpSustain || phase == MicroHidPhase.WindingDown)
					{
						controller.PlayOneShot(this._explosionClip, this._explosionSoundRange, MixerChannel.Weapons);
						return;
					}
				}
				break;
			}
			case 1:
				BrokenSyncModule.BrokenSerialTimes.Clear();
				while (reader.Remaining > 0)
				{
					BrokenSyncModule.BrokenSerialTimes.Add(reader.ReadUShort(), 0.0);
				}
				break;
			case 2:
				BrokenSyncModule.BrokenSerialTimes.Remove(reader.ReadUShort());
				return;
			default:
				return;
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			BrokenSyncModule.BrokenSerialTimes.Clear();
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (base.IsServer && this.Broken)
			{
				this.ServerSetBroken();
			}
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
			base.ServerOnPlayerConnected(hub, firstSubcomponent);
			if (!firstSubcomponent)
			{
				return;
			}
			this.SendRpc(hub, delegate(NetworkWriter writer)
			{
				writer.WriteByte(1);
				BrokenSyncModule.BrokenSerialTimes.ForEachKey(new Action<ushort>(writer.WriteUShort));
			});
		}

		private static readonly Dictionary<ushort, double> BrokenSerialTimes = new Dictionary<ushort, double>();

		[SerializeField]
		private AudioClip _explosionClip;

		[SerializeField]
		private float _explosionSoundRange;

		private enum RpcType
		{
			BreakSpecific,
			ResyncAll,
			UnbreakSpecific
		}
	}
}
