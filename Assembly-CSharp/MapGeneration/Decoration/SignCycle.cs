using System;
using TMPro;
using UnityEngine;

namespace MapGeneration.Decoration;

public class SignCycle : MonoBehaviour
{
	[Serializable]
	private struct CycleData
	{
		public float Duration;

		public string Text;
	}

	[SerializeField]
	private CycleData[] _cycles;

	[SerializeField]
	private TMP_Text _targetText;

	private int _currentIndex;

	private float _timePassed;

	private void Update()
	{
		CycleData cycleData = this._cycles[this._currentIndex];
		this._timePassed += Time.deltaTime;
		if (!(this._timePassed < cycleData.Duration))
		{
			this._timePassed = 0f;
			if (++this._currentIndex >= this._cycles.Length)
			{
				this._currentIndex = 0;
			}
			this._targetText.text = this._cycles[this._currentIndex].Text;
		}
	}
}
