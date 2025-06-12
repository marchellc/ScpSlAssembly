using UnityEngine;

namespace Cmdbinding;

public readonly struct Bind
{
	public readonly KeyCode Key;

	public readonly string Command;

	public Bind(KeyCode key, string command)
	{
		this.Key = key;
		this.Command = command;
	}
}
