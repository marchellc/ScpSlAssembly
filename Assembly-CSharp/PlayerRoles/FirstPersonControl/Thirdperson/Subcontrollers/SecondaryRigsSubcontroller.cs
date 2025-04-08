using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class SecondaryRigsSubcontroller : SubcontrollerBehaviour
	{
		private void LateUpdate()
		{
			if (base.HasCuller)
			{
				return;
			}
			this.MatchAll();
		}

		private void MatchAll()
		{
			foreach (SecondaryRigsSubcontroller.SecondaryBone secondaryBone in this._secondaryBones)
			{
				secondaryBone.Match();
			}
		}

		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			if (base.HasCuller)
			{
				base.Culler.OnAnimatorUpdated += this.MatchAll;
			}
		}

		[SerializeField]
		private SecondaryRigsSubcontroller.SecondaryBone[] _secondaryBones;

		[Serializable]
		public struct SecondaryBone
		{
			public readonly void Match()
			{
				this.Target.SetPositionAndRotation(this.Original.position, this.Original.rotation);
			}

			public Transform Original;

			public Transform Target;
		}
	}
}
