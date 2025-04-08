﻿using System;

namespace InventorySystem.Items.Firearms.ShotEvents
{
	public class BulletShotEvent : ShotEvent, IMultiBarreledShot
	{
		public int BarrelId { get; }

		public BulletShotEvent(ItemIdentifier shotFirearm)
			: base(shotFirearm)
		{
			this.BarrelId = 0;
		}

		public BulletShotEvent(ItemIdentifier shotFirearm, int barrelId)
			: base(shotFirearm)
		{
			this.BarrelId = barrelId;
		}
	}
}
