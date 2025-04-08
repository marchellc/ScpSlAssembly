using System;
using GameCore;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Test
{
	public class TestItem : AutosyncItem
	{
		public override float Weight
		{
			get
			{
				return 1f;
			}
		}

		public static void Log(string msg)
		{
			global::GameCore.Console.AddLog("TEST ITEM: " + msg, new Color(0.3765f, 0.7882f, 0.9019f), false, global::GameCore.Console.ConsoleLogType.Log);
		}

		public override void EquipUpdate()
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (Input.GetKeyDown(KeyCode.G))
			{
				TestItem.Log("Sending empty message");
				new AutosyncCmd(base.ItemId).Send();
			}
			if (Input.GetKeyDown(KeyCode.H))
			{
				TestItem.Log("Sending 'KNOCK KNOCK'");
				NetworkWriter networkWriter;
				using (new AutosyncCmd(base.ItemId, out networkWriter))
				{
					networkWriter.WriteString("KNOCK KNOCK");
				}
			}
			if (Input.GetKeyDown(KeyCode.J))
			{
				TestItem.Log("Sending a sequence:");
				for (int i = 1; i <= 10; i++)
				{
					NetworkWriter networkWriter2;
					using (new AutosyncCmd(base.ItemId, out networkWriter2))
					{
						networkWriter2.WriteString("Sequence: " + i.ToString());
					}
				}
			}
		}

		public override void ServerConfirmAcqusition()
		{
			base.ServerConfirmAcqusition();
			TestItem.Log("Received acquisition confirmation of " + base.ItemSerial.ToString());
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (this.IsLocalPlayer)
			{
				TestItemViewmodel testItemViewmodel = this.ViewModel as TestItemViewmodel;
				if (testItemViewmodel != null)
				{
					testItemViewmodel.UpdateText(reader.ReadString());
				}
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			string text;
			if (reader.Position >= reader.Capacity)
			{
				text = "Empty";
				TestItem.Log("Received an empty message.");
			}
			else
			{
				string text2 = reader.ReadString();
				TestItem.Log("Received a message - " + text2);
				if (text2 == "KNOCK KNOCK")
				{
					text = "Who's there?";
				}
				else
				{
					text = "Unknown - " + text2;
				}
			}
			TestItem.Log("Sending response - " + text);
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, base.Owner, out networkWriter))
			{
				networkWriter.WriteString(text);
			}
		}
	}
}
