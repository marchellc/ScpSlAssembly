using UnityEngine;

namespace InventorySystem.Items.Usables;

public class VariableGFXUsableItemThirdperson : UsableItemThirdperson
{
	[SerializeField]
	private float _replacementTime;

	private float _remainingToReplace;

	private bool _alreadyReplaced;

	private GameObject _lastInstance;

	[field: SerializeField]
	protected GameObject MainGfx { get; private set; }

	[field: SerializeField]
	protected GameObject LeftHandedGfx { get; private set; }

	public override void ResetObject()
	{
		base.ResetObject();
		this.RestoreMainGfx();
	}

	protected override void Update()
	{
		base.Update();
		if (base.IsUsing && !this._alreadyReplaced)
		{
			this._remainingToReplace -= Time.deltaTime;
			if (this._remainingToReplace < 0f)
			{
				this.ReplaceGfx();
				this._alreadyReplaced = true;
			}
		}
	}

	protected override void OnUsingStatusChanged()
	{
		base.OnUsingStatusChanged();
		if (base.IsUsing)
		{
			this._remainingToReplace = this._replacementTime;
		}
		else
		{
			this.RestoreMainGfx();
		}
		this._alreadyReplaced = false;
	}

	protected virtual void SetupLeftHandedInstance(GameObject instance, Transform leftHand)
	{
		Transform obj = instance.transform;
		obj.SetParent(leftHand);
		obj.ResetLocalPose();
		instance.SetActive(value: true);
	}

	private void RestoreMainGfx()
	{
		this.MainGfx.SetActive(value: true);
		if (this._alreadyReplaced && this._lastInstance != null)
		{
			this._lastInstance.transform.SetParent(null);
			this._lastInstance.SetActive(value: false);
		}
	}

	private void ReplaceGfx()
	{
		this.MainGfx.SetActive(value: false);
		if (!(this.LeftHandedGfx == null))
		{
			if (this._lastInstance == null)
			{
				this._lastInstance = Object.Instantiate(this.LeftHandedGfx);
			}
			Transform boneTransform = base.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
			if (boneTransform != null)
			{
				this.SetupLeftHandedInstance(this._lastInstance, boneTransform);
			}
		}
	}
}
