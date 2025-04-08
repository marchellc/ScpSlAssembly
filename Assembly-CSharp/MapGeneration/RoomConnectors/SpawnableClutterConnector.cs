using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration.Clutter;
using UnityEngine;

namespace MapGeneration.RoomConnectors
{
	public class SpawnableClutterConnector : SpawnableRoomConnector
	{
		public bool Intersects(Bounds targetBounds)
		{
			Transform transform = base.transform;
			foreach (SpawnableClutterConnector.SerializedClutter serializedClutter in this._clutter)
			{
				if (serializedClutter.GetWorldspaceBounds(transform).Intersects(targetBounds))
				{
					return true;
				}
			}
			return false;
		}

		private void Start()
		{
			IRoomConnector component = base.GetComponent<IRoomConnector>();
			if (component.RoomsAlreadyRegistered)
			{
				this.CheckClutterConflicts();
				return;
			}
			component.OnRoomsRegistered += this.CheckClutterConflicts;
		}

		private void CheckClutterConflicts()
		{
			Transform transform = base.transform;
			foreach (SpawnableClutterConnector.SerializedClutter serializedClutter in this._clutter)
			{
				Bounds worldspaceBounds = serializedClutter.GetWorldspaceBounds(transform);
				using (List<IClutterBlocker>.Enumerator enumerator2 = IClutterBlocker.Instances.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.BlockingBounds.Intersects(worldspaceBounds))
						{
							global::UnityEngine.Object.Destroy(serializedClutter.TargetClutter);
						}
					}
				}
			}
		}

		private void Awake()
		{
			SpawnableClutterConnector.Instances.Add(this);
		}

		private void OnDestroy()
		{
			SpawnableClutterConnector.Instances.Remove(this);
		}

		private void OnDrawGizmosSelected()
		{
			foreach (SpawnableClutterConnector.SerializedClutter serializedClutter in this._clutter)
			{
				Bounds worldspaceBounds = serializedClutter.GetWorldspaceBounds(base.transform);
				Gizmos.color = Color.green;
				Gizmos.DrawWireCube(worldspaceBounds.center, worldspaceBounds.size);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += SpawnableClutterConnector.Instances.Clear;
		}

		public override bool Weaved()
		{
			return true;
		}

		public static readonly List<SpawnableClutterConnector> Instances = new List<SpawnableClutterConnector>();

		[SerializeField]
		private List<SpawnableClutterConnector.SerializedClutter> _clutter;

		[Serializable]
		private struct SerializedClutter
		{
			public Bounds GetWorldspaceBounds(Transform refTransform)
			{
				Vector3 vector;
				Quaternion quaternion;
				refTransform.GetPositionAndRotation(out vector, out quaternion);
				Vector3 vector2 = quaternion * this._bounds.center;
				Vector3 vector3 = (quaternion * this._bounds.size).Abs();
				return new Bounds(vector + vector2, vector3);
			}

			public GameObject TargetClutter;

			[SerializeField]
			private Bounds _bounds;
		}
	}
}
