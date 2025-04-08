using System;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class ClientPredictedValue<T>
	{
		public T Value
		{
			get
			{
				if (this._predictionTimeout.Busy)
				{
					return this._predicted;
				}
				this._predicted = this._fetcher();
				return this._predicted;
			}
			set
			{
				this._predicted = value;
				this._predictionTimeout.Trigger();
			}
		}

		public void ForceResync()
		{
			this._predictionTimeout.Reset();
		}

		public ClientPredictedValue(Func<T> serverSyncvarFetcher)
		{
			this._fetcher = serverSyncvarFetcher;
			this._predictionTimeout = new ClientRequestTimer();
		}

		private readonly Func<T> _fetcher;

		private readonly ClientRequestTimer _predictionTimeout;

		private T _predicted;
	}
}
