using System;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class PopupImageInterobject : PopupInterobject
	{
		private static UserMainInterface UserGUI
		{
			get
			{
				return UserMainInterface.singleton;
			}
		}

		protected override void OnClientStateChange()
		{
			if (PopupInterobject.CurrentState != PopupInterobject.PopupState.Enabling)
			{
				return;
			}
			PopupInterobject.TrackedPosition = base.transform.position;
		}

		protected override void OnClientUpdate(float enableRatio)
		{
		}

		public Sprite ImageToDisplay;

		public Vector2 Resolution;
	}
}
