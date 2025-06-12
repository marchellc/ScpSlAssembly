using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public abstract class SubcontrollerBehaviour : MonoBehaviour, INetworkedAnimatedModelSubcontroller, IAnimatedModelSubcontroller
{
	public AnimatedCharacterModel Model { get; private set; }

	public int SubcontrollerIndex { get; private set; }

	public ReferenceHub OwnerHub => this.Model.OwnerHub;

	public Transform ModelTr => this.Model.CachedTransform;

	public Animator Animator => this.Model.Animator;

	protected bool Culled
	{
		get
		{
			if (this.HasCuller)
			{
				return this.Culler.IsCulled;
			}
			return false;
		}
	}

	protected CullingSubcontroller Culler { get; private set; }

	protected bool HasCuller { get; private set; }

	protected bool HasOwner => this.Model.HasOwner;

	public virtual void Init(AnimatedCharacterModel model, int index)
	{
		this.Model = model;
		this.SubcontrollerIndex = index;
		if (this.HasOwner && model.TryGetSubcontroller<CullingSubcontroller>(out var subcontroller))
		{
			this.Culler = subcontroller;
			this.HasCuller = true;
		}
		else
		{
			this.HasCuller = false;
		}
	}

	public virtual void OnReassigned()
	{
	}

	public virtual void ProcessRpc(NetworkReader reader)
	{
	}
}
