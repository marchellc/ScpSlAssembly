using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public abstract class SubcontrollerBehaviour : MonoBehaviour, INetworkedAnimatedModelSubcontroller, IAnimatedModelSubcontroller
	{
		public AnimatedCharacterModel Model { get; private set; }

		public int SubcontrollerIndex { get; private set; }

		public ReferenceHub OwnerHub
		{
			get
			{
				return this.Model.OwnerHub;
			}
		}

		public Transform ModelTr
		{
			get
			{
				return this.Model.CachedTransform;
			}
		}

		public Animator Animator
		{
			get
			{
				return this.Model.Animator;
			}
		}

		protected bool Culled
		{
			get
			{
				return this.HasCuller && this.Culler.IsCulled;
			}
		}

		private protected CullingSubcontroller Culler { protected get; private set; }

		private protected bool HasCuller { protected get; private set; }

		protected bool ThreadmillEnabled
		{
			get
			{
				return this.Model.ThreadmillEnabled;
			}
		}

		public virtual void Init(AnimatedCharacterModel model, int index)
		{
			this.Model = model;
			this.SubcontrollerIndex = index;
			CullingSubcontroller cullingSubcontroller;
			if (model.TryGetSubcontroller<CullingSubcontroller>(out cullingSubcontroller))
			{
				this.Culler = cullingSubcontroller;
				this.HasCuller = true;
				return;
			}
			this.HasCuller = false;
		}

		public virtual void OnReassigned()
		{
		}

		public virtual void ProcessRpc(NetworkReader reader)
		{
		}
	}
}
