using System;
using Interactables.Interobjects;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class ElevatorOvercon : StandardOvercon
	{
		public ElevatorDoor Target { get; internal set; }

		private Color TargetColor
		{
			get
			{
				ElevatorChamber elevatorChamber;
				if (!ElevatorChamber.TryGetChamber(this.Target.Group, out elevatorChamber))
				{
					return ElevatorOvercon._busyColor;
				}
				if (!elevatorChamber.IsReady)
				{
					return ElevatorOvercon._busyColor;
				}
				if (!this.IsHighlighted)
				{
					return StandardOvercon.NormalColor;
				}
				return StandardOvercon.HighlightedColor;
			}
		}

		private void LateUpdate()
		{
			this.TargetSprite.color = this.TargetColor;
		}

		private static Color _busyColor = new Color(1f, 1f, 1f, 0.1f);
	}
}
