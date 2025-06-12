using System;

public struct PreauthChallengeItem
{
	public long Added { get; private set; }

	public ArraySegment<byte> ValidResponse { get; private set; }

	public PreauthChallengeItem(ArraySegment<byte> response)
	{
		this.ValidResponse = response;
		this.Added = DateTime.Now.Ticks;
	}
}
