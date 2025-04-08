using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AlphaWarheadScreen : MonoBehaviour
{
	private void Start()
	{
		if (!AlphaWarheadScreen.Instances.Contains(this))
		{
			AlphaWarheadScreen.Instances.Add(this);
		}
		this.UpdateScreen();
	}

	private void OnDestroy()
	{
		AlphaWarheadScreen.Instances.Remove(this);
	}

	private void UpdateScreen()
	{
		this._text.text = AlphaWarheadScreen._lastText;
		GameObject[] inevitable = this._inevitable;
		for (int i = 0; i < inevitable.Length; i++)
		{
			inevitable[i].SetActive(AlphaWarheadScreen._lastInevitable);
		}
	}

	public static void SetScreen(string text, bool showInevitableWarn)
	{
		AlphaWarheadScreen._lastText = text;
		AlphaWarheadScreen._lastInevitable = showInevitableWarn;
		foreach (AlphaWarheadScreen alphaWarheadScreen in AlphaWarheadScreen.Instances)
		{
			alphaWarheadScreen.UpdateScreen();
		}
	}

	private static readonly List<AlphaWarheadScreen> Instances = new List<AlphaWarheadScreen>();

	private static string _lastText;

	private static bool _lastInevitable;

	[SerializeField]
	private TMP_Text _text;

	[SerializeField]
	private GameObject[] _inevitable;
}
