using System;
using System.Collections.Generic;
using NetworkManagerUtils.Dummies;
using UnityEngine;

namespace RemoteAdmin.Communication;

public class RaDummyActions : RaClientDataRequest
{
	private class DummyData
	{
		private int _groupsLen;

		private ActionsGroupPair[] _groups;

		private const int InitialGroupSize = 16;

		private const string InitialGroupName = "Miscellaneous";

		public ReadOnlySpan<ActionsGroupPair> Groups => new ReadOnlySpan<ActionsGroupPair>(this._groups, 0, this._groupsLen);

		private ActionsGroupPair CurGroup
		{
			get
			{
				this.EnsureCapacity(this._groupsLen);
				int num = Mathf.Max(0, this._groupsLen - 1);
				return this._groups[num];
			}
		}

		private void EnsureCapacity(int minLen)
		{
			if (this._groups == null)
			{
				int num = Mathf.Max(minLen * 2, 16);
				this._groups = new ActionsGroupPair[num];
			}
			else if (this._groups.Length < minLen)
			{
				Array.Resize(ref this._groups, minLen * 2);
			}
			for (int i = 0; i < this._groups.Length; i++)
			{
				ActionsGroupPair[] groups = this._groups;
				int num2 = i;
				if (groups[num2] == null)
				{
					groups[num2] = new ActionsGroupPair("Miscellaneous");
				}
			}
		}

		public void Clear()
		{
			if (this._groups != null)
			{
				for (int i = 0; i < this._groupsLen; i++)
				{
					ActionsGroupPair obj = this._groups[i];
					obj.Name = "Miscellaneous";
					obj.Actions.Clear();
				}
			}
			this._groupsLen = 0;
		}

		public void Receive(string word)
		{
			if (word.StartsWith("___"))
			{
				this._groupsLen++;
				ActionsGroupPair curGroup = this.CurGroup;
				int length = "___".Length;
				curGroup.Name = word.Substring(length, word.Length - length);
			}
			else
			{
				this.CurGroup.Actions.Add(word);
			}
		}
	}

	private class ActionsGroupPair
	{
		public string Name;

		public readonly List<string> Actions;

		public ActionsGroupPair(string name)
		{
			this.Name = name;
			this.Actions = new List<string>();
		}
	}

	private const string GroupPrefix = "___";

	private const string DummyIdPrefix = "***";

	private static readonly Dictionary<uint, HashSet<uint>> NonDirtyReceivers = new Dictionary<uint, HashSet<uint>>();

	private static readonly Dictionary<uint, DummyData> ReceivedData = new Dictionary<uint, DummyData>();

	private uint _senderNetId;

	public string LastError { get; private set; }

	public override int DataId => 9;

	public override void ReceiveData(string data, bool secure)
	{
		base.ReceiveData(data, secure);
		this.LastError = null;
		try
		{
			int num = data.IndexOf("***");
			string text = data.Substring(num, data.Length - num);
			this.ClientReceive(text.Split(','));
		}
		catch (Exception ex)
		{
			this.LastError = ex.Message;
		}
	}

	public override void ReceiveData(CommandSender sender, string data)
	{
		if (!(sender is PlayerCommandSender playerCommandSender))
		{
			base.ReceiveData(sender, data);
			return;
		}
		bool flag = false;
		this._senderNetId = playerCommandSender.ReferenceHub.netId;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.IsDummy)
			{
				HashSet<uint> orAddNew = RaDummyActions.NonDirtyReceivers.GetOrAddNew(allHub.netId);
				if (DummyActionCollector.IsDirty(allHub))
				{
					orAddNew.Clear();
					flag = true;
				}
				else if (!orAddNew.Contains(this._senderNetId))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			base.ReceiveData(sender, data);
		}
	}

	protected override void GatherData()
	{
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.IsDummy && !RaDummyActions.NonDirtyReceivers.GetOrAddNew(allHub.netId).Contains(this._senderNetId))
			{
				this.AppendDummy(allHub);
			}
		}
	}

	private void AppendDummy(ReferenceHub dummy)
	{
		base.AppendData("***" + dummy.netId);
		foreach (DummyAction item in DummyActionCollector.ServerGetActions(dummy))
		{
			if (item.Action == null)
			{
				base.AppendData("___" + item.Name);
			}
			else
			{
				base.AppendData(item.Name);
			}
		}
	}

	private void ClientReceive(string[] words)
	{
		DummyData dummyData = null;
		foreach (string text in words)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (text.StartsWith("***"))
			{
				string text2 = text;
				int length = "***".Length;
				string s = text2.Substring(length, text2.Length - length);
				dummyData = RaDummyActions.ReceivedData.GetOrAdd(uint.Parse(s), () => new DummyData());
				dummyData.Clear();
			}
			else
			{
				dummyData.Receive(text);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += OnClientReady;
	}

	private static void OnClientReady()
	{
		RaDummyActions.NonDirtyReceivers.Clear();
		RaDummyActions.ReceivedData.Clear();
	}
}
