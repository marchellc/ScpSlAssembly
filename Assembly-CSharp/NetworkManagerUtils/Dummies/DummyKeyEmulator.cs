using System;
using System.Collections.Generic;

namespace NetworkManagerUtils.Dummies;

public class DummyKeyEmulator : IDummyActionProvider
{
	private readonly struct EmulatedEntry
	{
		public readonly ActionName Action;

		public readonly bool OnlyOneClick;

		public EmulatedEntry(ActionName action, bool click)
		{
			this.Action = action;
			this.OnlyOneClick = click;
		}
	}

	private readonly IRootDummyActionProvider _root;

	private readonly List<ActionName> _registeredListeners = new List<ActionName>();

	private readonly List<ActionName> _firstFrameActions = new List<ActionName>();

	private readonly List<EmulatedEntry> _activeEntries = new List<EmulatedEntry>();

	private readonly List<EmulatedEntry> _scheduledEntries = new List<EmulatedEntry>();

	public bool AnyListeners => this._registeredListeners.Count > 0;

	public DummyKeyEmulator(IRootDummyActionProvider inventory)
	{
		this._root = inventory;
	}

	public bool GetAction(ActionName action, bool firstFrameOnly)
	{
		if (!this._registeredListeners.Contains(action))
		{
			this._root.DummyActionsDirty = true;
			this._registeredListeners.Add(action);
		}
		if (this.IsHeld(action))
		{
			if (firstFrameOnly)
			{
				return this._firstFrameActions.Contains(action);
			}
			return true;
		}
		return false;
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder)
	{
		foreach (ActionName action in this._registeredListeners)
		{
			actionAdder(new DummyAction($"{action}->Click", delegate
			{
				this.AddEntry(action, isClick: true);
			}));
			if (this.IsHeld(action))
			{
				actionAdder(new DummyAction($"{action}->Release", delegate
				{
					this.RemoveEntry(action);
				}));
			}
			else
			{
				actionAdder(new DummyAction($"{action}->Hold", delegate
				{
					this.AddEntry(action, isClick: false);
				}));
			}
		}
	}

	public void LateUpdate()
	{
		this._firstFrameActions.Clear();
		for (int num = this._activeEntries.Count - 1; num >= 0; num--)
		{
			if (this._activeEntries[num].OnlyOneClick)
			{
				this._activeEntries.RemoveAt(num);
				this._root.DummyActionsDirty = true;
			}
		}
		foreach (EmulatedEntry scheduledEntry in this._scheduledEntries)
		{
			this._firstFrameActions.Add(scheduledEntry.Action);
			this._activeEntries.Add(scheduledEntry);
			this._root.DummyActionsDirty = true;
		}
		this._scheduledEntries.Clear();
	}

	private void RemoveEntry(List<EmulatedEntry> list, ActionName action)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Action == action)
			{
				list.RemoveAt(i);
				this._root.DummyActionsDirty = true;
				break;
			}
		}
	}

	private void RemoveEntry(ActionName action)
	{
		this.RemoveEntry(this._activeEntries, action);
		this.RemoveEntry(this._scheduledEntries, action);
	}

	private bool IsHeld(ActionName action)
	{
		foreach (EmulatedEntry activeEntry in this._activeEntries)
		{
			if (activeEntry.Action == action)
			{
				return true;
			}
		}
		return false;
	}

	private void AddEntry(ActionName action, bool isClick)
	{
		this.RemoveEntry(action);
		this._scheduledEntries.Add(new EmulatedEntry(action, isClick));
	}
}
