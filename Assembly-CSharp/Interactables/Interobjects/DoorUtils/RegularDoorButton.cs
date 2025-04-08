using System;
using TMPro;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class RegularDoorButton : InteractableCollider
	{
		protected override void Awake()
		{
			base.Awake();
			this._useText = this._mainText != null;
		}

		public void SetupButton(string text, Material mat)
		{
			if (this._useText)
			{
				this._mainText.text = text;
			}
			this._mainRenderer.sharedMaterial = mat;
		}

		[SerializeField]
		private TextMeshProUGUI _mainText;

		[SerializeField]
		private MeshRenderer _mainRenderer;

		private bool _useText;
	}
}
