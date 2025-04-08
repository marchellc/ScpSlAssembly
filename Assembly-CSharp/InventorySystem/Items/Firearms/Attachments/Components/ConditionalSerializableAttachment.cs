using System;
using InventorySystem.Items.Firearms.Extensions;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class ConditionalSerializableAttachment : SerializableAttachment
	{
		private ConditionalSerializableAttachment.ConditionalOverride? LastOverride
		{
			get
			{
				if (this._lastAppliedOverride == null)
				{
					return null;
				}
				return new ConditionalSerializableAttachment.ConditionalOverride?(this._overrides[this._lastAppliedOverride.Value]);
			}
		}

		public override AttachmentDescriptiveAdvantages DescriptivePros
		{
			get
			{
				if (this.LastOverride == null)
				{
					return base.DescriptivePros;
				}
				ConditionalSerializableAttachment.ConditionalOverride? conditionalOverride;
				return conditionalOverride.GetValueOrDefault().ExtraPros;
			}
		}

		public override AttachmentDescriptiveDownsides DescriptiveCons
		{
			get
			{
				if (this.LastOverride == null)
				{
					return base.DescriptiveCons;
				}
				ConditionalSerializableAttachment.ConditionalOverride? conditionalOverride;
				return conditionalOverride.GetValueOrDefault().ExtraCons;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			ConditionalSerializableAttachment.ConditionalOverride[] overrides = this._overrides;
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
				return;
			}
			this._needsRefreshing = true;
		}

		private void RefreshOverrides()
		{
			for (int i = 0; i < this._overrides.Length; i++)
			{
				if (this._overrides[i].Condition.Evaluate())
				{
					int? lastAppliedOverride = this._lastAppliedOverride;
					int num = i;
					if (!((lastAppliedOverride.GetValueOrDefault() == num) & (lastAppliedOverride != null)))
					{
						this.ApplyOverride(new int?(i));
					}
					return;
				}
			}
			if (this._lastAppliedOverride == null)
			{
				return;
			}
			this.ApplyOverride(null);
		}

		private void ApplyOverride(int? index)
		{
			base.ClearAllParameters();
			if (index == null)
			{
				base.SetDefaultParameters();
			}
			else
			{
				this._overrides[index.Value].Params.ForEach(new Action<AttachmentParameterValuePair>(base.SetParameter));
			}
			this._lastAppliedOverride = index;
		}

		private int? _lastAppliedOverride;

		private bool _initialized;

		private bool _needsRefreshing;

		[SerializeField]
		private ConditionalSerializableAttachment.ConditionalOverride[] _overrides;

		[Serializable]
		private struct ConditionalOverride
		{
			public ConditionalEvaluator Condition;

			public AttachmentDescriptiveAdvantages ExtraPros;

			public AttachmentDescriptiveDownsides ExtraCons;

			public AttachmentParameterValuePair[] Params;
		}
	}
}
