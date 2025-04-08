using System;
using System.Collections.Generic;
using MapGeneration.Holidays;
using UnityEngine;

namespace MapGeneration
{
	public class AtlasZoneGenerator : ZoneGenerator
	{
		public AtlasInterpretation[] Interpreted { get; private set; }

		public override void Generate(global::System.Random rng)
		{
			foreach (SpawnableRoom spawnableRoom in this.CompatibleRooms)
			{
				SpawnableRoom spawnableRoom2;
				if (spawnableRoom.HolidayVariants.TryGetResult(out spawnableRoom2))
				{
					spawnableRoom2.RegisterIdentities();
				}
				else
				{
					spawnableRoom.RegisterIdentities();
				}
			}
			Texture2D texture2D = this.Atlases[rng.Next(this.Atlases.Length)];
			this.Interpreted = MapAtlasInterpreter.Singleton.Interpret(texture2D, rng);
			this.RandomizeInterpreted(rng);
			for (int j = 0; j < this.Interpreted.Length; j++)
			{
				try
				{
					this.ProcessInterpreted(this.Interpreted[j], rng);
				}
				catch (Exception ex)
				{
					string text = "Interpretation failed at ";
					AtlasInterpretation atlasInterpretation = this.Interpreted[j];
					Debug.LogError(text + atlasInterpretation.ToString());
					Debug.LogException(ex);
				}
			}
		}

		protected virtual void RandomizeInterpreted(global::System.Random rng)
		{
			int i = this.Interpreted.Length;
			while (i > 1)
			{
				i--;
				int num = rng.Next(i + 1);
				ref AtlasInterpretation ptr = ref this.Interpreted[i];
				AtlasInterpretation[] interpreted = this.Interpreted;
				int num2 = num;
				AtlasInterpretation atlasInterpretation = this.Interpreted[num];
				AtlasInterpretation atlasInterpretation2 = this.Interpreted[i];
				ptr = atlasInterpretation;
				interpreted[num2] = atlasInterpretation2;
			}
		}

		public virtual void GetPositionAndRotation(AtlasInterpretation toSpawn, out Vector3 worldPosition, out float yRotation)
		{
			worldPosition = new Vector3((float)toSpawn.Coords.x * RoomIdentifier.GridScale.x, this._zoneHeight, (float)toSpawn.Coords.y * RoomIdentifier.GridScale.z);
			yRotation = toSpawn.RotationY;
		}

		public virtual float GetChanceWeight(Vector2Int coords, SpawnableRoom candidate)
		{
			Vector2Int vector2Int = coords + Vector2Int.up;
			Vector2Int vector2Int2 = coords + Vector2Int.down;
			Vector2Int vector2Int3 = coords + Vector2Int.left;
			Vector2Int vector2Int4 = coords + Vector2Int.right;
			float num = candidate.ChanceMultiplier;
			foreach (AtlasZoneGenerator.SpawnedRoomData spawnedRoomData in this.Spawned)
			{
				if (!(spawnedRoomData.ChosenCandidate != candidate))
				{
					Vector2Int coords2 = spawnedRoomData.Interpretation.Coords;
					if (coords2 == vector2Int || coords2 == vector2Int2 || coords2 == vector2Int3 || coords2 == vector2Int4)
					{
						num *= candidate.AdjacentChanceMultiplier;
					}
				}
			}
			return num;
		}

		public int PreviouslySpawnedCnt(SpawnableRoom candidate)
		{
			int num = 0;
			using (List<AtlasZoneGenerator.SpawnedRoomData>.Enumerator enumerator = this.Spawned.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!(enumerator.Current.ChosenCandidate != candidate))
					{
						num++;
					}
				}
			}
			return num;
		}

		public void SpawnRoom(AtlasInterpretation interpretation, SpawnableRoom chosenCandidate)
		{
			Vector3 vector;
			float num;
			this.GetPositionAndRotation(interpretation, out vector, out num);
			SpawnableRoom spawnableRoom = global::UnityEngine.Object.Instantiate<SpawnableRoom>(chosenCandidate, this.RoomSpawnParent);
			spawnableRoom.transform.SetPositionAndRotation(vector, Quaternion.Euler(0f, num, 0f));
			spawnableRoom.SetupNetIdHandlers(this.PreviouslySpawnedCnt(chosenCandidate));
			this.Spawned.Add(new AtlasZoneGenerator.SpawnedRoomData
			{
				ChosenCandidate = chosenCandidate,
				Instance = spawnableRoom,
				Interpretation = interpretation
			});
		}

		private void ProcessInterpreted(AtlasInterpretation interpretation, global::System.Random rng)
		{
			this._spawnCandidates.Clear();
			float num = 0f;
			bool flag = interpretation.SpecificRooms.Length != 0;
			for (int i = 0; i < this.CompatibleRooms.Length; i++)
			{
				SpawnableRoom spawnableRoom = this.CompatibleRooms[i];
				SpawnableRoom spawnableRoom2;
				if (spawnableRoom.HolidayVariants.TryGetResult(out spawnableRoom2))
				{
					spawnableRoom = spawnableRoom2;
				}
				int num2 = this.PreviouslySpawnedCnt(spawnableRoom);
				if (flag == spawnableRoom.SpecialRoom && (!flag || interpretation.SpecificRooms.Contains(spawnableRoom.Room.Name)) && spawnableRoom.Room.Shape == interpretation.RoomShape && num2 < spawnableRoom.MaxAmount)
				{
					if (num2 < spawnableRoom.MinAmount)
					{
						this.SpawnRoom(interpretation, spawnableRoom);
						return;
					}
					num += this.GetChanceWeight(interpretation.Coords, spawnableRoom);
					this._spawnCandidates.Add(spawnableRoom);
				}
			}
			if (this._spawnCandidates.Count == 0 || num == 0f)
			{
				Debug.LogError(string.Format("No candidates found for {0} {1}", this.TargetZone, interpretation));
				return;
			}
			double num3 = rng.NextDouble() * (double)num;
			float num4 = 0f;
			for (int j = 0; j < this._spawnCandidates.Count; j++)
			{
				SpawnableRoom spawnableRoom3 = this._spawnCandidates[j];
				SpawnableRoom spawnableRoom4;
				if (spawnableRoom3.HolidayVariants.TryGetResult(out spawnableRoom4))
				{
					spawnableRoom3 = spawnableRoom4;
				}
				num4 += this.GetChanceWeight(interpretation.Coords, spawnableRoom3);
				if (num3 <= (double)num4)
				{
					this.SpawnRoom(interpretation, spawnableRoom3);
					return;
				}
			}
			Debug.LogError(string.Format("Random room spawning failed for {0} {1}", this.TargetZone, interpretation));
		}

		private readonly List<SpawnableRoom> _spawnCandidates = new List<SpawnableRoom>();

		[SerializeField]
		private float _zoneHeight;

		public SpawnableRoom[] CompatibleRooms;

		public Texture2D[] Atlases;

		public Transform RoomSpawnParent;

		public readonly List<AtlasZoneGenerator.SpawnedRoomData> Spawned = new List<AtlasZoneGenerator.SpawnedRoomData>();

		public struct SpawnedRoomData
		{
			public AtlasInterpretation Interpretation;

			public SpawnableRoom ChosenCandidate;

			public SpawnableRoom Instance;
		}
	}
}
