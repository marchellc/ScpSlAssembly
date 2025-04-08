using System;
using System.Collections.Generic;
using UnityEngine;

public class ClassPresetChooser : MonoBehaviour
{
	private void Update()
	{
	}

	public GameObject bottomMenuItem;

	public Transform bottomMenuHolder;

	public ClassPresetChooser.PickerPreset[] presets;

	private string curKey;

	private List<ClassPresetChooser.PickerPreset> curPresets = new List<ClassPresetChooser.PickerPreset>();

	[Serializable]
	public class PickerPreset
	{
		public string classID;

		public Texture icon;

		public int health;

		public float wSpeed;

		public float rSpeed;

		public float stamina;

		public Texture[] startingItems;

		public string en_additionalInfo;

		public string pl_additionalInfo;
	}
}
