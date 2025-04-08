using System;

namespace InventorySystem.Items.Firearms.Attachments
{
	[Serializable]
	public struct AttachmentParameterValuePair
	{
		public AttachmentParameterValuePair(AttachmentParam param, float val, bool uiOnly = false)
		{
			this.Parameter = param;
			this.Value = val;
			this.UIOnly = uiOnly;
		}

		public AttachmentParam Parameter;

		public float Value;

		public bool UIOnly;
	}
}
