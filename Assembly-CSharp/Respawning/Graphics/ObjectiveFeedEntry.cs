using System;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics
{
	public class ObjectiveFeedEntry : MonoBehaviour
	{
		public void CreateDisplay(string text)
		{
			this._endTime = Time.time + 5f;
			this._textDisplay.text = text;
			this._textDisplay.alpha = 1f;
			base.gameObject.SetActive(true);
			base.transform.SetAsFirstSibling();
		}

		private void Awake()
		{
			this._textDisplay = base.GetComponent<TextMeshProUGUI>();
		}

		private void Update()
		{
			float num = this._endTime - Time.time;
			if (num < 1f)
			{
				this._textDisplay.alpha = num;
			}
			if (num < 0f)
			{
				base.gameObject.SetActive(false);
			}
		}

		private const float EntryDuration = 5f;

		private TextMeshProUGUI _textDisplay;

		private float _endTime;
	}
}
