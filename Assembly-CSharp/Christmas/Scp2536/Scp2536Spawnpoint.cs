using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace Christmas.Scp2536
{
	public class Scp2536Spawnpoint : MonoBehaviour
	{
		public Vector3 Position { get; private set; }

		public Quaternion Rotation { get; private set; }

		private void Awake()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._eventAssigned = true;
			Scp2536Spawnpoint.Spawnpoints.Add(this);
			SeedSynchronizer.OnGenerationFinished += this.OnGen;
		}

		private void OnDestroy()
		{
			if (!this._eventAssigned)
			{
				return;
			}
			Scp2536Spawnpoint.Spawnpoints.Remove(this);
			this._eventAssigned = false;
			SeedSynchronizer.OnGenerationFinished -= this.OnGen;
		}

		private void OnGen()
		{
			Transform transform = base.transform;
			this.Position = transform.position;
			this.Rotation = transform.rotation;
		}

		public static readonly HashSet<Scp2536Spawnpoint> Spawnpoints = new HashSet<Scp2536Spawnpoint>();

		private bool _eventAssigned;
	}
}
