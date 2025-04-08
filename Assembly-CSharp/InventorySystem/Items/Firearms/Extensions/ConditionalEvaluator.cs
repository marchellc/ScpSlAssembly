using System;
using System.Runtime.CompilerServices;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;

namespace InventorySystem.Items.Firearms.Extensions
{
	[Serializable]
	public class ConditionalEvaluator
	{
		public bool Evaluate()
		{
			bool? flag = this.EvaluateArray<ConditionalEvaluator.AttachmentStatusPair>(this.AttachmentConditions, new Predicate<ConditionalEvaluator.AttachmentStatusPair>(this.EvaluateAttachmentCondition));
			bool? flag2 = this.EvaluateArray<ConditionalEvaluator.OtherCondition>(this.OtherConditions, new Predicate<ConditionalEvaluator.OtherCondition>(this.EvaluateModuleCondition));
			if (flag == null && flag2 == null)
			{
				return true;
			}
			if (this.Any)
			{
				return flag.GetValueOrDefault() || flag2.GetValueOrDefault();
			}
			return (flag ?? true) && (flag2 ?? true);
		}

		public void InitInstance(Firearm instance)
		{
			this._firearm = instance;
			this._hasSetter = instance.TryGetModule(out this._setterModule, true);
			ConditionalEvaluator.AttachmentStatusPair[] attachmentConditions = this.AttachmentConditions;
			for (int i = 0; i < attachmentConditions.Length; i++)
			{
				attachmentConditions[i].Link.InitCache(instance);
			}
		}

		public void InitWorldmodel(FirearmWorldmodel worldmodel)
		{
			this._worldmodel = worldmodel;
			if (this._worldmodelMode && this._worldmodel == worldmodel)
			{
				return;
			}
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(worldmodel.Identifier.TypeId, out firearm))
			{
				return;
			}
			ConditionalEvaluator.AttachmentStatusPair[] attachmentConditions = this.AttachmentConditions;
			for (int i = 0; i < attachmentConditions.Length; i++)
			{
				attachmentConditions[i].Link.InitCache(firearm);
			}
			this._worldmodelMode = true;
		}

		private bool EvaluateAttachmentCondition(ConditionalEvaluator.AttachmentStatusPair pair)
		{
			ConditionalEvaluator.<>c__DisplayClass11_0 CS$<>8__locals1;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.link = pair.Link;
			bool flag;
			switch (pair.DesiredStatus)
			{
			case ConditionalEvaluator.AttachmentStatus.Disabled:
				flag = !this.<EvaluateAttachmentCondition>g__GetEnabled|11_0(ref CS$<>8__locals1);
				break;
			case ConditionalEvaluator.AttachmentStatus.Enabled:
				flag = this.<EvaluateAttachmentCondition>g__GetEnabled|11_0(ref CS$<>8__locals1);
				break;
			case ConditionalEvaluator.AttachmentStatus.EnabledAndReady:
				flag = this.<EvaluateAttachmentCondition>g__GetEnabled|11_0(ref CS$<>8__locals1) && this.<EvaluateAttachmentCondition>g__GetReadyFlag|11_1(ref CS$<>8__locals1);
				break;
			case ConditionalEvaluator.AttachmentStatus.EnabledAndNotReady:
				flag = this.<EvaluateAttachmentCondition>g__GetEnabled|11_0(ref CS$<>8__locals1) && !this.<EvaluateAttachmentCondition>g__GetReadyFlag|11_1(ref CS$<>8__locals1);
				break;
			default:
				flag = false;
				break;
			}
			return flag;
		}

		private bool EvaluateModuleCondition(ConditionalEvaluator.OtherCondition cond)
		{
			bool flag;
			switch (cond)
			{
			case ConditionalEvaluator.OtherCondition.Equipped:
			{
				IEquipperModule equipperModule;
				flag = this._firearm.TryGetModule(out equipperModule, true) && equipperModule.IsEquipped;
				break;
			}
			case ConditionalEvaluator.OtherCondition.Firstperson:
				flag = !this._worldmodelMode && this._firearm.HasViewmodel;
				break;
			case ConditionalEvaluator.OtherCondition.Pickup:
				flag = this._worldmodelMode && this._worldmodel.WorldmodelType == FirearmWorldmodelType.Pickup;
				break;
			case ConditionalEvaluator.OtherCondition.Thirdperson:
				flag = this._worldmodelMode && this._worldmodel.WorldmodelType == FirearmWorldmodelType.Thirdperson;
				break;
			default:
				flag = false;
				break;
			}
			return flag;
		}

		private bool? EvaluateArray<T>(T[] arr, Predicate<T> evaluator)
		{
			if (arr == null || arr.Length == 0)
			{
				return null;
			}
			foreach (T t in arr)
			{
				if (evaluator(t))
				{
					if (this.Any)
					{
						return new bool?(true);
					}
				}
				else if (!this.Any)
				{
					return new bool?(false);
				}
			}
			return new bool?(!this.Any);
		}

		[CompilerGenerated]
		private bool <EvaluateAttachmentCondition>g__GetEnabled|11_0(ref ConditionalEvaluator.<>c__DisplayClass11_0 A_1)
		{
			if (!this._worldmodelMode)
			{
				return A_1.link.Instance.IsEnabled;
			}
			return (this._worldmodel.AttachmentCode & A_1.link.Filter) > 0U;
		}

		[CompilerGenerated]
		private bool <EvaluateAttachmentCondition>g__GetReadyFlag|11_1(ref ConditionalEvaluator.<>c__DisplayClass11_0 A_1)
		{
			return this._hasSetter && this._setterModule.GetReadyFlag(A_1.link.Instance);
		}

		public ConditionalEvaluator.AttachmentStatusPair[] AttachmentConditions;

		public ConditionalEvaluator.OtherCondition[] OtherConditions;

		public bool Any;

		private bool _worldmodelMode;

		private bool _hasSetter;

		private AnimatorStateSetterModule _setterModule;

		private Firearm _firearm;

		private FirearmWorldmodel _worldmodel;

		[Serializable]
		public struct AttachmentStatusPair
		{
			public AttachmentLink Link;

			public ConditionalEvaluator.AttachmentStatus DesiredStatus;
		}

		public enum AttachmentStatus
		{
			Disabled,
			Enabled,
			EnabledAndReady,
			EnabledAndNotReady
		}

		public enum OtherCondition
		{
			Equipped,
			Firstperson,
			Pickup,
			Thirdperson
		}
	}
}
