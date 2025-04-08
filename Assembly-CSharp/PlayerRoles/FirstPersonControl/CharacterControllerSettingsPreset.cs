using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	[CreateAssetMenu(fileName = "New CharacterController Setting Preset", menuName = "ScriptableObject/Player Roles/Character Controller Preset")]
	public class CharacterControllerSettingsPreset : ScriptableObject
	{
		public void Apply(CharacterController charCtrl)
		{
			charCtrl.slopeLimit = this.SlopeLimit;
			charCtrl.stepOffset = this.StepOffset;
			charCtrl.skinWidth = this.SkinWidth;
			charCtrl.minMoveDistance = this.MinMoveDistance;
			charCtrl.center = this.Center;
			charCtrl.radius = this.Radius;
			charCtrl.height = this.Height;
		}

		public float SlopeLimit = 45f;

		public float StepOffset = 0.3f;

		public float SkinWidth = 0.08f;

		public float MinMoveDistance = 0.001f;

		public Vector3 Center = Vector3.zero;

		public float Radius = 0.5f;

		public float Height = 2.5f;
	}
}
