using GameCore;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Test;

public class TestItem : AutosyncItem
{
	public override float Weight => 1f;

	public static void Log(string msg)
	{
		Console.AddLog("TEST ITEM: " + msg, new Color(0.3765f, 0.7882f, 0.9019f));
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
			NetworkWriter writer;
			using (new AutosyncCmd(base.ItemId, out writer))
			{
				writer.WriteString("KNOCK KNOCK");
			}
		}
		if (!Input.GetKeyDown(KeyCode.J))
		{
			return;
		}
		TestItem.Log("Sending a sequence:");
		for (int i = 1; i <= 10; i++)
		{
			NetworkWriter writer2;
			using (new AutosyncCmd(base.ItemId, out writer2))
			{
				writer2.WriteString("Sequence: " + i);
			}
		}
	}

	public override void ServerConfirmAcqusition()
	{
		base.ServerConfirmAcqusition();
		TestItem.Log("Received acquisition confirmation of " + base.ItemSerial);
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (this.IsLocalPlayer && base.ViewModel is TestItemViewmodel testItemViewmodel)
		{
			testItemViewmodel.UpdateText(reader.ReadString());
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
			text = ((!(text2 == "KNOCK KNOCK")) ? ("Unknown - " + text2) : "Who's there?");
		}
		TestItem.Log("Sending response - " + text);
		NetworkWriter writer;
		using (new AutosyncRpc(base.ItemId, base.Owner, out writer))
		{
			writer.WriteString(text);
		}
	}
}
