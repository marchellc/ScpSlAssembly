using System;

public struct PreauthItem
{
	public string UserId { get; private set; }

	public long Added { get; private set; }

	public PreauthItem(string userId)
	{
		this.UserId = userId;
		this.Added = DateTime.Now.Ticks;
	}

	public void SetUserId(string userId)
	{
		this.UserId = userId;
		this.Added = DateTime.Now.Ticks;
	}
}
