using System;
using System.Diagnostics;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson
{
	[CreateAssetMenu(fileName = "New Model Settings", menuName = "ScriptableObject/Role Management/Model Shared Settings")]
	public class ModelSharedSettings : ScriptableObject
	{
		public Vector3 GetHeadBob(float time, Vector2 animDirection)
		{
			float magnitude = animDirection.magnitude;
			if (magnitude > 1f)
			{
				animDirection /= magnitude;
			}
			Vector3 vector = new Vector3(0f, this._landingAnimation.Evaluate((float)this._landingSw.Elapsed.TotalSeconds), 0f);
			Vector3 vector2 = new Vector3(this._walkBobHorizontal.Evaluate(time), this._walkBobVertical.Evaluate(time), 0f);
			Vector3 vector3 = new Vector3(this._strafeBobHorizontal.Evaluate(time), this._strafeBobVertical.Evaluate(time), 0f);
			return (vector2 * Mathf.Abs(animDirection.y) + vector3 * Mathf.Abs(animDirection.x)) * this._bobScaleOverParams.Evaluate(magnitude) + vector;
		}

		public void PlayLandingAnimation()
		{
			this._landingSw.Restart();
		}

		public AudioClip FallDamageSound;

		[SerializeField]
		private AnimationCurve _walkBobHorizontal;

		[SerializeField]
		private AnimationCurve _walkBobVertical;

		[SerializeField]
		private AnimationCurve _strafeBobHorizontal;

		[SerializeField]
		private AnimationCurve _strafeBobVertical;

		[SerializeField]
		private AnimationCurve _bobScaleOverParams;

		[SerializeField]
		private AnimationCurve _landingAnimation;

		private readonly Stopwatch _landingSw = new Stopwatch();
	}
}
