using System;
using System.Collections.Generic;
using AudioPooling;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class DrawAndInspectorModule : MicroHidModuleBase
	{
		public static event Action<ushort> OnInspectRequested;

		public static bool CheckPickupPreference(ushort serial)
		{
			return DrawAndInspectorModule.PickupAnimSerials.Contains(serial);
		}

		public void ServerRegisterSerial(ushort serial)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(0);
				x.WriteUShort(serial);
			}, true);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				while (reader.Remaining > 0)
				{
					DrawAndInspectorModule.PickupAnimSerials.Add(reader.ReadUShort());
				}
				return;
			case 1:
				break;
			case 2:
			{
				DrawAndInspectorModule.PickupAnimSerials.Remove(serial);
				using (List<AudioPoolSession>.Enumerator enumerator = AudioManagerModule.GetController(serial).ActiveSessions.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AudioPoolSession audioPoolSession = enumerator.Current;
						AudioClip clip = audioPoolSession.Source.clip;
						if (!(clip != this._equipSound) || !(clip != this._pickupSound))
						{
							audioPoolSession.Source.Stop();
						}
					}
					return;
				}
				break;
			}
			case 3:
			{
				Action<ushort> onInspectRequested = DrawAndInspectorModule.OnInspectRequested;
				if (onInspectRequested == null)
				{
					return;
				}
				onInspectRequested(serial);
				return;
			}
			default:
				return;
			}
			AudioManagerModule.GetController(serial).PlayOneShot(DrawAndInspectorModule.CheckPickupPreference(serial) ? this._pickupSound : this._equipSound, 10f, MixerChannel.NoDucking);
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			if (!base.IsServer)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(1);
			}, true);
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(2);
			}, true);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (base.IsLocalPlayer && base.GetActionDown(ActionName.InspectItem))
			{
				this.SendCmd(null);
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (base.ItemUsageBlocked)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(3);
			}, true);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			DrawAndInspectorModule.PickupAnimSerials.Clear();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
			base.ServerOnPlayerConnected(hub, firstSubcomponent);
			this.SendRpc(hub, delegate(NetworkWriter x)
			{
				x.WriteByte(0);
				foreach (ushort num in DrawAndInspectorModule.PickupAnimSerials)
				{
					x.WriteUShort(num);
				}
			});
		}

		private static readonly HashSet<ushort> PickupAnimSerials = new HashSet<ushort>();

		[SerializeField]
		private AudioClip _equipSound;

		[SerializeField]
		private AudioClip _pickupSound;

		private enum RpcType
		{
			AddPickup,
			OnEquipped,
			OnHolstered,
			InspectRequested
		}
	}
}
