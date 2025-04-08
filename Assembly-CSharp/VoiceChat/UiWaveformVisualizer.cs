using System;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceChat
{
	public class UiWaveformVisualizer : Graphic
	{
		public ushort RenderersCount { get; internal set; } = 128;

		public float FlatlineHeight { get; internal set; } = 0.05f;

		public bool TwoSidedMode { get; internal set; } = true;

		public float MaxNormalizer { get; internal set; } = 30f;

		public AnimationCurve CorrectionCurve { get; internal set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		public void Generate(float[] samples)
		{
			this.Generate(samples, 0, samples.Length);
		}

		public void Generate(float[] samples, int startIndex, int length)
		{
			if (this.RenderersCount == 0)
			{
				this.SetVerticesDirty();
				return;
			}
			float[] recordedAverages = this._recordedAverages;
			int? num = ((recordedAverages != null) ? new int?(recordedAverages.Length) : null);
			int renderersCount = (int)this.RenderersCount;
			if ((num.GetValueOrDefault() == renderersCount) & (num != null))
			{
				Array.Clear(this._recordedAverages, 0, (int)this.RenderersCount);
			}
			else
			{
				this._recordedAverages = new float[(int)this.RenderersCount];
			}
			float num2 = 0f;
			float num3 = 0f;
			int num4 = 0;
			int num5 = 0;
			int num6 = length / (int)this.RenderersCount;
			float num7 = 1f / (float)num6;
			for (int i = 0; i < length; i++)
			{
				float num8 = samples[startIndex + i];
				if (num8 < 0f)
				{
					num2 += -num8;
				}
				else
				{
					num2 += num8;
				}
				num4++;
				if (num4 >= num6)
				{
					float num9 = num2 * num7;
					this._recordedAverages[num5++] = num9;
					num2 = 0f;
					num4 = 0;
					if (num9 > num3)
					{
						num3 = num9;
					}
					if (num5 >= (int)this.RenderersCount)
					{
						break;
					}
				}
			}
			float num10 = Mathf.Min(1f / ((num3 <= 0f) ? 1f : num3), this.MaxNormalizer);
			for (int j = 0; j < (int)this.RenderersCount; j++)
			{
				this._recordedAverages[j] *= num10;
			}
			this.SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			Rect rect = base.rectTransform.rect;
			UIVertex simpleVert = UIVertex.simpleVert;
			simpleVert.color = this.color;
			this.DrawWaveform(simpleVert, rect.width, rect.height / 2f, vh);
			if (this.TwoSidedMode)
			{
				this.DrawWaveform(simpleVert, rect.width, -rect.height / 2f, vh);
			}
		}

		private void DrawWaveform(UIVertex vert, float rectWidth, float rectHeight, VertexHelper vh)
		{
			float[] recordedAverages = this._recordedAverages;
			int num = ((recordedAverages != null) ? recordedAverages.Length : 0);
			float num2 = this.FlatlineHeight * rectHeight;
			float num3 = rectWidth / 2f;
			vert.position = new Vector2(-num3, num2);
			vh.AddVert(vert);
			if (num == 0)
			{
				int num4 = vh.currentVertCount - 1;
				vert.position = new Vector2(num3, num2);
				vh.AddVert(vert);
				vert.position = new Vector2(num3, 0f);
				vh.AddVert(vert);
				vert.position = new Vector2(-num3, 0f);
				vh.AddVert(vert);
				vh.AddTriangle(num4, num4 + 2, num4 + 1);
				vh.AddTriangle(num4, num4 + 2, num4 + 3);
				return;
			}
			float num5 = num2;
			float num6 = -num3;
			for (int i = 0; i < num; i++)
			{
				float num7 = rectHeight * this.CorrectionCurve.Evaluate(this._recordedAverages[i]) + num2;
				float num8 = rectWidth * (float)i / (float)(num - 1) - num3;
				vert.position = new Vector2(num8, num7);
				vh.AddVert(vert);
				vert.position = new Vector3(num6, num5);
				vh.AddVert(vert);
				vert.position = new Vector2(num8, 0f);
				vh.AddVert(vert);
				num6 = num8;
				num5 = num7;
				int num9 = vh.currentVertCount - 1;
				vh.AddTriangle(num9, num9 - 1, num9 - 2);
				vh.AddTriangle(num9, num9 - 1, num9 - 3);
			}
		}

		private float[] _recordedAverages;
	}
}
