using System;
using Respawning.Waves.Generic;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics
{
	public class RespawnTokensDisplay : SerializedWaveInterface
	{
		private void Update()
		{
			ILimitedWave limitedWave = base.Wave as ILimitedWave;
			if (limitedWave == null)
			{
				return;
			}
			int respawnTokens = limitedWave.RespawnTokens;
			if (respawnTokens == this._lastTokens)
			{
				return;
			}
			this._lastTokens = respawnTokens;
			this._respawnTokens.text = respawnTokens.ToString();
		}

		[SerializeField]
		private TextMeshProUGUI _respawnTokens;

		private int _lastTokens = 1;
	}
}
