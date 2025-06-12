using System;
using System.Runtime.InteropServices;
using CursorManagement;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.Searching;

[RequireComponent(typeof(ReferenceHub))]
public class SearchCoordinator : NetworkBehaviour, ICursorOverride
{
	public const string DebugKey = "SEARCH";

	[Header("Network Shared")]
	[SerializeField]
	[SyncVar(hook = "SetRayDistance")]
	private float rayDistance = 3f;

	[Header("Server only")]
	[SerializeField]
	private float serverRayDistanceThreshold = 1.2f;

	[SerializeField]
	private double serverDelayThreshold = 1.399999976158142;

	private static readonly CachedUserSetting<bool> ToggleSearch = new CachedUserSetting<bool>(MiscControlsSetting.SearchToggle);

	public ReferenceHub Hub { get; private set; }

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement => false;

	public float ServerMaxRayDistanceSqr { get; private set; }

	public float RayDistance
	{
		get
		{
			return this.rayDistance;
		}
		set
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("The ray distance can only be set by the server.");
			}
			this.NetworkrayDistance = value;
			this.UpdateMaxDistanceSqr();
		}
	}

	public SearchSessionPipe SessionPipe { get; private set; }

	public ISearchCompletor Completor { get; private set; }

	public float NetworkrayDistance
	{
		get
		{
			return this.rayDistance;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.rayDistance, 1uL, SetRayDistance);
		}
	}

	public event Action<ISearchCompletor> OnCompleted;

	private void SetRayDistance(float oldValue, float newValue)
	{
		this.UpdateMaxDistanceSqr();
	}

	private void UpdateMaxDistanceSqr()
	{
		this.ServerMaxRayDistanceSqr = this.rayDistance * this.rayDistance * this.serverRayDistanceThreshold;
	}

	private void Start()
	{
		this.UpdateMaxDistanceSqr();
		this.Hub = ReferenceHub.GetHub(base.gameObject);
		this.SessionPipe = new SearchSessionPipe(this, NetworkServer.active ? this.Hub.playerRateLimitHandler.RateLimits[0] : null);
		this.SessionPipe.RequestUpdated += HandleRequest;
		this.SessionPipe.RegisterHandlers();
		if (base.isLocalPlayer)
		{
			CursorManager.Register(this);
		}
	}

	private void OnDestroy()
	{
		CursorManager.Unregister(this);
	}

	private void Update()
	{
		if (NetworkServer.active && this.SessionPipe.Status == SearchSessionPipe.Activity.Promised)
		{
			this.ContinuePickupServer();
		}
		this.SessionPipe.Update();
	}

	private void HandleRequest()
	{
		bool flag;
		SearchSession? session;
		ISearchCompletor completor;
		try
		{
			flag = this.ReceiveRequestUnsafe(out session, out completor);
		}
		catch (Exception exception)
		{
			this.SessionPipe.Invalidate();
			DebugLog.LogException(exception);
			return;
		}
		if (flag)
		{
			if (session.HasValue)
			{
				this.SessionPipe.Session = session.Value;
			}
			else
			{
				this.SessionPipe.Invalidate();
			}
		}
		this.Completor = completor;
	}

	private bool ReceiveRequestUnsafe(out SearchSession? session, out ISearchCompletor completor)
	{
		SearchRequest request = this.SessionPipe.Request;
		completor = request.Target.GetSearchCompletor(this, this.ServerMaxRayDistanceSqr);
		if (completor == null || !completor.ValidateStart())
		{
			session = null;
			completor = null;
			return true;
		}
		SearchSession body = request.Body;
		if (!base.isLocalPlayer)
		{
			double num = NetworkTime.time - request.InitialTime;
			double num2 = (double)LiteNetLib4MirrorServer.Peers[base.connectionToClient.connectionId].Ping * 0.001 * this.serverDelayThreshold;
			float num3 = request.Target.SearchTimeForPlayer(this.Hub);
			if (num < 0.0 || num2 < num)
			{
				body.InitialTime = NetworkTime.time - num2;
				body.FinishTime = body.InitialTime + (double)num3;
			}
			else if (Math.Abs(body.FinishTime - body.InitialTime - (double)num3) > 0.001)
			{
				body.FinishTime = body.InitialTime + (double)num3;
			}
		}
		session = body;
		return true;
	}

	private void ContinuePickupServer()
	{
		if (this.Completor.ValidateUpdate())
		{
			if (!(NetworkTime.time < this.SessionPipe.Session.FinishTime))
			{
				this.Completor.Complete();
				this.OnCompleted?.Invoke(this.Completor);
			}
		}
		else
		{
			this.SessionPipe.Invalidate();
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this.rayDistance);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(this.rayDistance);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.rayDistance, SetRayDistance, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.rayDistance, SetRayDistance, reader.ReadFloat());
		}
	}
}
