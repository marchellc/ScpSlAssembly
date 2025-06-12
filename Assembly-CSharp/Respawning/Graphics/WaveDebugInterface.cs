using Respawning.Objectives;
using Respawning.Waves;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics;

public class WaveDebugInterface : SerializedWaveInterface
{
	[SerializeField]
	private TMP_Text _text;

	private void Start()
	{
		if (ColorUtility.TryParseHtmlString(base.Wave.TargetFaction.GetFactionColor(), out var color))
		{
			this._text.color = color;
		}
	}

	private void Update()
	{
		if (base.Wave != null)
		{
			this._text.text = base.Wave.CreateDebugString();
		}
	}
}
