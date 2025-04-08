using System;
using InventorySystem.Items.Pickups;
using Mirror;
using Security;

namespace InventorySystem.Searching
{
	public class SearchSessionPipe
	{
		private SearchInvalidation Invalidation
		{
			get
			{
				return new SearchInvalidation(this._request.Id);
			}
		}

		public SearchRequest Request
		{
			get
			{
				return this._request;
			}
		}

		public SearchSession Session
		{
			get
			{
				return this._session;
			}
			set
			{
				if (!NetworkServer.active)
				{
					throw new InvalidOperationException("The promise can only be set from the server.");
				}
				if (value.Equals(this._session))
				{
					return;
				}
				this._owner.connectionToClient.Send<SearchSession>(value, 0);
				this.Status = SearchSessionPipe.Activity.Promised;
				this._session = value;
			}
		}

		public SearchSessionPipe.Activity Status { get; private set; }

		public event Action RequestUpdated;

		public event Action SessionAborted;

		public SearchSessionPipe(SearchCoordinator owner, RateLimit rateLimit)
		{
			this._owner = owner;
			this._rateLimiter = rateLimit;
		}

		private static void ReceiveRequest(NetworkConnection source, SearchRequest request)
		{
			SearchCoordinator searchCoordinator = ((source == null) ? ReferenceHub.LocalHub.searchCoordinator : source.identity.GetComponent<SearchCoordinator>());
			if (searchCoordinator == null)
			{
				return;
			}
			SearchSessionPipe sessionPipe = searchCoordinator.SessionPipe;
			if (request.Target == null)
			{
				sessionPipe.Invalidate();
				if (source != null)
				{
					source.identity.GetComponent<GameConsoleTransmission>().SendToClient("Pickup request rejected - target is null.", "red");
				}
				return;
			}
			PickupSyncInfo info = request.Target.Info;
			if (info.Locked)
			{
				sessionPipe.Invalidate();
				if (source != null)
				{
					source.identity.GetComponent<GameConsoleTransmission>().SendToClient("Pickup request rejected - target is locked.", "red");
				}
				return;
			}
			if (info.InUse)
			{
				sessionPipe.Invalidate();
				if (source != null)
				{
					source.identity.GetComponent<GameConsoleTransmission>().SendToClient("Pickup request rejected - target is in use.", "red");
				}
				return;
			}
			info.InUse = true;
			request.Target.NetworkInfo = info;
			sessionPipe.HandleRequest(request);
		}

		private void HandleRequest(SearchRequest request)
		{
			if (!this._rateLimiter.CanExecute(true))
			{
				return;
			}
			this._request = request;
			this.Status = SearchSessionPipe.Activity.Requested;
			Action requestUpdated = this.RequestUpdated;
			if (requestUpdated == null)
			{
				return;
			}
			requestUpdated();
		}

		private void ReceivePromise(NetworkConnection source, SearchSession session)
		{
			this.HandlePromise(session);
		}

		private void HandlePromise(SearchSession session)
		{
		}

		private static void ReceiveAbortion(NetworkConnection source, SearchInvalidation invalidation)
		{
			SearchCoordinator searchCoordinator = ((source == null) ? ReferenceHub.LocalHub.searchCoordinator : source.identity.GetComponent<SearchCoordinator>());
			if (searchCoordinator == null)
			{
				return;
			}
			searchCoordinator.SessionPipe.HandleAbort(invalidation);
		}

		private void HandleAbort(SearchInvalidation invalidation)
		{
			if (this._request.Id != invalidation.Id)
			{
				return;
			}
			if (this._request.Target != null)
			{
				PickupSyncInfo info = this._request.Target.Info;
				info.InUse = false;
				this._request.Target.NetworkInfo = info;
			}
			try
			{
				this.Status = SearchSessionPipe.Activity.Idle;
				Action sessionAborted = this.SessionAborted;
				if (sessionAborted != null)
				{
					sessionAborted();
				}
			}
			finally
			{
				this._request = default(SearchRequest);
				this._session = default(SearchSession);
			}
		}

		private void ReceiveInvalidation(NetworkConnection source, SearchInvalidation invalidation)
		{
			this.HandleInvalidate(invalidation);
		}

		private void HandleInvalidate(SearchInvalidation invalidation)
		{
		}

		public void RegisterHandlers()
		{
			NetworkServer.ReplaceHandler<SearchRequest>(new Action<NetworkConnectionToClient, SearchRequest>(SearchSessionPipe.ReceiveRequest), true);
			NetworkServer.ReplaceHandler<SearchInvalidation>(new Action<NetworkConnectionToClient, SearchInvalidation>(SearchSessionPipe.ReceiveAbortion), true);
			NetworkClient.ReplaceHandler<SearchInvalidation>(new Action<NetworkConnection, SearchInvalidation>(this.ReceiveInvalidation), true);
			NetworkClient.ReplaceHandler<SearchSession>(new Action<NetworkConnection, SearchSession>(this.ReceivePromise), true);
		}

		public void Invalidate()
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("An invalidation can only be performed by the server.");
			}
			if (this._request.Target != null)
			{
				PickupSyncInfo info = this._request.Target.Info;
				info.InUse = false;
				this._request.Target.NetworkInfo = info;
			}
			this._owner.connectionToClient.Send<SearchInvalidation>(this.Invalidation, 0);
			this.Status = SearchSessionPipe.Activity.Idle;
		}

		public void Update()
		{
			SearchSessionPipe.Activity activity = this.Status;
			for (;;)
			{
				SearchSessionPipe.Activity activity2 = activity;
				SearchSessionPipe.Activity status = this.Status;
				if (status != SearchSessionPipe.Activity.Requested)
				{
					if (status != SearchSessionPipe.Activity.Promised || NetworkTime.time < this.Session.FinishTime)
					{
						goto IL_004B;
					}
					activity = SearchSessionPipe.Activity.Requested;
				}
				else
				{
					if (NetworkTime.time < this.Request.FinishTime)
					{
						goto IL_004B;
					}
					activity = SearchSessionPipe.Activity.Idle;
				}
				IL_0052:
				if (activity == activity2)
				{
					break;
				}
				continue;
				IL_004B:
				activity = this.Status;
				goto IL_0052;
			}
			this.Status = activity;
		}

		private readonly SearchCoordinator _owner;

		private readonly RateLimit _rateLimiter;

		private SearchRequest _request;

		private SearchSession _session;

		public enum Activity
		{
			Idle,
			Requested,
			Promised
		}
	}
}
