using System;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class WearableSubcontroller : SubcontrollerBehaviour, IPoolResettable
	{
		public void ResetObject()
		{
			this._syncWearable = WearableElements.None;
			this.SetWearables(WearableElements.None);
		}

		public override void OnReassigned()
		{
			base.OnReassigned();
			this._syncWearable = WearableSync.GetWearables(base.OwnerHub);
			if (base.HasCuller && !base.Culler.IsCulled)
			{
				this.SetWearables(this._syncWearable);
			}
		}

		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			model.OnFadeChanged += this.OnFadeChanged;
			model.OnVisibilityChanged += this.OnVisibilityChanged;
			if (base.HasCuller)
			{
				base.Culler.OnCullChanged += this.OnCulllChanged;
			}
			WearableSubcontroller.DisplayableWearable[] wearables = this._wearables;
			for (int i = 0; i < wearables.Length; i++)
			{
				wearables[i].Awake(base.Animator);
			}
		}

		private void OnVisibilityChanged()
		{
			this.SetWearables(this._syncWearable);
		}

		private void OnFadeChanged()
		{
			float fade = base.Model.Fade;
			WearableSubcontroller.DisplayableWearable[] wearables = this._wearables;
			for (int i = 0; i < wearables.Length; i++)
			{
				wearables[i].SetScale(fade);
			}
		}

		private void OnCulllChanged()
		{
			if (base.Culler.IsCulled)
			{
				this.SetWearables(WearableElements.None);
				return;
			}
			this.SetWearables(this._syncWearable);
		}

		private void SetWearables(WearableElements elements)
		{
			foreach (WearableSubcontroller.DisplayableWearable displayableWearable in this._wearables)
			{
				bool flag = (displayableWearable.Item & elements) > WearableElements.None;
				displayableWearable.TargetObject.SetActive(flag && base.Model.IsVisible);
			}
		}

		public void ClientReceiveWearables(WearableElements sync)
		{
			this._syncWearable = sync;
			this.SetWearables(sync);
		}

		public bool TryGetWearable(WearableElements wearable, out WearableSubcontroller.DisplayableWearable ret)
		{
			foreach (WearableSubcontroller.DisplayableWearable displayableWearable in this._wearables)
			{
				if (displayableWearable.Item == wearable)
				{
					ret = displayableWearable;
					return true;
				}
			}
			ret = null;
			return false;
		}

		[SerializeField]
		private WearableSubcontroller.DisplayableWearable[] _wearables;

		private WearableElements _syncWearable;

		[Serializable]
		public class DisplayableWearable
		{
			public WearableElements Item { get; private set; }

			public GameObject TargetObject { get; private set; }

			public HumanBodyBones ParentBone { get; private set; }

			public Transform TargetTransform { get; private set; }

			public Vector3 GlobalScale { get; private set; }

			public void SetScale(float scale)
			{
				this.TargetTransform.localScale = this._parentOriginalScale * scale;
			}

			public void Awake(Animator anim)
			{
				this.TargetTransform = this.TargetObject.transform;
				this.GlobalScale = this.TargetTransform.lossyScale;
				Transform boneTransform = anim.GetBoneTransform(this.ParentBone);
				this.TargetTransform.SetParent(boneTransform);
				this._parentOriginalScale = this.TargetTransform.localScale;
			}

			private Vector3 _parentOriginalScale;
		}
	}
}
