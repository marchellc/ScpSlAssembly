using System;
using System.Runtime.InteropServices;
using CursorManagement;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.Searching
{
	[RequireComponent(typeof(ReferenceHub))]
	public class SearchCoordinator : NetworkBehaviour, ICursorOverride
	{
		public event Action<SearchCompletor> OnCompleted;

		public ReferenceHub Hub { get; private set; }

		public CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		public bool LockMovement
		{
			get
			{
				return false;
			}
		}

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

		public SearchCompletor Completor { get; private set; }

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
			this.SessionPipe.RequestUpdated += this.HandleRequest;
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
			SearchSession? searchSession;
			SearchCompletor searchCompletor;
			bool flag;
			try
			{
				flag = this.ReceiveRequestUnsafe(out searchSession, out searchCompletor);
			}
			catch (Exception ex)
			{
				this.SessionPipe.Invalidate();
				DebugLog.LogException(ex);
				return;
			}
			if (flag)
			{
				if (searchSession != null)
				{
					this.SessionPipe.Session = searchSession.Value;
				}
				else
				{
					this.SessionPipe.Invalidate();
				}
			}
			this.Completor = searchCompletor;
		}

		private bool ReceiveRequestUnsafe(out SearchSession? session, out SearchCompletor completor)
		{
			SearchRequest request = this.SessionPipe.Request;
			completor = SearchCompletor.FromPickup(this, request.Target, (double)this.ServerMaxRayDistanceSqr);
			if (!completor.ValidateStart())
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
			session = new SearchSession?(body);
			return true;
		}

		private void ContinuePickupServer()
		{
			if (!this.Completor.ValidateUpdate())
			{
				this.SessionPipe.Invalidate();
				return;
			}
			if (NetworkTime.time < this.SessionPipe.Session.FinishTime)
			{
				return;
			}
			PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(this.Completor.Hub, this.Completor.TargetPickup));
			this.Completor.Complete();
			Action<SearchCompletor> onCompleted = this.OnCompleted;
			if (onCompleted == null)
			{
				return;
			}
			onCompleted(this.Completor);
		}

		public override bool Weaved()
		{
			return true;
		}

		public float NetworkrayDistance
		{
			get
			{
				return this.rayDistance;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this.rayDistance, 1UL, new Action<float, float>(this.SetRayDistance));
			}
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
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteFloat(this.rayDistance);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.rayDistance, new Action<float, float>(this.SetRayDistance), reader.ReadFloat());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.rayDistance, new Action<float, float>(this.SetRayDistance), reader.ReadFloat());
			}
		}

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
	}
}
