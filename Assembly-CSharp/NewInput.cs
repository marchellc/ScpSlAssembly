using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

public static class NewInput
{
	public static event Action OnAnyModified;

	public static event Action<ActionName, KeyCode> OnKeyModified;

	public static KeyCode GetKey(ActionName actionName, KeyCode fallbackKeycode = KeyCode.None)
	{
		if (!NewInput._loaded)
		{
			NewInput.Load();
		}
		KeyCode keyCode;
		if (!NewInput.UserKeybinds.TryGetValue(actionName, out keyCode))
		{
			return fallbackKeycode;
		}
		return keyCode;
	}

	public static void Save()
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		foreach (KeyValuePair<ActionName, KeyCode> keyValuePair in NewInput.UserKeybinds)
		{
			stringBuilder.Append((int)keyValuePair.Key);
			stringBuilder.Append(':');
			stringBuilder.Append((int)keyValuePair.Value);
			stringBuilder.Append(';');
		}
		File.WriteAllText(NewInput.SaveFilePath, stringBuilder.ToString(0, stringBuilder.Length - 1));
		StringBuilderPool.Shared.Return(stringBuilder);
	}

	public static void Load()
	{
		NewInput.ResetToDefault();
		if (!File.Exists(NewInput.SaveFilePath))
		{
			NewInput.Save();
		}
		string[] array = File.ReadAllText(NewInput.SaveFilePath).Split(';', StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(':', StringSplitOptions.None);
			ActionName actionName;
			KeyCode keyCode;
			if (array2.Length == 2 && NewInput.TryParseActionName(array2[0], out actionName) && NewInput.TryParseKeycode(array2[1], out keyCode))
			{
				NewInput.UserKeybinds[actionName] = keyCode;
			}
		}
		NewInput._loaded = true;
	}

	public static void ChangeKeybind(ActionName action, KeyCode key)
	{
		if (key == KeyCode.None && !NewInput.DefaultKeybinds.TryGetValue(action, out key))
		{
			return;
		}
		NewInput.UserKeybinds[action] = key;
		Action onAnyModified = NewInput.OnAnyModified;
		if (onAnyModified != null)
		{
			onAnyModified();
		}
		Action<ActionName, KeyCode> onKeyModified = NewInput.OnKeyModified;
		if (onKeyModified != null)
		{
			onKeyModified(action, key);
		}
		NewInput.Save();
	}

	public static bool TryParseActionName(string s, out ActionName actionName)
	{
		int num;
		if (int.TryParse(s, out num) && Enum.IsDefined(typeof(ActionName), (ActionName)num))
		{
			actionName = (ActionName)num;
			return true;
		}
		Debug.Log("Action name " + s + " is not defined");
		actionName = ActionName.Shoot;
		return false;
	}

	public static bool TryParseKeycode(string s, out KeyCode keyCode)
	{
		int num;
		if (int.TryParse(s, out num) && Enum.IsDefined(typeof(KeyCode), (KeyCode)num))
		{
			keyCode = (KeyCode)num;
			return true;
		}
		keyCode = KeyCode.None;
		return false;
	}

	public static void ResetToDefault()
	{
		NewInput.UserKeybinds.Clear();
		foreach (NewInput.ActionDefinition actionDefinition in NewInput.DefinedActions)
		{
			KeyCode defaultKey = actionDefinition.DefaultKey;
			NewInput.UserKeybinds[actionDefinition.Name] = defaultKey;
			NewInput.DefaultKeybinds[actionDefinition.Name] = defaultKey;
		}
	}

	public static bool TryGetCategory(this ActionName sourceAction, out ActionCategory cat)
	{
		foreach (NewInput.ActionDefinition actionDefinition in NewInput.DefinedActions)
		{
			if (actionDefinition.Name == sourceAction)
			{
				cat = actionDefinition.Category;
				return true;
			}
		}
		cat = ActionCategory.Gameplay;
		return false;
	}

	public static readonly Dictionary<ActionName, KeyCode> DefaultKeybinds = new Dictionary<ActionName, KeyCode>();

	private static readonly Dictionary<ActionName, KeyCode> UserKeybinds = new Dictionary<ActionName, KeyCode>();

	private static readonly string SaveFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/keybinding.txt";

	public static readonly NewInput.ActionDefinition[] DefinedActions = new NewInput.ActionDefinition[]
	{
		new NewInput.ActionDefinition(ActionName.Shoot, KeyCode.Mouse0, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.Zoom, KeyCode.Mouse1, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.Jump, KeyCode.Space, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.Interact, KeyCode.E, ActionCategory.Gameplay),
		new NewInput.ActionDefinition(ActionName.Inventory, KeyCode.Tab, ActionCategory.Gameplay),
		new NewInput.ActionDefinition(ActionName.Reload, KeyCode.R, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.Run, KeyCode.LeftShift, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.VoiceChat, KeyCode.Q, ActionCategory.Communication),
		new NewInput.ActionDefinition(ActionName.Sneak, KeyCode.C, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.MoveForward, KeyCode.W, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.MoveBackward, KeyCode.S, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.MoveLeft, KeyCode.A, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.MoveRight, KeyCode.D, ActionCategory.Movement),
		new NewInput.ActionDefinition(ActionName.PlayerList, KeyCode.N, ActionCategory.Gameplay),
		new NewInput.ActionDefinition(ActionName.CharacterInfo, KeyCode.F1, ActionCategory.Gameplay),
		new NewInput.ActionDefinition(ActionName.RemoteAdmin, KeyCode.M, ActionCategory.System),
		new NewInput.ActionDefinition(ActionName.ToggleFlashlight, KeyCode.F, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.AltVoiceChat, KeyCode.V, ActionCategory.Communication),
		new NewInput.ActionDefinition(ActionName.Noclip, KeyCode.LeftAlt, ActionCategory.System),
		new NewInput.ActionDefinition(ActionName.NoClipFogToggle, KeyCode.O, ActionCategory.System),
		new NewInput.ActionDefinition(ActionName.GameConsole, KeyCode.BackQuote, ActionCategory.System),
		new NewInput.ActionDefinition(ActionName.InspectItem, KeyCode.I, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.WeaponAlt, KeyCode.Mouse2, ActionCategory.Weapons),
		new NewInput.ActionDefinition(ActionName.ThrowItem, KeyCode.T, ActionCategory.Gameplay),
		new NewInput.ActionDefinition(ActionName.HideGUI, KeyCode.P, ActionCategory.System),
		new NewInput.ActionDefinition(ActionName.PauseMenu, KeyCode.Escape, ActionCategory.Unbindable),
		new NewInput.ActionDefinition(ActionName.DebugLogMenu, KeyCode.F4, ActionCategory.Unbindable),
		new NewInput.ActionDefinition(ActionName.Scp079FreeLook, KeyCode.Space, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079LockDoor, KeyCode.Mouse1, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079UnlockAll, KeyCode.R, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079Blackout, KeyCode.F, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079Lockdown, KeyCode.G, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079PingLocation, KeyCode.E, ActionCategory.Scp079),
		new NewInput.ActionDefinition(ActionName.Scp079BreachScanner, KeyCode.Space, ActionCategory.Scp079)
	};

	private static bool _loaded;

	public class ActionDefinition
	{
		public ActionDefinition(ActionName actionName, KeyCode k, ActionCategory c)
		{
			this.Name = actionName;
			this.Category = c;
			this.DefaultKey = k;
		}

		public readonly ActionName Name;

		public readonly ActionCategory Category;

		public readonly KeyCode DefaultKey;
	}
}
