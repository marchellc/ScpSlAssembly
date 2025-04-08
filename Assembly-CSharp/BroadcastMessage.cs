using System;

[Serializable]
public class BroadcastMessage
{
	public BroadcastMessage(string content, uint t, bool mono)
	{
		this.Text = content;
		this.Time = t;
		this.Truncated = mono;
	}

	public string Text;

	public uint Time;

	public bool Truncated;
}
