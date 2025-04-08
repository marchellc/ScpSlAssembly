using System;
using Respawning.Objectives;
using Respawning.Waves;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics
{
	public class WaveDebugInterface : SerializedWaveInterface
	{
		private void Start()
		{
			Color color;
			if (!ColorUtility.TryParseHtmlString(base.Wave.TargetFaction.GetFactionColor(), out color))
			{
				return;
			}
			this._text.color = color;
		}

		private void Update()
		{
			if (base.Wave == null)
			{
				return;
			}
			this._text.text = base.Wave.CreateDebugString();
		}

		[SerializeField]
		private TMP_Text _text;
	}
}
