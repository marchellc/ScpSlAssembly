using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;

namespace ProgressiveCulling;

public class RoomCullingConnection : CullableBehaviour, IBoundsCullable, ICullable
{
	public readonly struct CoordsPair : IEquatable<CoordsPair>
	{
		public readonly Vector3Int CoordsA;

		public readonly Vector3Int CoordsB;

		public CoordsPair(Vector3Int coordsA, Vector3Int coordsB)
		{
			this.CoordsA = coordsA;
			this.CoordsB = coordsB;
		}

		public bool Equals(CoordsPair other)
		{
			if (this.CoordsA == other.CoordsA)
			{
				return this.CoordsB == other.CoordsB;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is CoordsPair other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.CoordsA, this.CoordsB);
		}
	}

	public readonly struct RoomLink
	{
		public readonly CoordsPair Coords;

		public readonly CoordsPair InvCoords;

		public readonly RoomIdentifier RoomA;

		public readonly RoomIdentifier RoomB;

		public readonly RoomIdentifier[] Rooms;

		public readonly CullableRoom CullableA;

		public readonly CullableRoom CullableB;

		public readonly CullableRoom[] Cullables;

		public readonly bool Valid;

		public bool BothCulled
		{
			get
			{
				if (this.CullableA.IsCulled)
				{
					return this.CullableB.IsCulled;
				}
				return false;
			}
		}

		public RoomLink(RoomIdentifier a, RoomIdentifier b)
		{
			this.RoomA = a;
			this.RoomB = b;
			bool flag = a.TryGetComponent<CullableRoom>(out this.CullableA);
			bool flag2 = b.TryGetComponent<CullableRoom>(out this.CullableB);
			this.Valid = flag && flag2;
			Vector3Int mainCoords = a.MainCoords;
			Vector3Int mainCoords2 = b.MainCoords;
			this.Coords = new CoordsPair(mainCoords, mainCoords2);
			this.InvCoords = new CoordsPair(mainCoords2, mainCoords);
			if (this.Valid)
			{
				this.Rooms = new RoomIdentifier[2] { this.RoomA, this.RoomB };
				this.Cullables = new CullableRoom[2] { this.CullableA, this.CullableB };
			}
			else
			{
				this.Rooms = new RoomIdentifier[0];
				this.Cullables = new CullableRoom[0];
			}
		}
	}

	public static readonly HashSet<RoomCullingConnection> AllInstances = new HashSet<RoomCullingConnection>();

	private static readonly Dictionary<CoordsPair, RoomCullingConnection> SmallestInstancesByPair = new Dictionary<CoordsPair, RoomCullingConnection>();

	private readonly AutoCuller _autoCuller = new AutoCuller();

	private bool _isSmallest;

	[SerializeField]
	private Bounds _bounds;

	public RoomLink Link { get; private set; }

	public IRoomConnector Connector { get; private set; }

	public Bounds WorldspaceBounds { get; private set; }

	public override bool ShouldBeVisible
	{
		get
		{
			if (!this.Link.Valid)
			{
				return CullingCamera.CheckBoundsVisibility(this.WorldspaceBounds);
			}
			return !this.Link.BothCulled;
		}
	}

	private float BoundsArea => this._bounds.size.x * this._bounds.size.y;

	private void Awake()
	{
		RoomCullingConnection.AllInstances.Add(this);
		this.Connector = base.GetComponent<IRoomConnector>();
		this.Connector.OnRoomsRegistered += OnRoomsRegistered;
		this._autoCuller.Generate(base.gameObject, null, null, ignoreDoNotCullTag: true);
		if (this.Connector.RoomsAlreadyRegistered)
		{
			this.OnRoomsRegistered();
		}
	}

	private void OnDestroy()
	{
		RoomCullingConnection.AllInstances.Remove(this);
		if (this._isSmallest)
		{
			RoomCullingConnection.SmallestInstancesByPair.Remove(this.Link.Coords);
			RoomCullingConnection.SmallestInstancesByPair.Remove(this.Link.InvCoords);
		}
	}

	private Bounds GenerateWorldspaceBounds()
	{
		Transform obj = base.transform;
		Vector3 center = obj.TransformPoint(this._bounds.center);
		Vector3 v = obj.rotation * this._bounds.size;
		return new Bounds(center, v.Abs());
	}

	private void OnRoomsRegistered()
	{
		RoomIdentifier[] rooms = this.Connector.Rooms;
		if (rooms.Length != 2)
		{
			return;
		}
		this.Link = new RoomLink(rooms[0], rooms[1]);
		this.WorldspaceBounds = this.GenerateWorldspaceBounds();
		if (this.Link.Valid)
		{
			this.AddVisibilityListener(this.Link.CullableA);
			this.AddVisibilityListener(this.Link.CullableB);
			if (this.TryRegisterSmallest())
			{
				RoomCullingConnection.SmallestInstancesByPair[this.Link.Coords] = this;
				RoomCullingConnection.SmallestInstancesByPair[this.Link.InvCoords] = this;
				this._isSmallest = true;
			}
			((ICullable)this).UpdateState();
		}
	}

	private bool TryRegisterSmallest()
	{
		if (!RoomCullingConnection.SmallestInstancesByPair.TryGetValue(this.Link.Coords, out var value))
		{
			return true;
		}
		if (this.BoundsArea > value.BoundsArea)
		{
			return false;
		}
		value._isSmallest = false;
		return true;
	}

	private void AddVisibilityListener(CullableRoom cb)
	{
		cb.OnVisibilityUpdated += ((ICullable)this).UpdateState;
	}

	protected override void OnVisibilityChanged(bool isVisible)
	{
		this._autoCuller.SetVisibility(isVisible);
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Bounds bounds = this.GenerateWorldspaceBounds();
		Gizmos.DrawWireCube(bounds.center, bounds.size);
	}

	public static bool TryGetConnection(Vector3Int a, Vector3Int b, out RoomCullingConnection conn)
	{
		return RoomCullingConnection.SmallestInstancesByPair.TryGetValue(new CoordsPair(a, b), out conn);
	}
}
