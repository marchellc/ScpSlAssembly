using System;

[Serializable]
public class BroadcastMessage
{
	public string Text;

	public uint Time;

	public bool Truncated;

	public BroadcastMessage(string content, uint t, bool mono)
	{
		Text = content;
		Time = t;
		Truncated = mono;
	}
}
