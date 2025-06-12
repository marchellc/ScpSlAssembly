using System;
using System.Collections.Generic;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class FullAutoShotsQueue<T> : FullAutoRateLimiter
{
	private const float NetworkShootRequestTimeout = 0.22f;

	public Action<T> OnRequestTimedOut;

	private readonly Queue<T> _queuedShots;

	private readonly Func<T, ShotBacktrackData> _dataSelector;

	public bool Idle
	{
		get
		{
			if (this._queuedShots.IsEmpty())
			{
				return base.Ready;
			}
			return false;
		}
	}

	public void Enqueue(T item)
	{
		this._queuedShots.Enqueue(item);
	}

	public bool TryDequeue(out T dequeued)
	{
		while (base.Ready && this._queuedShots.TryDequeue(out dequeued))
		{
			if (this._dataSelector == null)
			{
				return true;
			}
			if (this._dataSelector(dequeued).Age < 0.2199999988079071)
			{
				return true;
			}
			this.OnRequestTimedOut?.Invoke(dequeued);
		}
		dequeued = default(T);
		return false;
	}

	public FullAutoShotsQueue(Func<T, ShotBacktrackData> backtrackDataSelector)
	{
		this._queuedShots = new Queue<T>();
		this._dataSelector = backtrackDataSelector;
	}
}
