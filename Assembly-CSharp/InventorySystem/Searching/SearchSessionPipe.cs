using System;
using Mirror;
using Security;
using UnityEngine;

namespace InventorySystem.Searching;

public class SearchSessionPipe
{
	public enum Activity
	{
		Idle,
		Requested,
		Promised
	}

	private readonly SearchCoordinator _owner;

	private readonly RateLimit _rateLimiter;

	private SearchRequest _request;

	private SearchSession _session;

	private SearchInvalidation Invalidation => new SearchInvalidation(this._request.Id);

	public SearchRequest Request => this._request;

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
			if (!value.Equals(this._session))
			{
				this._owner.connectionToClient.Send(value);
				this.Status = Activity.Promised;
				this._session = value;
			}
		}
	}

	public Activity Status { get; private set; }

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
		if (!(searchCoordinator == null))
		{
			SearchSessionPipe sessionPipe = searchCoordinator.SessionPipe;
			if (request.Target == null)
			{
				sessionPipe.Invalidate();
				source?.identity.GetComponent<GameConsoleTransmission>().SendToClient("Search request rejected - target is null.", "red");
			}
			else if (request.Target.ServerValidateRequest(source, sessionPipe))
			{
				sessionPipe.HandleRequest(request);
			}
		}
	}

	private void HandleRequest(SearchRequest request)
	{
		if (this._rateLimiter.CanExecute())
		{
			this._request = request;
			this.Status = Activity.Requested;
			this.RequestUpdated?.Invoke();
		}
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
		if (!(searchCoordinator == null))
		{
			searchCoordinator.SessionPipe.HandleAbort(invalidation);
		}
	}

	private void HandleAbort(SearchInvalidation invalidation)
	{
		if (this._request.Id != invalidation.Id)
		{
			return;
		}
		if (this._request.Target is UnityEngine.Object obj && obj != null)
		{
			this._request.Target.ServerHandleAbort(this._owner.Hub);
		}
		try
		{
			this.Status = Activity.Idle;
			this.SessionAborted?.Invoke();
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
		NetworkServer.ReplaceHandler<SearchRequest>(ReceiveRequest);
		NetworkServer.ReplaceHandler<SearchInvalidation>(ReceiveAbortion);
		NetworkClient.ReplaceHandler<SearchInvalidation>(ReceiveInvalidation);
		NetworkClient.ReplaceHandler<SearchSession>(ReceivePromise);
	}

	public void Invalidate()
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("An invalidation can only be performed by the server.");
		}
		if (this._request.Target is UnityEngine.Object obj && obj != null)
		{
			this._request.Target.ServerHandleAbort(this._owner.Hub);
		}
		this._owner.connectionToClient.Send(this.Invalidation);
		this.Status = Activity.Idle;
	}

	public void Update()
	{
		Activity activity = this.Status;
		Activity activity2;
		do
		{
			activity2 = activity;
			switch (this.Status)
			{
			case Activity.Promised:
				if (!(NetworkTime.time >= this.Session.FinishTime))
				{
					break;
				}
				activity = Activity.Requested;
				continue;
			case Activity.Requested:
				if (!(NetworkTime.time >= this.Request.FinishTime))
				{
					break;
				}
				activity = Activity.Idle;
				continue;
			}
			activity = this.Status;
		}
		while (activity != activity2);
		this.Status = activity;
	}
}
