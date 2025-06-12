using System;
using InventorySystem.Items.Firearms.Extensions;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components;

public class ConditionalSerializableAttachment : SerializableAttachment
{
	[Serializable]
	private struct ConditionalOverride
	{
		public ConditionalEvaluator Condition;

		public AttachmentDescriptiveAdvantages ExtraPros;

		public AttachmentDescriptiveDownsides ExtraCons;

		public AttachmentParameterValuePair[] Params;
	}

	private int? _lastAppliedOverride;

	private bool _initialized;

	private bool _needsRefreshing;

	[SerializeField]
	private ConditionalOverride[] _overrides;

	private ConditionalOverride? LastOverride
	{
		get
		{
			if (!this._lastAppliedOverride.HasValue)
			{
				return null;
			}
			return this._overrides[this._lastAppliedOverride.Value];
		}
	}

	public override AttachmentDescriptiveAdvantages DescriptivePros => this.LastOverride?.ExtraPros ?? base.DescriptivePros;

	public override AttachmentDescriptiveDownsides DescriptiveCons => this.LastOverride?.ExtraCons ?? base.DescriptiveCons;

	protected override void OnInit()
	{
		base.OnInit();
		ConditionalOverride[] overrides = this._overrides;
		for (int i = 0; i < overrides.Length; i++)
		{
			overrides[i].Condition.InitInstance(base.Firearm);
		}
		if (this._needsRefreshing)
		{
			this.RefreshOverrides();
		}
		this._initialized = true;
	}

	internal override void OnAttachmentsApplied()
	{
		base.OnAttachmentsApplied();
		if (this._initialized)
		{
			this.RefreshOverrides();
		}
		else
		{
			this._needsRefreshing = true;
		}
	}

	private void RefreshOverrides()
	{
		for (int i = 0; i < this._overrides.Length; i++)
		{
			if (this._overrides[i].Condition.Evaluate())
			{
				if (this._lastAppliedOverride != i)
				{
					this.ApplyOverride(i);
				}
				return;
			}
		}
		if (this._lastAppliedOverride.HasValue)
		{
			this.ApplyOverride(null);
		}
	}

	private void ApplyOverride(int? index)
	{
		base.ClearAllParameters();
		if (!index.HasValue)
		{
			base.SetDefaultParameters();
		}
		else
		{
			this._overrides[index.Value].Params.ForEach(base.SetParameter);
		}
		this._lastAppliedOverride = index;
	}
}
