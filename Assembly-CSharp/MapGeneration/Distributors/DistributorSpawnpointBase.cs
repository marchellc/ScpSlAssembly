using System;
using UnityEngine;

namespace MapGeneration.Distributors;

public abstract class DistributorSpawnpointBase : MonoBehaviour
{
	private RoomName _roomName;

	private bool _roomSet;

	public RoomName RoomName
	{
		get
		{
			if (this._roomSet)
			{
				return this._roomName;
			}
			if (base.transform.position.TryGetRoom(out var room))
			{
				this._roomName = room.Name;
				this._roomSet = true;
				return this._roomName;
			}
			Debug.LogError("This spawnpoint (" + base.transform.GetHierarchyPath() + ") has no valid room!", base.gameObject);
			throw new InvalidOperationException("Misplaced spawnpoint detected!");
		}
	}

	protected virtual void Awake()
	{
		base.transform.localScale = Vector3.one;
	}
}
