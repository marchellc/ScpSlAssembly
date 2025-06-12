using System.Collections.Generic;
using UnityEngine;

public class ContentFitter : MonoBehaviour
{
	public bool continuousUpdate;

	public RectTransform targetTransform;

	private List<RectTransform> transforms = new List<RectTransform>();

	private void LateUpdate()
	{
		if (this.continuousUpdate)
		{
			this.continuousUpdate = false;
			this.Fit();
		}
	}

	public void Fit()
	{
		this.transforms.Clear();
		RectTransform[] componentsInChildren = base.GetComponentsInChildren<RectTransform>();
		foreach (RectTransform rectTransform in componentsInChildren)
		{
			if (rectTransform != base.GetComponent<RectTransform>())
			{
				this.transforms.Add(rectTransform);
			}
		}
		Vector2 vector = new Vector2(1E+09f, -1E+09f);
		Vector2 vector2 = new Vector2(-1E+09f, 1E+09f);
		foreach (RectTransform transform in this.transforms)
		{
			Vector2 vector3 = new Vector2(transform.localPosition.x - transform.sizeDelta.x * transform.pivot.x, transform.localPosition.y + transform.sizeDelta.y * transform.pivot.y);
			Vector2 vector4 = new Vector2(transform.localPosition.x + transform.sizeDelta.x * (1f - transform.pivot.x), transform.localPosition.y - transform.sizeDelta.y * (1f - transform.pivot.y));
			if (vector3.x < vector.x)
			{
				vector.x = vector3.x;
			}
			if (vector3.y > vector.y)
			{
				vector.y = vector3.y;
			}
			if (vector4.y < vector2.y)
			{
				vector2.y = vector4.y;
			}
			if (vector4.x > vector2.x)
			{
				vector2.x = vector4.x;
			}
		}
		Vector2 sizeDelta = new Vector2(Mathf.Abs(vector.x - vector2.x), Mathf.Abs(vector.y - vector2.y));
		this.targetTransform.localPosition = vector;
		this.targetTransform.sizeDelta = sizeDelta;
	}
}
