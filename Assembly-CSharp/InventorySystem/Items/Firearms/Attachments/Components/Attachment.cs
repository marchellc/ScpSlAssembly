using System;

namespace InventorySystem.Items.Firearms.Attachments.Components;

public abstract class Attachment : FirearmSubcomponentBase
{
	private AttachmentParamState[] _parameterStates;

	private float[] _parameterValues;

	private bool _arraysInitialized;

	private const string AttNamesFilename = "AttachmentNames";

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
		if (!this._arraysInitialized)
		{
			int totalNumberOfParams = AttachmentsUtils.TotalNumberOfParams;
			this._parameterValues = new float[totalNumberOfParams];
			this._parameterStates = new AttachmentParamState[totalNumberOfParams];
			this._arraysInitialized = true;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		if (base.Firearm.AllSubcomponents.TryGet(base.SyncId - 1, out var element) && element is Attachment attachment)
		{
			this.Index = (byte)(attachment.Index + 1);
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
		this.GetParameterData((int)param, out val, out var state);
		return (state & AttachmentParamState.SilentlyActive) != 0;
	}

	public bool TryGetDisplayValue(AttachmentParam param, out float val)
	{
		this.GetParameterData((int)param, out val, out var state);
		return (state & AttachmentParamState.UserInterface) != 0;
	}

	public void GetNameAndDescription(out string n, out string d)
	{
		if (TranslationReader.TryGet("AttachmentNames", (int)this.Name, out var val))
		{
			string[] array = val.Split('~');
			n = array[0];
			d = ((array.Length == 1) ? string.Empty : array[1]);
		}
		else
		{
			n = this.Name.ToString();
			d = string.Empty;
		}
	}

	internal sealed override void EquipUpdate()
	{
		base.EquipUpdate();
		if (this.IsEnabled)
		{
			this.EnabledEquipUpdate();
		}
	}
}
