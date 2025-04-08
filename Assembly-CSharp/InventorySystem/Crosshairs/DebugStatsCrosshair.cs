using System;
using System.Text;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using TMPro;
using UnityEngine;

namespace InventorySystem.Crosshairs
{
	public class DebugStatsCrosshair : FirearmCrosshairBase
	{
		public static bool Enabled { get; set; }

		protected override float GetAlpha(Firearm firearm)
		{
			return 1f;
		}

		protected override void UpdateCrosshair(Firearm firearm, float currentInaccuracy)
		{
			DisplayInaccuracyValues combinedDisplayInaccuracy = IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(firearm, false);
			this._circle.Radius = this._radiusRatio * currentInaccuracy;
			this._sb.Clear();
			this.AppendValue("Total", currentInaccuracy);
			this._sb.Append("\nDisplay values\n");
			this.AppendValue("Hip", combinedDisplayInaccuracy.HipDeg);
			this.AppendValue("Running", combinedDisplayInaccuracy.RunningDeg);
			this.AppendValue("ADS", combinedDisplayInaccuracy.AdsDeg);
			this.AppendValue("Bullet", combinedDisplayInaccuracy.BulletDeg);
			this._text.SetText(this._sb);
		}

		private void AppendValue(string label, float accuracy)
		{
			this._sb.Append(label + ": ");
			for (int i = label.Length; i < 10; i++)
			{
				this._sb.Append(' ');
			}
			this._sb.Append(string.Format("{0:0.000}°\n", accuracy));
		}

		[SerializeField]
		private UiCircle _circle;

		[SerializeField]
		private TMP_Text _text;

		[SerializeField]
		private float _radiusRatio;

		private readonly StringBuilder _sb = new StringBuilder();
	}
}
