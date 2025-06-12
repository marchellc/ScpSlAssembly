using System;
using System.Collections.Generic;
using System.Linq;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace RelativePositioning;

public class NetIdWaypoint : WaypointBase
{
	[SerializeField]
	private NetworkIdentity _targetNetId;

	private Vector3 _pos;

	private const byte Offset = 32;

	private static readonly HashSet<NetIdWaypoint> AllNetWaypoints = new HashSet<NetIdWaypoint>();

	private static bool _refreshNextFrame;

	private static bool _callEvent;

	public static event Action OnNetIdWaypointsSet;

	protected override void Start()
	{
		base.Start();
		this.SetPosition();
		NetIdWaypoint.AllNetWaypoints.Add(this);
		NetIdWaypoint._refreshNextFrame = true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		NetIdWaypoint.AllNetWaypoints.Remove(this);
	}

	protected override float SqrDistanceTo(Vector3 pos)
	{
		return (pos - this._pos).sqrMagnitude;
	}

	public override Vector3 GetWorldspacePosition(Vector3 relPosition)
	{
		return relPosition + this._pos;
	}

	public override Vector3 GetRelativePosition(Vector3 worldPoint)
	{
		return worldPoint - this._pos;
	}

	private void Update()
	{
		if (!NetIdWaypoint._refreshNextFrame)
		{
			base.enabled = false;
			return;
		}
		byte b = 32;
		foreach (NetIdWaypoint item in NetIdWaypoint.AllNetWaypoints.OrderBy((NetIdWaypoint x) => x._targetNetId.netId))
		{
			item.SetPosition();
			item.SetId(b);
			b++;
		}
		if (NetIdWaypoint._callEvent)
		{
			NetIdWaypoint.OnNetIdWaypointsSet?.Invoke();
			NetIdWaypoint._callEvent = false;
		}
		NetIdWaypoint._refreshNextFrame = false;
	}

	private void Reset()
	{
		this._targetNetId = base.GetComponent<NetworkIdentity>();
	}

	private void SetPosition()
	{
		this._pos = this._targetNetId.transform.position;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationStage += delegate(MapGenerationPhase stage)
		{
			if (stage == MapGenerationPhase.RelativePositioningWaypoints)
			{
				NetIdWaypoint._refreshNextFrame = true;
				NetIdWaypoint._callEvent = true;
			}
		};
	}
}
