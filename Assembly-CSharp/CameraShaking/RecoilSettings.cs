using System;

namespace CameraShaking;

[Serializable]
public struct RecoilSettings
{
	public float AnimationTime;

	public float ZAxis;

	public float FovKick;

	public float UpKick;

	public float SideKick;

	public RecoilSettings(float animationTime, float zAxis, float fovKick, float upKick, float sideKick)
	{
		this.AnimationTime = animationTime;
		this.ZAxis = zAxis;
		this.FovKick = fovKick;
		this.UpKick = upKick;
		this.SideKick = sideKick;
	}
}
