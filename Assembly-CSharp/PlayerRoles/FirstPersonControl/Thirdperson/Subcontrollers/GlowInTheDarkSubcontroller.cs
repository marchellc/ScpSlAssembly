using System;
using MapGeneration;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class GlowInTheDarkSubcontroller : SubcontrollerBehaviour
{
	[Serializable]
	private class TargetRenderer
	{
		public Renderer[] Targets;

		[ColorUsage(false, true)]
		public Color DarkenedColor;

		[ColorUsage(false, true)]
		public Color NormalColor;

		public string PropertyName;

		private bool _everSetUp;

		private int _hash;

		private MaterialPropertyBlock _propertyBlock;

		public void Set(bool darkened)
		{
			if (!this._everSetUp)
			{
				this._hash = Shader.PropertyToID(this.PropertyName);
				this._propertyBlock = new MaterialPropertyBlock();
				this.Targets[0].GetPropertyBlock(this._propertyBlock);
				this._everSetUp = true;
			}
			this._propertyBlock.SetColor(this._hash, darkened ? this.DarkenedColor : this.NormalColor);
			Renderer[] targets = this.Targets;
			for (int i = 0; i < targets.Length; i++)
			{
				targets[i].SetPropertyBlock(this._propertyBlock);
			}
		}
	}

	private enum ForceCondition
	{
		NeverForce,
		ForceWhenFriendly,
		ForceWhenEnemy
	}

	private enum ForceResult
	{
		ForceDarkened,
		ForceNormal
	}

	[SerializeField]
	private TargetRenderer[] _targetRenderers;

	[SerializeField]
	private ForceCondition _forceCondition;

	[SerializeField]
	private ForceResult _resultWhenConditionMet;

	private bool? _prevDarkened;

	private bool IsAtBlackedOutRoom
	{
		get
		{
			if (!base.OwnerHub.TryGetLastKnownRoom(out var room))
			{
				return false;
			}
			Vector3 position = base.Model.LastRole.FpcModule.Position;
			return RoomLightController.IsInDarkenedRoom(room, position);
		}
	}

	private bool IsEnemyToPov
	{
		get
		{
			if (ReferenceHub.TryGetPovHub(out var hub))
			{
				return HitboxIdentity.IsEnemy(hub, base.OwnerHub);
			}
			return false;
		}
	}

	private void LateUpdate()
	{
		if ((base.HasCuller && base.Culler.IsCulled) || !base.HasOwner)
		{
			return;
		}
		bool flag;
		switch (this._forceCondition)
		{
		case ForceCondition.ForceWhenFriendly:
		{
			bool conditionMet = !this.IsEnemyToPov;
			flag = this.GetDarkenedConditionally(conditionMet);
			break;
		}
		case ForceCondition.ForceWhenEnemy:
			flag = this.GetDarkenedConditionally(this.IsEnemyToPov);
			break;
		default:
			flag = this.IsAtBlackedOutRoom;
			break;
		}
		if (flag != this._prevDarkened)
		{
			TargetRenderer[] targetRenderers = this._targetRenderers;
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				targetRenderers[i].Set(flag);
			}
			this._prevDarkened = flag;
		}
	}

	private void OnValidate()
	{
		this._prevDarkened = null;
	}

	private bool GetDarkenedConditionally(bool conditionMet)
	{
		if (!conditionMet)
		{
			return this.IsAtBlackedOutRoom;
		}
		return this._resultWhenConditionMet switch
		{
			ForceResult.ForceNormal => false, 
			ForceResult.ForceDarkened => true, 
			_ => this.IsAtBlackedOutRoom, 
		};
	}
}
