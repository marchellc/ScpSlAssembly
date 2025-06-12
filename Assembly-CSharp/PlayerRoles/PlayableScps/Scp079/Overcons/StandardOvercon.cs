using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons;

public class StandardOvercon : OverconBase
{
	[SerializeField]
	private AnimationCurve _scaleOverDistance;

	[SerializeField]
	protected SpriteRenderer TargetSprite;

	private const float SurfaceSizeScale = 2.5f;

	public static Color HighlightedColor = new Color(1f, 1f, 1f, 1f);

	public static Color NormalColor = new Color(1f, 1f, 1f, 0.27f);

	public override bool IsHighlighted
	{
		get
		{
			return base.IsHighlighted;
		}
		internal set
		{
			this.TargetSprite.color = (value ? StandardOvercon.HighlightedColor : StandardOvercon.NormalColor);
			base.IsHighlighted = value;
		}
	}

	protected virtual void Awake()
	{
		this.TargetSprite.color = StandardOvercon.HighlightedColor;
	}

	public void Rescale(Scp079Camera cam)
	{
		this.Rescale(cam, Vector3.Distance(cam.Position, base.transform.position));
	}

	public void Rescale(Scp079Camera cam, float dis)
	{
		base.transform.LookAt(cam.Position);
		float num = this._scaleOverDistance.Evaluate(dis);
		if (cam.Room.Zone == FacilityZone.Surface)
		{
			num *= 2.5f;
		}
		base.transform.localScale = Vector3.one * num;
	}
}
