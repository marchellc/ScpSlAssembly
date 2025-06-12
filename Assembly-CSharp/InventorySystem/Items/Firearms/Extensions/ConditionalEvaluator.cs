using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;

namespace InventorySystem.Items.Firearms.Extensions;

[Serializable]
public class ConditionalEvaluator
{
	[Serializable]
	public struct AttachmentStatusPair
	{
		public AttachmentLink Link;

		public AttachmentStatus DesiredStatus;
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

	public AttachmentStatusPair[] AttachmentConditions;

	public OtherCondition[] OtherConditions;

	public bool Any;

	private bool _worldmodelMode;

	private bool _hasSetter;

	private AnimatorStateSetterModule _setterModule;

	private Firearm _firearm;

	private FirearmWorldmodel _worldmodel;

	public bool Evaluate()
	{
		bool? flag = this.EvaluateArray(this.AttachmentConditions, EvaluateAttachmentCondition);
		bool? flag2 = this.EvaluateArray(this.OtherConditions, EvaluateModuleCondition);
		if (!flag.HasValue && !flag2.HasValue)
		{
			return true;
		}
		if (this.Any)
		{
			if (flag != true)
			{
				return flag2 == true;
			}
			return true;
		}
		if (flag ?? true)
		{
			return flag2 ?? true;
		}
		return false;
	}

	public void InitInstance(Firearm instance)
	{
		this._firearm = instance;
		this._hasSetter = instance.TryGetModule<AnimatorStateSetterModule>(out this._setterModule);
		AttachmentStatusPair[] attachmentConditions = this.AttachmentConditions;
		for (int i = 0; i < attachmentConditions.Length; i++)
		{
			attachmentConditions[i].Link.InitCache(instance);
		}
	}

	public void InitWorldmodel(FirearmWorldmodel worldmodel)
	{
		this._worldmodel = worldmodel;
		if ((!this._worldmodelMode || !(this._worldmodel == worldmodel)) && InventoryItemLoader.TryGetItem<Firearm>(worldmodel.Identifier.TypeId, out var result))
		{
			AttachmentStatusPair[] attachmentConditions = this.AttachmentConditions;
			for (int i = 0; i < attachmentConditions.Length; i++)
			{
				attachmentConditions[i].Link.InitCache(result);
			}
			this._worldmodelMode = true;
		}
	}

	private bool EvaluateAttachmentCondition(AttachmentStatusPair pair)
	{
		AttachmentLink link = pair.Link;
		return pair.DesiredStatus switch
		{
			AttachmentStatus.Enabled => GetEnabled(), 
			AttachmentStatus.Disabled => !GetEnabled(), 
			AttachmentStatus.EnabledAndReady => GetEnabled() && GetReadyFlag(), 
			AttachmentStatus.EnabledAndNotReady => GetEnabled() && !GetReadyFlag(), 
			_ => false, 
		};
		bool GetEnabled()
		{
			if (!this._worldmodelMode)
			{
				return link.Instance.IsEnabled;
			}
			return (this._worldmodel.AttachmentCode & link.Filter) != 0;
		}
		bool GetReadyFlag()
		{
			if (this._hasSetter)
			{
				return this._setterModule.GetReadyFlag(link.Instance);
			}
			return false;
		}
	}

	private bool EvaluateModuleCondition(OtherCondition cond)
	{
		IEquipperModule module;
		return cond switch
		{
			OtherCondition.Equipped => this._firearm.TryGetModule<IEquipperModule>(out module) && module.IsEquipped, 
			OtherCondition.Pickup => this._worldmodelMode && this._worldmodel.WorldmodelType == FirearmWorldmodelType.Pickup, 
			OtherCondition.Thirdperson => this._worldmodelMode && this._worldmodel.WorldmodelType == FirearmWorldmodelType.Thirdperson, 
			OtherCondition.Firstperson => !this._worldmodelMode && this._firearm.HasViewmodel, 
			_ => false, 
		};
	}

	private bool? EvaluateArray<T>(T[] arr, Predicate<T> evaluator)
	{
		if (arr == null || arr.Length == 0)
		{
			return null;
		}
		foreach (T obj in arr)
		{
			if (evaluator(obj))
			{
				if (this.Any)
				{
					return true;
				}
			}
			else if (!this.Any)
			{
				return false;
			}
		}
		return !this.Any;
	}
}
