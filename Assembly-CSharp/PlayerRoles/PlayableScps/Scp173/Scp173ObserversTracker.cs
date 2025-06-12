using System.Collections.Generic;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173ObserversTracker : StandardSubroutine<Scp173Role>
{
	public delegate void ObserversChanged(int prev, int current);

	public readonly HashSet<ReferenceHub> Observers = new HashSet<ReferenceHub>();

	private const float WidthMultiplier = 0.2f;

	[SerializeField]
	private float _modelWidth;

	[SerializeField]
	private float _maxViewDistance;

	[SerializeField]
	private Vector2[] _visibilityReferencePoints;

	private int _curObservers;

	private int _simulatedTargets;

	private float _simulatedStareTime;

	private readonly Stopwatch _simulatedStareSw = Stopwatch.StartNew();

	public int CurrentObservers
	{
		get
		{
			return this._curObservers;
		}
		private set
		{
			if (value != this._curObservers)
			{
				int curObservers = this._curObservers;
				this._curObservers = value;
				this.OnObserversChanged?.Invoke(curObservers, value);
			}
		}
	}

	public bool IsObserved => this.CurrentObservers > 0;

	public float SimulatedStare
	{
		get
		{
			return Mathf.Max(0f, this._simulatedStareTime - (float)this._simulatedStareSw.Elapsed.TotalSeconds);
		}
		set
		{
			this._simulatedStareTime = value;
			this._simulatedStareSw.Restart();
		}
	}

	public event ObserversChanged OnObserversChanged;

	private void Update()
	{
		this.UpdateObservers();
	}

	private void CheckRemovedPlayer(ReferenceHub ply)
	{
		if (NetworkServer.active && this.Observers.Remove(ply))
		{
			this.CurrentObservers--;
		}
	}

	private int UpdateObserver(ReferenceHub targetHub)
	{
		if (!HitboxIdentity.IsEnemy(base.Owner, targetHub))
		{
			if (!this.Observers.Remove(targetHub))
			{
				return 0;
			}
			return -1;
		}
		bool num = this.IsObservedBy(targetHub, 0.2f);
		bool flag = this.Observers.Contains(targetHub);
		if (num)
		{
			if (flag)
			{
				return 0;
			}
			Scp173AddingObserverEventArgs e = new Scp173AddingObserverEventArgs(targetHub, base.Owner);
			Scp173Events.OnAddingObserver(e);
			if (!e.IsAllowed)
			{
				return 0;
			}
			this.Observers.Add(targetHub);
			Scp173Events.OnAddedObserver(new Scp173AddedObserverEventArgs(targetHub, base.Owner));
			return 1;
		}
		if (!flag)
		{
			return 0;
		}
		Scp173RemovingObserverEventArgs e2 = new Scp173RemovingObserverEventArgs(targetHub, base.Owner);
		Scp173Events.OnRemovingObserver(e2);
		if (!e2.IsAllowed)
		{
			return 0;
		}
		this.Observers.Remove(targetHub);
		Scp173Events.OnRemovedObserver(new Scp173RemovedObserverEventArgs(targetHub, base.Owner));
		return -1;
	}

	protected override void Awake()
	{
		base.Awake();
		ReferenceHub.OnPlayerRemoved += CheckRemovedPlayer;
	}

	public bool IsObservedBy(ReferenceHub target, float widthMultiplier = 1f)
	{
		Vector3 position = base.CastRole.FpcModule.Position;
		float num = this._maxViewDistance;
		if (base.Owner.GetCurrentZone() == FacilityZone.Surface)
		{
			num *= 2f;
		}
		if (!VisionInformation.GetVisionInformation(target, target.PlayerCameraReference, position, this._modelWidth, num, checkFog: false, checkLineOfSight: false).IsLooking)
		{
			return false;
		}
		Vector3 position2 = target.PlayerCameraReference.position;
		Vector3 vector = target.PlayerCameraReference.TransformDirection(Vector3.right);
		Vector2[] visibilityReferencePoints = this._visibilityReferencePoints;
		for (int i = 0; i < visibilityReferencePoints.Length; i++)
		{
			Vector2 vector2 = visibilityReferencePoints[i];
			if (!Physics.Linecast(position + vector2.x * widthMultiplier * vector + Vector3.up * vector2.y, position2, VisionInformation.VisionLayerMask))
			{
				return true;
			}
		}
		return false;
	}

	public void UpdateObservers()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		int num = this.CurrentObservers;
		int num2 = ((this.SimulatedStare > 0f) ? 1 : 0);
		if (this._simulatedTargets != num2)
		{
			num += num2 - this._simulatedTargets;
			this._simulatedTargets = num2;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			num += this.UpdateObserver(allHub);
		}
		this.CurrentObservers = num;
		if (!base.Owner.isLocalPlayer)
		{
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)Mathf.Clamp(this.CurrentObservers, 0, 255));
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.CurrentObservers = reader.ReadByte();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._curObservers = 0;
		this._simulatedTargets = 0;
		this._simulatedStareTime = 0f;
		this.Observers.Clear();
	}
}
