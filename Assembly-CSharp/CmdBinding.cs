using System;
using System.Collections.Generic;
using System.IO;
using GameCore;
using UnityEngine;

public class CmdBinding : MonoBehaviour
{
	static CmdBinding()
	{
		CmdBinding.Load();
	}

	private void Update()
	{
	}

	public static void KeyBind(KeyCode code, string cmd)
	{
		foreach (CmdBinding.Bind bind in CmdBinding.Bindings)
		{
			if (bind.key == code)
			{
				bind.command = cmd;
				CmdBinding.Save();
				return;
			}
		}
		CmdBinding.Bindings.Add(new CmdBinding.Bind
		{
			command = cmd,
			key = code
		});
	}

	public static void Save()
	{
		string text = "";
		for (int i = 0; i < CmdBinding.Bindings.Count; i++)
		{
			string text2 = text;
			int key = (int)CmdBinding.Bindings[i].key;
			text = text2 + key.ToString() + ":" + CmdBinding.Bindings[i].command;
			if (i != CmdBinding.Bindings.Count - 1)
			{
				text += Environment.NewLine;
			}
		}
		StreamWriter streamWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/cmdbinding.txt");
		streamWriter.WriteLine(text);
		streamWriter.Close();
	}

	public static void Load()
	{
		global::GameCore.Console.AddLog("Loading cmd bindings...", Color.grey, false, global::GameCore.Console.ConsoleLogType.Log);
		try
		{
			CmdBinding.Bindings.Clear();
			if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/cmdbinding.txt"))
			{
				CmdBinding.Revent();
			}
			StreamReader streamReader = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/cmdbinding.txt");
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				if (!string.IsNullOrEmpty(text) && text.Contains(":"))
				{
					CmdBinding.Bindings.Add(new CmdBinding.Bind
					{
						command = text.Split(':', StringSplitOptions.None)[1],
						key = (KeyCode)int.Parse(text.Split(':', StringSplitOptions.None)[0])
					});
				}
			}
			streamReader.Close();
		}
		catch (Exception ex)
		{
			Debug.Log("REVENT: " + ex.StackTrace + " - " + ex.Message);
			CmdBinding.Revent();
		}
	}

	private static void Revent()
	{
		Debug.Log("Reventing!");
		new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/cmdbinding.txt").Close();
	}

	public static readonly List<CmdBinding.Bind> Bindings = new List<CmdBinding.Bind>();

	public class Bind
	{
		public string command;

		public KeyCode key;
	}
}
