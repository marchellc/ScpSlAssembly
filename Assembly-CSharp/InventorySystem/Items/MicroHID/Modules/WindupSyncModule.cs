using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class WindupSyncModule : MicroHidModuleBase
	{
		private void ServerUpdateController(CycleController cycle)
		{
			byte b = (byte)Mathf.RoundToInt(cycle.ServerWindUpProgress * 255f);
			byte b2;
			if (WindupSyncModule.ReceivedData.TryGetValue(cycle.Serial, out b2) && b2 == b)
			{
				return;
			}
			NetworkWriter networkWriter;
			using (new AutosyncRpc(new ItemIdentifier(base.ItemId.TypeId, cycle.Serial), out networkWriter))
			{
				networkWriter.WriteByte(base.SyncId);
				networkWriter.WriteByte(b);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			WindupSyncModule.ReceivedData.Clear();
		}

		internal override void TemplateUpdate()
		{
			base.TemplateUpdate();
			if (!NetworkServer.active)
			{
				return;
			}
			CycleSyncModule.ForEachController(new Action<CycleController>(this.ServerUpdateController));
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			WindupSyncModule.ReceivedData[serial] = reader.ReadByte();
		}

		public static float GetProgress(ushort serial)
		{
			return (float)WindupSyncModule.ReceivedData.GetValueOrDefault(serial) * 0.003921569f;
		}

		private static readonly Dictionary<ushort, byte> ReceivedData = new Dictionary<ushort, byte>();
	}
}
