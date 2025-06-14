using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public class PackedArmorPart : MonoBehaviour
{
	[SerializeField]
	private WearableArmor.ElementBonePair[] _trackedBones;

	[SerializeField]
	private WearableArmor.VisibleElement[] _visibleObjects;

	[SerializeField]
	private Renderer[] _fadeableRenderers;

	public void Unpack(WearableArmor.ArmorSet target, List<Renderer> fadeable)
	{
		target.TrackedBones.AddRange(this._trackedBones);
		target.VisibleObjects.AddRange(this._visibleObjects);
		fadeable.AddRange(this._fadeableRenderers);
	}
}
