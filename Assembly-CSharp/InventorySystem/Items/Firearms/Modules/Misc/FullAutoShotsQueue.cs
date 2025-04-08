using System;
using System.Collections.Generic;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class FullAutoShotsQueue<T> : FullAutoRateLimiter
	{
		public bool Idle
		{
			get
			{
				return this._queuedShots.IsEmpty<T>() && base.Ready;
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
				if (this._dataSelector(dequeued).Age < 0.15000000596046448)
				{
					return true;
				}
				Action<T> onRequestRejected = this.OnRequestRejected;
				if (onRequestRejected != null)
				{
					onRequestRejected(dequeued);
				}
			}
			dequeued = default(T);
			return false;
		}

		public FullAutoShotsQueue(Func<T, ShotBacktrackData> backtrackDataSelector)
		{
			this._queuedShots = new Queue<T>();
			this._dataSelector = backtrackDataSelector;
		}

		private const float NetworkShootRequestTimeout = 0.15f;

		public Action<T> OnRequestRejected;

		private readonly Queue<T> _queuedShots;

		private readonly Func<T, ShotBacktrackData> _dataSelector;
	}
}
