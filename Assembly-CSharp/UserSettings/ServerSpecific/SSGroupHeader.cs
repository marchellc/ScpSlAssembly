using System;

namespace UserSettings.ServerSpecific
{
	public class SSGroupHeader : ServerSpecificSettingBase
	{
		public override ServerSpecificSettingBase.UserResponseMode ResponseMode
		{
			get
			{
				return ServerSpecificSettingBase.UserResponseMode.None;
			}
		}

		public bool ReducedPadding { get; private set; }

		public override string DebugValue
		{
			get
			{
				return "N/A";
			}
		}

		public SSGroupHeader(string label, bool reducedPadding = false, string hint = null)
		{
			base.Label = label;
			base.HintDescription = hint;
			this.ReducedPadding = reducedPadding;
		}

		public override void ApplyDefaultValues()
		{
		}
	}
}
