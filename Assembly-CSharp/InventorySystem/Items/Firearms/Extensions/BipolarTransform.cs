using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	[Serializable]
	public class BipolarTransform
	{
		public bool Polarity
		{
			get
			{
				return this._polarity;
			}
			set
			{
				if (this._polarity == value)
				{
					return;
				}
				this._polarity = value;
				Offset offset = (value ? this.TruePole : this.FalsePole);
				Quaternion quaternion = Quaternion.Euler(offset.rotation);
				this.TargetTransform.localScale = offset.scale;
				this.TargetTransform.SetLocalPositionAndRotation(offset.position, quaternion);
			}
		}

		public Transform TargetTransform;

		[Space(15f)]
		public Offset TruePole;

		public Offset FalsePole;

		private bool _polarity;
	}
}
