using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp079.Map;

public class ZoneBlackoutIcon : Scp079GuiElementBase
{
	private static readonly HashSet<ZoneBlackoutIcon> Instances = new HashSet<ZoneBlackoutIcon>();

	[SerializeField]
	private RectTransform _root;

	[SerializeField]
	private float _triggerDis;

	[SerializeField]
	private FacilityZone _zone;

	[SerializeField]
	private Graphic _recolorable;

	[SerializeField]
	private Color _defaultColor;

	private RectTransform _rt;

	private Scp079TierManager _tier;

	private Scp079BlackoutZoneAbility _ability;

	public static FacilityZone HighlightedZone
	{
		get
		{
			foreach (ZoneBlackoutIcon instance in ZoneBlackoutIcon.Instances)
			{
				if (instance.Highlighted)
				{
					return instance._zone;
				}
			}
			return FacilityZone.None;
		}
	}

	private bool Highlighted
	{
		get
		{
			bool flag = Vector3.Distance(this._rt.position, this._root.position) < this._triggerDis * this._root.localScale.x;
			this._recolorable.color = (flag ? Color.white : this._defaultColor);
			return flag;
		}
	}

	internal override void Init(Scp079Role role, ReferenceHub owner)
	{
		base.Init(role, owner);
		base.Role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out this._tier);
		base.Role.SubroutineModule.TryGetSubroutine<Scp079BlackoutZoneAbility>(out this._ability);
		Scp079TierManager tier = this._tier;
		tier.OnLevelledUp = (Action)Delegate.Combine(tier.OnLevelledUp, new Action(RefreshVisibiltiy));
		ZoneBlackoutIcon.Instances.Add(this);
		this._rt = base.GetComponent<RectTransform>();
		this.RefreshVisibiltiy();
	}

	private void OnDestroy()
	{
		ZoneBlackoutIcon.Instances.Remove(this);
		Scp079TierManager tier = this._tier;
		tier.OnLevelledUp = (Action)Delegate.Remove(tier.OnLevelledUp, new Action(RefreshVisibiltiy));
	}

	private void RefreshVisibiltiy()
	{
		base.gameObject.SetActive(this._ability.Unlocked);
	}
}
