using System;
using UnityEngine;
using UnityEngine.UI;

public class UiCircle : MaskableGraphic
{
	[Range(3f, 180f)]
	[SerializeField]
	private int _vertsCount = 64;

	[SerializeField]
	private float _radius;

	[SerializeField]
	private float _width;

	public float Radius
	{
		get
		{
			return this._radius;
		}
		set
		{
			this._radius = value;
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
		float radius = this.Radius;
		float num = this.Radius - this.Width;
		this.DrawVertCircle(vh, simpleVert, radius);
		if (num <= 0f)
		{
			this.RenderFullCircle(vh, simpleVert);
			return;
		}
		this.DrawVertCircle(vh, simpleVert, num);
		for (int i = 0; i < this._vertsCount; i++)
		{
			vh.AddTriangle(i, (i + 1) % this._vertsCount, i + this._vertsCount);
			vh.AddTriangle(i, i + this._vertsCount, (i - 1 + this._vertsCount) % this._vertsCount + this._vertsCount);
		}
	}

	private void DrawVertCircle(VertexHelper vh, UIVertex vert, float radius)
	{
		float num = MathF.PI * 2f / (float)this._vertsCount;
		float num2 = 0f;
		for (int i = 0; i < this._vertsCount; i++)
		{
			float num3 = Mathf.Sin(num2);
			float num4 = Mathf.Sin(num2 + MathF.PI / 2f);
			vert.position = new Vector2(num3 * radius, num4 * radius);
			vh.AddVert(vert);
			num2 += num;
		}
	}

	private void RenderFullCircle(VertexHelper vh, UIVertex vert)
	{
		vert.position = Vector2.zero;
		vh.AddVert(vert);
		for (int i = 0; i < this._vertsCount; i++)
		{
			vh.AddTriangle(i, (i + 1) % this._vertsCount, this._vertsCount);
		}
	}
}
