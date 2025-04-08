using System;
using UnityEngine;

namespace InventorySystem.Items.Usables
{
	public class VariableGFXUsableItemThirdperson : UsableItemThirdperson
	{
		private protected GameObject MainGfx { protected get; private set; }

		private protected GameObject LeftHandedGfx { protected get; private set; }

		public override void ResetObject()
		{
			base.ResetObject();
			this.RestoreMainGfx();
		}

		protected override void Update()
		{
			base.Update();
			if (!base.IsUsing || this._alreadyReplaced)
			{
				return;
			}
			this._remainingToReplace -= Time.deltaTime;
			if (this._remainingToReplace < 0f)
			{
				this.ReplaceGfx();
				this._alreadyReplaced = true;
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
			Transform transform = instance.transform;
			transform.SetParent(leftHand);
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			instance.SetActive(true);
		}

		private void RestoreMainGfx()
		{
			this.MainGfx.SetActive(true);
			if (this._alreadyReplaced && this._lastInstance != null)
			{
				this._lastInstance.transform.SetParent(null);
				this._lastInstance.SetActive(false);
			}
		}

		private void ReplaceGfx()
		{
			this.MainGfx.SetActive(false);
			if (this.LeftHandedGfx == null)
			{
				return;
			}
			if (this._lastInstance == null)
			{
				this._lastInstance = global::UnityEngine.Object.Instantiate<GameObject>(this.LeftHandedGfx);
			}
			Transform boneTransform = base.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
			if (boneTransform != null)
			{
				this.SetupLeftHandedInstance(this._lastInstance, boneTransform);
			}
		}

		[SerializeField]
		private float _replacementTime;

		private float _remainingToReplace;

		private bool _alreadyReplaced;

		private GameObject _lastInstance;
	}
}
