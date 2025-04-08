using System;
using TMPro;
using UnityEngine;

namespace MapGeneration.Decoration
{
	public class SignCycle : MonoBehaviour
	{
		private void Update()
		{
			SignCycle.CycleData cycleData = this._cycles[this._currentIndex];
			this._timePassed += Time.deltaTime;
			if (this._timePassed < cycleData.Duration)
			{
				return;
			}
			this._timePassed = 0f;
			int num = this._currentIndex + 1;
			this._currentIndex = num;
			if (num >= this._cycles.Length)
			{
				this._currentIndex = 0;
			}
			this._targetText.text = this._cycles[this._currentIndex].Text;
		}

		[SerializeField]
		private SignCycle.CycleData[] _cycles;

		[SerializeField]
		private TMP_Text _targetText;

		private int _currentIndex;

		private float _timePassed;

		[Serializable]
		private struct CycleData
		{
			public float Duration;

			public string Text;
		}
	}
}
