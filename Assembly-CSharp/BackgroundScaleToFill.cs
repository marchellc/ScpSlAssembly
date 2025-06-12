using UnityEngine;

public class BackgroundScaleToFill : MonoBehaviour
{
	private Vector2Int? _prevResolution;

	[SerializeField]
	private Vector2 _originalResolution;

	[SerializeField]
	private Vector2 _scale;

	private void Update()
	{
		Vector2Int vector2Int = new Vector2Int(Screen.width, Screen.height);
		Vector2Int value = vector2Int;
		Vector2Int? prevResolution = this._prevResolution;
		if (!(value == prevResolution))
		{
			this._prevResolution = vector2Int;
			this.Rescale((float)vector2Int.x / (float)vector2Int.y);
		}
	}

	private void Rescale(float screenAspect)
	{
		float num = this._originalResolution.x / this._originalResolution.y;
		if (screenAspect > num)
		{
			float num2 = screenAspect / num;
			base.transform.localScale = new Vector3(this._scale.x, this._scale.y * num2, 1f);
		}
		else
		{
			float num3 = num / screenAspect;
			base.transform.localScale = new Vector3(this._scale.x * num3, this._scale.y, 1f);
		}
	}

	private void Reset()
	{
		this._originalResolution = new Vector2(16f, 9f);
		this._scale = Vector2.one;
	}
}
