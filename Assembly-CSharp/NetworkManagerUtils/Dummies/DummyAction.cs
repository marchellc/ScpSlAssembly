using System;

namespace NetworkManagerUtils.Dummies;

public readonly struct DummyAction
{
	public readonly string Name;

	public readonly Action Action;

	public DummyAction(string name, Action action)
	{
		this.Name = name;
		this.Action = action;
	}
}
