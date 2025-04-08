using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiGraphLine : Graphic
{
	public List<Vector2> RelativePoints
	{
		get
		{
			return this._relativePoints;
		}
		set
		{
			this._relativePoints = value;
			this.SetVerticesDirty();
		}
	}

	public float Width
	{
		get
		{
			return this._width;
		}
		set
		{
			this._width = value;
			this.SetVerticesDirty();
		}
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.color = this.color;
		int count = this._relativePoints.Count;
		if (count <= 1)
		{
			return;
		}
		this._rectSize = base.rectTransform.rect.size;
		this._rectOffset = base.rectTransform.pivot * this._rectSize;
		for (int i = 0; i < count - 1; i++)
		{
			Vector2 vector = this.ClampPoint(this._relativePoints[i]);
			Vector2 vector2 = this.ClampPoint(this._relativePoints[i + 1]);
			float num = this.RealAngle(vector2 - vector);
			this.PlacePoint(vh, vector, num);
			this.PlacePoint(vh, vector2, num);
			this.FillRectangle(vh, i * 4);
		}
	}

	private void FillRectangle(VertexHelper vh, int startIndex)
	{
		vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		vh.AddTriangle(startIndex + 1, startIndex + 2, startIndex + 3);
		if (startIndex == 0)
		{
			return;
		}
		vh.AddTriangle(startIndex - 2, startIndex, startIndex + 1);
		vh.AddTriangle(startIndex - 1, startIndex, startIndex + 1);
	}

	private float RealAngle(Vector2 v2)
	{
		return Vector2.Angle(v2, Vector2.up) * -Mathf.Sign(Vector2.Dot(v2, Vector2.right));
	}

	private void PlacePoint(VertexHelper vh, Vector2 p, float rotation)
	{
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.color = this.color;
		float num = Mathf.Sin(rotation * 0.017453292f);
		Vector2 vector = new Vector2(Mathf.Cos(rotation * 0.017453292f), num) * this.Width;
		simpleVert.position = p * this._rectSize + vector - this._rectOffset;
		vh.AddVert(simpleVert);
		simpleVert.position = p * this._rectSize - vector - this._rectOffset;
		vh.AddVert(simpleVert);
	}

	private Vector2 ClampPoint(Vector2 point)
	{
		return new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
	}

	[SerializeField]
	private List<Vector2> _relativePoints = new List<Vector2>();

	[SerializeField]
	private float _width;

	private Vector2 _rectSize;

	private Vector2 _rectOffset;
}
