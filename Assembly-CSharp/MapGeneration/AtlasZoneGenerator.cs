using System;
using System.Collections.Generic;
using MapGeneration.Holidays;
using UnityEngine;

namespace MapGeneration;

public class AtlasZoneGenerator : ZoneGenerator
{
	public struct SpawnedRoomData
	{
		public AtlasInterpretation Interpretation;

		public SpawnableRoom ChosenCandidate;

		public SpawnableRoom Instance;
	}

	private readonly List<SpawnableRoom> _spawnCandidates = new List<SpawnableRoom>();

	[SerializeField]
	private float _zoneHeight;

	public SpawnableRoom[] CompatibleRooms;

	public Texture2D[] Atlases;

	public Transform RoomSpawnParent;

	public readonly List<SpawnedRoomData> Spawned = new List<SpawnedRoomData>();

	public AtlasInterpretation[] Interpreted { get; private set; }

	public override void Generate(System.Random rng)
	{
		SpawnableRoom[] compatibleRooms = this.CompatibleRooms;
		foreach (SpawnableRoom spawnableRoom in compatibleRooms)
		{
			if (spawnableRoom.HolidayVariants.TryGetResult<HolidayRoomVariant, SpawnableRoom>(out var result))
			{
				result.RegisterIdentities();
			}
			else
			{
				spawnableRoom.RegisterIdentities();
			}
		}
		Texture2D atlas = this.Atlases[rng.Next(this.Atlases.Length)];
		this.Interpreted = MapAtlasInterpreter.Singleton.Interpret(atlas, rng);
		this.RandomizeInterpreted(rng);
		for (int j = 0; j < this.Interpreted.Length; j++)
		{
			try
			{
				this.ProcessInterpreted(this.Interpreted[j], rng);
			}
			catch (Exception exception)
			{
				AtlasInterpretation atlasInterpretation = this.Interpreted[j];
				Debug.LogError("Interpretation failed at " + atlasInterpretation.ToString());
				Debug.LogException(exception);
			}
		}
	}

	protected virtual void RandomizeInterpreted(System.Random rng)
	{
		int num = this.Interpreted.Length;
		while (num > 1)
		{
			num--;
			int num2 = rng.Next(num + 1);
			ref AtlasInterpretation reference = ref this.Interpreted[num];
			ref AtlasInterpretation reference2 = ref this.Interpreted[num2];
			AtlasInterpretation atlasInterpretation = this.Interpreted[num2];
			AtlasInterpretation atlasInterpretation2 = this.Interpreted[num];
			reference = atlasInterpretation;
			reference2 = atlasInterpretation2;
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
		foreach (SpawnedRoomData item in this.Spawned)
		{
			if (!(item.ChosenCandidate != candidate))
			{
				Vector2Int coords2 = item.Interpretation.Coords;
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
		foreach (SpawnedRoomData item in this.Spawned)
		{
			if (!(item.ChosenCandidate != candidate))
			{
				num++;
			}
		}
		return num;
	}

	public void SpawnRoom(AtlasInterpretation interpretation, SpawnableRoom chosenCandidate)
	{
		this.GetPositionAndRotation(interpretation, out var worldPosition, out var yRotation);
		SpawnableRoom spawnableRoom = UnityEngine.Object.Instantiate(chosenCandidate, worldPosition, Quaternion.Euler(0f, yRotation, 0f), this.RoomSpawnParent);
		spawnableRoom.SetupNetIdHandlers(this.PreviouslySpawnedCnt(chosenCandidate));
		this.Spawned.Add(new SpawnedRoomData
		{
			ChosenCandidate = chosenCandidate,
			Instance = spawnableRoom,
			Interpretation = interpretation
		});
	}

	private void ProcessInterpreted(AtlasInterpretation interpretation, System.Random rng)
	{
		this._spawnCandidates.Clear();
		float num = 0f;
		bool flag = interpretation.SpecificRooms.Length != 0;
		for (int i = 0; i < this.CompatibleRooms.Length; i++)
		{
			SpawnableRoom spawnableRoom = this.CompatibleRooms[i];
			if (spawnableRoom.HolidayVariants.TryGetResult<HolidayRoomVariant, SpawnableRoom>(out var result))
			{
				spawnableRoom = result;
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
			Debug.LogError($"No candidates found for {base.TargetZone} {interpretation}");
			return;
		}
		double num3 = rng.NextDouble() * (double)num;
		float num4 = 0f;
		for (int j = 0; j < this._spawnCandidates.Count; j++)
		{
			SpawnableRoom spawnableRoom2 = this._spawnCandidates[j];
			if (spawnableRoom2.HolidayVariants.TryGetResult<HolidayRoomVariant, SpawnableRoom>(out var result2))
			{
				spawnableRoom2 = result2;
			}
			num4 += this.GetChanceWeight(interpretation.Coords, spawnableRoom2);
			if (num3 <= (double)num4)
			{
				this.SpawnRoom(interpretation, spawnableRoom2);
				return;
			}
		}
		Debug.LogError($"Random room spawning failed for {base.TargetZone} {interpretation}");
	}
}
