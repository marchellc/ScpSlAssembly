using System.Collections.Generic;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace Christmas.Scp2536;

public class Scp2536Spawnpoint : MonoBehaviour
{
	public static readonly HashSet<Scp2536Spawnpoint> Spawnpoints = new HashSet<Scp2536Spawnpoint>();

	private bool _eventAssigned;

	public Vector3 Position { get; private set; }

	public Quaternion Rotation { get; private set; }

	private void Awake()
	{
		if (NetworkServer.active)
		{
			this._eventAssigned = true;
			Scp2536Spawnpoint.Spawnpoints.Add(this);
			SeedSynchronizer.OnGenerationFinished += OnGen;
		}
	}

	private void OnDestroy()
	{
		if (this._eventAssigned)
		{
			Scp2536Spawnpoint.Spawnpoints.Remove(this);
			this._eventAssigned = false;
			SeedSynchronizer.OnGenerationFinished -= OnGen;
		}
	}

	private void OnGen()
	{
		Transform transform = base.transform;
		this.Position = transform.position;
		this.Rotation = transform.rotation;
	}
}
