using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration.Clutter;
using UnityEngine;

namespace MapGeneration.RoomConnectors;

public class SpawnableClutterConnector : SpawnableRoomConnector
{
	[Serializable]
	private struct SerializedClutter
	{
		public GameObject TargetClutter;

		[SerializeField]
		private Bounds _bounds;

		public Bounds GetWorldspaceBounds(Transform refTransform)
		{
			refTransform.GetPositionAndRotation(out var position, out var rotation);
			Vector3 vector = rotation * this._bounds.center;
			Vector3 size = (rotation * this._bounds.size).Abs();
			return new Bounds(position + vector, size);
		}
	}

	public static readonly List<SpawnableClutterConnector> Instances = new List<SpawnableClutterConnector>();

	[SerializeField]
	private List<SerializedClutter> _clutter;

	public bool Intersects(Bounds targetBounds)
	{
		Transform refTransform = base.transform;
		foreach (SerializedClutter item in this._clutter)
		{
			if (item.GetWorldspaceBounds(refTransform).Intersects(targetBounds))
			{
				return true;
			}
		}
		return false;
	}

	protected override void Start()
	{
		base.Start();
		IRoomConnector component = base.GetComponent<IRoomConnector>();
		if (component.RoomsAlreadyRegistered)
		{
			this.CheckClutterConflicts();
		}
		else
		{
			component.OnRoomsRegistered += CheckClutterConflicts;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		SpawnableClutterConnector.Instances.Remove(this);
	}

	private void CheckClutterConflicts()
	{
		Transform refTransform = base.transform;
		foreach (SerializedClutter item in this._clutter)
		{
			Bounds worldspaceBounds = item.GetWorldspaceBounds(refTransform);
			foreach (IClutterBlocker instance in IClutterBlocker.Instances)
			{
				if (instance.BlockingBounds.Intersects(worldspaceBounds))
				{
					UnityEngine.Object.Destroy(item.TargetClutter);
				}
			}
		}
	}

	private void Awake()
	{
		SpawnableClutterConnector.Instances.Add(this);
	}

	private void OnDrawGizmosSelected()
	{
		foreach (SerializedClutter item in this._clutter)
		{
			Bounds worldspaceBounds = item.GetWorldspaceBounds(base.transform);
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
}
