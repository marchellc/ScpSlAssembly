using System;
using UnityEngine;

namespace ToggleableMenus
{
	public class SimpleToggleableMenu : ToggleableMenuBase
	{
		public override bool CanToggle
		{
			get
			{
				return true;
			}
		}

		protected override void OnToggled()
		{
			this._targetRoot.SetActive(this.IsEnabled);
		}

		[SerializeField]
		private GameObject _targetRoot;
	}
}
