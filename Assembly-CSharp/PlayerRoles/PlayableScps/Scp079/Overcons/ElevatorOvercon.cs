using Interactables.Interobjects;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons;

public class ElevatorOvercon : StandardOvercon
{
	private static Color _busyColor = new Color(1f, 1f, 1f, 0.1f);

	public ElevatorDoor Target { get; internal set; }

	private Color TargetColor
	{
		get
		{
			if (!ElevatorChamber.TryGetChamber(this.Target.Group, out var chamber))
			{
				return ElevatorOvercon._busyColor;
			}
			if (!chamber.IsReady)
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
		base.TargetSprite.color = this.TargetColor;
	}
}
