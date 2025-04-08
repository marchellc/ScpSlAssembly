using System;
using System.Diagnostics;
using System.Text;
using NorthwoodLib.Pools;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards
{
	public class CIKeycardViewmodel : RegularKeycardViewmodel
	{
		private void Awake()
		{
			this._index = 0;
			this._text.text = this._lines[this._index];
			this._stopwatch = new Stopwatch();
			this._stopwatch.Start();
		}

		private void Update()
		{
			if (this._isRandomizing)
			{
				if (this._stopwatch.Elapsed.TotalSeconds <= 0.699999988079071)
				{
					StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
					for (int i = 0; i < 8; i++)
					{
						stringBuilder.Append("1234567890!@#$%^&*()QWERTYUIOPASDFGHJKLZXCVBNM"[global::UnityEngine.Random.Range(0, "1234567890!@#$%^&*()QWERTYUIOPASDFGHJKLZXCVBNM".Length)]);
					}
					this._text.SetText(stringBuilder);
					StringBuilderPool.Shared.Return(stringBuilder);
					return;
				}
				this._stopwatch.Restart();
				this._index = 0;
				this._text.text = this._lines[0];
				this._isRandomizing = false;
			}
			float num = (float)this._stopwatch.Elapsed.TotalSeconds;
			if (num >= this._timeBetweenLines)
			{
				this._index += Mathf.FloorToInt(num / this._timeBetweenLines);
				while (this._index >= this._lines.Length)
				{
					this._index -= Mathf.Max(1, this._lines.Length);
				}
				this._text.text = this._lines[this._index];
				this._stopwatch.Restart();
			}
		}

		protected override void PlayInteractAnimations()
		{
			base.PlayInteractAnimations();
			this._isRandomizing = true;
			this._stopwatch.Restart();
		}

		[SerializeField]
		private TMP_Text _text;

		[SerializeField]
		private string[] _lines;

		[SerializeField]
		private float _timeBetweenLines;

		private int _index;

		private Stopwatch _stopwatch;

		private bool _isRandomizing;

		private const float RandomizeTime = 0.7f;

		private const string RandomizableChars = "1234567890!@#$%^&*()QWERTYUIOPASDFGHJKLZXCVBNM";

		private const int RandomTextLength = 8;
	}
}
