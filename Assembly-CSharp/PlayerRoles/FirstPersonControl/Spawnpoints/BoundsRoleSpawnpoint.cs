using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints;

public class BoundsRoleSpawnpoint : ISpawnpointHandler
{
	private int _lastIndex;

	private readonly Vector3[] _positions;

	private readonly float _rotMin;

	private readonly float _rotMax;

	private const int AmountThreshold = 64;

	private int NextIndex
	{
		get
		{
			if (++this._lastIndex >= this._positions.Length)
			{
				this._lastIndex = 0;
			}
			return this._lastIndex;
		}
	}

	public BoundsRoleSpawnpoint(Vector3 pos, float rot)
	{
		this._positions = new Vector3[1] { pos };
		this._rotMin = rot;
		this._rotMax = rot;
	}

	public BoundsRoleSpawnpoint(Vector3 posMin, Vector3 posMax, float rotMin, float rotMax, Vector3Int size)
	{
		this._positions = this.GeneratePositions(posMin, posMax, size);
		this._rotMin = rotMin;
		this._rotMax = rotMax;
	}

	public BoundsRoleSpawnpoint(Bounds bounds, float rotMin, float rotMax, Vector3Int size)
	{
		this._positions = this.GeneratePositions(bounds.min, bounds.max, size);
		this._rotMin = rotMin;
		this._rotMax = rotMax;
	}

	public bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
	{
		horizontalRot = Random.Range(this._rotMin, this._rotMax);
		if (this._positions.Length != 0)
		{
			position = this._positions[this.NextIndex];
			return true;
		}
		position = Vector3.zero;
		return false;
	}

	private Vector3[] GeneratePositions(Vector3 min, Vector3 max, Vector3Int size)
	{
		int num = size.x * size.y * size.z;
		if (num > 64)
		{
			Debug.LogError($"One of the spawnpoints has more than {64} potential positions. Consider reducing its size.");
		}
		Vector3 stepSize = new Vector3(1f / (float)size.x, 1f / (float)size.y, 1f / (float)size.z);
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < size.x; i++)
		{
			for (int j = 0; j < size.y; j++)
			{
				for (int k = 0; k < size.z; k++)
				{
					list.Add(this.GeneratePosition(stepSize, i, j, k, min, max));
				}
			}
		}
		Vector3[] array = new Vector3[num];
		for (int l = 0; l < num; l++)
		{
			array[l] = list.PullRandomItem();
		}
		return array;
	}

	private Vector3 GeneratePosition(Vector3 stepSize, int x, int y, int z, Vector3 min, Vector3 max)
	{
		Vector3 vector = new Vector3(stepSize.x * (float)x, stepSize.y * (float)y, stepSize.z * (float)z) + stepSize / 2f;
		return new Vector3(Mathf.Lerp(min.x, max.x, vector.x), Mathf.Lerp(min.y, max.y, vector.y), Mathf.Lerp(min.z, max.z, vector.z));
	}
}
