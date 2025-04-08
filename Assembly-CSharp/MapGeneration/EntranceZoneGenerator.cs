using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration
{
	public class EntranceZoneGenerator : AtlasZoneGenerator
	{
		public override void Generate(global::System.Random rng)
		{
			if (this._hcz == null || this._hcz.Interpreted == null)
			{
				throw new InvalidOperationException("Entrance Zone requires HCZ to generate first. Adjust the order of execution.");
			}
			base.Generate(rng);
		}

		protected override void RandomizeInterpreted(global::System.Random rng)
		{
			this.OnBeforeRandomize();
			base.RandomizeInterpreted(rng);
		}

		public override void GetPositionAndRotation(AtlasInterpretation toSpawn, out Vector3 worldPosition, out float yRotation)
		{
			base.GetPositionAndRotation(toSpawn, out worldPosition, out yRotation);
			worldPosition += this._positionOffset;
			yRotation += this._hardRotationOffset;
		}

		private void OnBeforeRandomize()
		{
			Vector3 vector;
			this.FindCheckpointPositions(this._hcz, out vector);
			Vector3 vector2;
			this.FindCheckpointPositions(this, out vector2);
			this._positionOffset = vector - vector2 + this._hardPositionOffset;
		}

		private void FindCheckpointPositions(AtlasZoneGenerator generator, out Vector3 centroid)
		{
			List<Vector3> list = ListPool<Vector3>.Shared.Rent();
			foreach (AtlasInterpretation atlasInterpretation in generator.Interpreted)
			{
				if (atlasInterpretation.RoomShape == RoomShape.Straight && atlasInterpretation.SpecificRooms.Contains(RoomName.HczCheckpointToEntranceZone))
				{
					Vector3 vector;
					float num;
					generator.GetPositionAndRotation(atlasInterpretation, out vector, out num);
					list.Add(vector);
				}
			}
			if (list.Count != 2)
			{
				throw new InvalidOperationException("HCZ-EZ require two checkpoints!");
			}
			centroid = (list[0] + list[1]) / 2f;
			ListPool<Vector3>.Shared.Return(list);
		}

		[SerializeField]
		private AtlasZoneGenerator _hcz;

		[SerializeField]
		private Vector3 _hardPositionOffset;

		[SerializeField]
		private float _hardRotationOffset;

		private Vector3 _positionOffset;
	}
}
