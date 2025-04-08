using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;

namespace ProgressiveCulling
{
	public class RoomCullingConnection : CullableBehaviour, IBoundsCullable, ICullable
	{
		public RoomCullingConnection.RoomLink Link { get; private set; }

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

		private float BoundsArea
		{
			get
			{
				return this._bounds.size.x * this._bounds.size.y;
			}
		}

		private void Awake()
		{
			RoomCullingConnection.AllInstances.Add(this);
			this.Connector = base.GetComponent<IRoomConnector>();
			this.Connector.OnRoomsRegistered += this.OnRoomsRegistered;
			this._autoCuller.Generate(base.gameObject, null, null, true);
			if (this.Connector.RoomsAlreadyRegistered)
			{
				this.OnRoomsRegistered();
			}
		}

		private void OnDestroy()
		{
			RoomCullingConnection.AllInstances.Remove(this);
			if (!this._isSmallest)
			{
				return;
			}
			RoomCullingConnection.SmallestInstancesByPair.Remove(this.Link.Coords);
			RoomCullingConnection.SmallestInstancesByPair.Remove(this.Link.InvCoords);
		}

		private Bounds GenerateWorldspaceBounds()
		{
			Transform transform = base.transform;
			Vector3 vector = transform.TransformPoint(this._bounds.center);
			Vector3 vector2 = transform.rotation * this._bounds.size;
			return new Bounds(vector, vector2.Abs());
		}

		private void OnRoomsRegistered()
		{
			RoomIdentifier[] rooms = this.Connector.Rooms;
			if (rooms.Length != 2)
			{
				return;
			}
			this.Link = new RoomCullingConnection.RoomLink(rooms[0], rooms[1]);
			this.WorldspaceBounds = this.GenerateWorldspaceBounds();
			if (!this.Link.Valid)
			{
				return;
			}
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

		private bool TryRegisterSmallest()
		{
			RoomCullingConnection roomCullingConnection;
			if (!RoomCullingConnection.SmallestInstancesByPair.TryGetValue(this.Link.Coords, out roomCullingConnection))
			{
				return true;
			}
			if (this.BoundsArea > roomCullingConnection.BoundsArea)
			{
				return false;
			}
			roomCullingConnection._isSmallest = false;
			return true;
		}

		private void AddVisibilityListener(CullableRoom cb)
		{
			cb.OnVisibilityUpdated += this.UpdateState;
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
			return RoomCullingConnection.SmallestInstancesByPair.TryGetValue(new RoomCullingConnection.CoordsPair(a, b), out conn);
		}

		public static readonly HashSet<RoomCullingConnection> AllInstances = new HashSet<RoomCullingConnection>();

		private static readonly Dictionary<RoomCullingConnection.CoordsPair, RoomCullingConnection> SmallestInstancesByPair = new Dictionary<RoomCullingConnection.CoordsPair, RoomCullingConnection>();

		private readonly AutoCuller _autoCuller = new AutoCuller();

		private bool _isSmallest;

		[SerializeField]
		private Bounds _bounds;

		public readonly struct CoordsPair : IEquatable<RoomCullingConnection.CoordsPair>
		{
			public CoordsPair(Vector3Int coordsA, Vector3Int coordsB)
			{
				this.CoordsA = coordsA;
				this.CoordsB = coordsB;
			}

			public bool Equals(RoomCullingConnection.CoordsPair other)
			{
				return this.CoordsA == other.CoordsA && this.CoordsB == other.CoordsB;
			}

			public override bool Equals(object obj)
			{
				if (obj is RoomCullingConnection.CoordsPair)
				{
					RoomCullingConnection.CoordsPair coordsPair = (RoomCullingConnection.CoordsPair)obj;
					return this.Equals(coordsPair);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine<Vector3Int, Vector3Int>(this.CoordsA, this.CoordsB);
			}

			public readonly Vector3Int CoordsA;

			public readonly Vector3Int CoordsB;
		}

		public readonly struct RoomLink
		{
			public bool BothCulled
			{
				get
				{
					return this.CullableA.IsCulled && this.CullableB.IsCulled;
				}
			}

			public RoomLink(RoomIdentifier a, RoomIdentifier b)
			{
				this.RoomA = a;
				this.RoomB = b;
				bool flag = a.TryGetComponent<CullableRoom>(out this.CullableA);
				bool flag2 = b.TryGetComponent<CullableRoom>(out this.CullableB);
				Vector3Int vector3Int;
				Vector3Int vector3Int2;
				bool flag3;
				if (a.TryGetMainCoords(out vector3Int) && b.TryGetMainCoords(out vector3Int2))
				{
					this.Coords = new RoomCullingConnection.CoordsPair(vector3Int, vector3Int2);
					this.InvCoords = new RoomCullingConnection.CoordsPair(vector3Int2, vector3Int);
					flag3 = true;
				}
				else
				{
					this.Coords = default(RoomCullingConnection.CoordsPair);
					this.InvCoords = default(RoomCullingConnection.CoordsPair);
					flag3 = false;
				}
				this.Valid = flag && flag2 && flag3;
				if (this.Valid)
				{
					this.Rooms = new RoomIdentifier[] { this.RoomA, this.RoomB };
					this.Cullables = new CullableRoom[] { this.CullableA, this.CullableB };
					return;
				}
				this.Rooms = new RoomIdentifier[0];
				this.Cullables = new CullableRoom[0];
			}

			public readonly RoomCullingConnection.CoordsPair Coords;

			public readonly RoomCullingConnection.CoordsPair InvCoords;

			public readonly RoomIdentifier RoomA;

			public readonly RoomIdentifier RoomB;

			public readonly RoomIdentifier[] Rooms;

			public readonly CullableRoom CullableA;

			public readonly CullableRoom CullableB;

			public readonly CullableRoom[] Cullables;

			public readonly bool Valid;
		}
	}
}
