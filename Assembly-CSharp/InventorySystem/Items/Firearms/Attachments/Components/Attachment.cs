using System;
using InventorySystem.Items.Autosync;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public abstract class Attachment : FirearmSubcomponentBase
	{
		public abstract AttachmentName Name { get; }

		public abstract AttachmentSlot Slot { get; }

		public abstract float Weight { get; }

		public abstract float Length { get; }

		public abstract AttachmentDescriptiveAdvantages DescriptivePros { get; }

		public abstract AttachmentDescriptiveDownsides DescriptiveCons { get; }

		public byte Index { get; private set; }

		public virtual bool IsEnabled { get; set; }

		private void SetupArrays()
		{
			if (this._arraysInitialized)
			{
				return;
			}
			int totalNumberOfParams = AttachmentsUtils.TotalNumberOfParams;
			this._parameterValues = new float[totalNumberOfParams];
			this._parameterStates = new AttachmentParamState[totalNumberOfParams];
			this._arraysInitialized = true;
		}

		protected override void OnInit()
		{
			base.OnInit();
			SubcomponentBase subcomponentBase;
			if (base.Firearm.AllSubcomponents.TryGet((int)(base.SyncId - 1), out subcomponentBase))
			{
				Attachment attachment = subcomponentBase as Attachment;
				if (attachment != null)
				{
					this.Index = attachment.Index + 1;
					return;
				}
			}
		}

		protected virtual void EnabledEquipUpdate()
		{
		}

		protected void SetParameter(AttachmentParameterValuePair pair)
		{
			this.SetParameter((int)pair.Parameter, pair.Value, pair.UIOnly ? AttachmentParamState.UserInterface : AttachmentParamState.ActiveAndDisplayed);
		}

		protected void SetParameter(int param, float val, AttachmentParamState state)
		{
			this.SetupArrays();
			this._parameterValues[param] = val;
			this._parameterStates[param] = state;
		}

		protected void ClearParameter(AttachmentParam param)
		{
			if (this._arraysInitialized)
			{
				this._parameterStates[(int)param] = AttachmentParamState.Disabled;
			}
		}

		protected void ClearAllParameters()
		{
			if (this._arraysInitialized)
			{
				Array.Clear(this._parameterStates, 0, AttachmentsUtils.TotalNumberOfParams);
			}
		}

		public void GetParameterData(int param, out float val, out AttachmentParamState state)
		{
			this.SetupArrays();
			val = this._parameterValues[param];
			state = this._parameterStates[param];
		}

		public bool TryGetActiveValue(AttachmentParam param, out float val)
		{
			AttachmentParamState attachmentParamState;
			this.GetParameterData((int)param, out val, out attachmentParamState);
			return (attachmentParamState & AttachmentParamState.SilentlyActive) > AttachmentParamState.Disabled;
		}

		public bool TryGetDisplayValue(AttachmentParam param, out float val)
		{
			AttachmentParamState attachmentParamState;
			this.GetParameterData((int)param, out val, out attachmentParamState);
			return (attachmentParamState & AttachmentParamState.UserInterface) > AttachmentParamState.Disabled;
		}

		public void GetNameAndDescription(out string n, out string d)
		{
			string text;
			if (TranslationReader.TryGet("AttachmentNames", (int)this.Name, out text))
			{
				string[] array = text.Split('~', StringSplitOptions.None);
				n = array[0];
				d = ((array.Length == 1) ? string.Empty : array[1]);
				return;
			}
			n = this.Name.ToString();
			d = string.Empty;
		}

		internal sealed override void EquipUpdate()
		{
			base.EquipUpdate();
			if (this.IsEnabled)
			{
				this.EnabledEquipUpdate();
			}
		}

		private AttachmentParamState[] _parameterStates;

		private float[] _parameterValues;

		private bool _arraysInitialized;

		private const string AttNamesFilename = "AttachmentNames";
	}
}
