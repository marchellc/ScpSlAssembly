using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class SecondaryRigsSubcontroller : SubcontrollerBehaviour
{
	[Serializable]
	public struct SecondaryBone
	{
		public Transform Original;

		public Transform Target;

		public readonly void Match()
		{
			this.Original.GetPositionAndRotation(out var position, out var rotation);
			this.Target.SetPositionAndRotation(position, rotation);
		}
	}

	[SerializeField]
	private SecondaryBone[] _secondaryBones;

	private void LateUpdate()
	{
		if (!base.HasCuller)
		{
			this.MatchAll();
		}
	}

	public void MatchAll()
	{
		SecondaryBone[] secondaryBones = this._secondaryBones;
		foreach (SecondaryBone secondaryBone in secondaryBones)
		{
			secondaryBone.Match();
		}
	}

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		if (base.HasCuller)
		{
			base.Culler.OnAnimatorUpdated += MatchAll;
		}
	}
}
