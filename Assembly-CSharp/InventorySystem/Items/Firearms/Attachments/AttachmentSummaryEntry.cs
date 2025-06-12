using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments;

public class AttachmentSummaryEntry : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI _label;

	[SerializeField]
	private TextMeshProUGUI[] _valuesBank;

	[SerializeField]
	private Color _oddColor;

	private bool _firstSetup = true;

	public void Setup(string label, int valuesLen, Func<int, string> valueSelector, bool isOdd)
	{
		if (isOdd && this._firstSetup)
		{
			base.GetComponent<Image>().color = this._oddColor;
			this._firstSetup = false;
		}
		this._label.text = label;
		for (int i = 0; i < this._valuesBank.Length; i++)
		{
			if (i >= valuesLen)
			{
				this._valuesBank[i].gameObject.SetActive(value: false);
				continue;
			}
			this._valuesBank[i].gameObject.SetActive(value: true);
			this._valuesBank[i].text = valueSelector(i);
		}
	}
}
