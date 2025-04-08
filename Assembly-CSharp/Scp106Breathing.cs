using System;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;

public class Scp106Breathing : StandardSubroutine<Scp106Role>
{
	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp106SinkholeController>(out this._sinkholeController);
	}

	private void Update()
	{
		this._breathingSource.volume = (1f - this._sinkholeController.SubmergeProgress) * ((base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated()) ? 0.5f : 1f);
		this._breathingSource.spatialBlend = ((base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated()) ? 0f : 1f);
	}

	private const float SelfMaxVolume = 0.5f;

	private const float MaxVolume = 1f;

	private Scp106SinkholeController _sinkholeController;

	[SerializeField]
	private AudioSource _breathingSource;
}
