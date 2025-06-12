using System.Collections.Generic;
using UnityEngine;

namespace DrawableLine;

public class DrawableLinesManager : MonoBehaviour
{
	private const int MaxExpectedSimultaneousMeshes = 550;

	private const string ColorKey = "_Color";

	private static readonly Quaternion QuadCorrection = Quaternion.Euler(90f, 0f, 0f);

	private static DrawableLinesManager _singleton;

	private static bool _singletonSet;

	[SerializeField]
	private Material _lineMaterial;

	[SerializeField]
	private Mesh _lineMesh;

	private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>(550);

	private readonly List<Vector4> _colors = new List<Vector4>(550);

	private readonly List<float> _timeStamps = new List<float>(550);

	private MaterialPropertyBlock _props;

	public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, float width = 0.05f)
	{
		if (DrawableLinesManager._singletonSet)
		{
			Vector3 vector = end - start;
			Vector3 pos = Vector3.Lerp(start, end, 0.5f);
			float item = Time.time + duration;
			float magnitude = vector.magnitude;
			Quaternion q = Quaternion.LookRotation(vector.normalized) * DrawableLinesManager.QuadCorrection;
			Vector3 s = new Vector3(width, magnitude, 1f);
			Matrix4x4 item2 = Matrix4x4.TRS(pos, q, s);
			DrawableLinesManager._singleton._timeStamps.Add(item);
			DrawableLinesManager._singleton._matrices.Add(item2);
			DrawableLinesManager._singleton._colors.Add(color);
		}
	}

	public static void ApplyMaxDurationRetroactively(float newMaxDuration)
	{
		if (!DrawableLinesManager._singletonSet)
		{
			return;
		}
		float num = Time.time + newMaxDuration;
		for (int i = 0; i < DrawableLinesManager._singleton._timeStamps.Count; i++)
		{
			if (!(DrawableLinesManager._singleton._timeStamps[i] < num))
			{
				DrawableLinesManager._singleton._timeStamps[i] = num;
			}
		}
	}

	private void DrawMeshes()
	{
		if (this._matrices.Count > 0)
		{
			this.CleanupExpiredLines();
			this._props.Clear();
			this._props.SetVectorArray("_Color", this._colors);
			Graphics.DrawMeshInstanced(this._lineMesh, 0, this._lineMaterial, this._matrices, this._props);
		}
	}

	private void CleanupExpiredLines()
	{
		for (int num = this._matrices.Count - 1; num >= 0; num--)
		{
			if (!(this._timeStamps[num] > Time.time))
			{
				this._colors.RemoveAt(num);
				this._matrices.RemoveAt(num);
				this._timeStamps.RemoveAt(num);
			}
		}
	}
}
