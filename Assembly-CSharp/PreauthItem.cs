using System;

public struct PreauthItem
{
	public string UserId { readonly get; private set; }

	public long Added { readonly get; private set; }

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
