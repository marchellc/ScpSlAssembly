using System;
using System.Collections.Generic;
using CustomRendering;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class DisruptorScopeTrackerExtension : MonoBehaviour, IViewmodelExtension
	{
		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._owner = viewmodel.ParentFirearm.Owner;
			viewmodel.ParentFirearm.TryGetModule(out this._adsModule, true);
		}

		private void LateUpdate()
		{
			if (!this._mainCamera.gameObject.activeInHierarchy)
			{
				foreach (DisruptorScopeTrackerExtension.TargetIndicator targetIndicator in this._activeIndicators)
				{
					targetIndicator.GameObject.SetActive(false);
					this._indicatorPool.Enqueue(targetIndicator);
				}
				this._activeIndicators.Clear();
				return;
			}
			Vector3 vector;
			Quaternion quaternion;
			this._mainCamera.transform.GetPositionAndRotation(out vector, out quaternion);
			Vector3 vector2 = quaternion * Vector3.forward;
			float num = FogController.FogFarPlaneDistance + this._extraFogRange;
			float num2 = num * num;
			this._detectedNetIds.Clear();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null && !(referenceHub == this._owner))
				{
					Vector3 vector3 = fpcRole.FpcModule.Position - vector;
					Vector3 vector4;
					if (vector3.sqrMagnitude <= num2 && Vector3.Dot(vector2, vector3) >= 0f && this.TryGetBestPos(vector, referenceHub.netId, fpcRole.FpcModule, out vector4))
					{
						Vector3 vector5 = this._mainCamera.WorldToViewportPoint(vector4);
						if (vector5.x >= 0f && vector5.x <= 1f && vector5.y >= 0f && vector5.y <= 1f)
						{
							this._detectedNetIds.Add(referenceHub.netId);
							this.UpdateIndicator(vector5, referenceHub);
						}
					}
				}
			}
			this.RePoolUnusedIndicators();
		}

		private void UpdateIndicator(Vector3 viewportPos, ReferenceHub target)
		{
			float num = (HitboxIdentity.IsEnemy(this._owner, target) ? this._enemyOpacity : this._friendlyOpacity);
			num *= this._opacityOverFog.Evaluate(viewportPos.z / Mathf.Max(this._extraFogRange, FogController.FogFarPlaneDistance + this._extraFogRange));
			num *= this._opacityOverDistance.Evaluate(viewportPos.z);
			num *= this._adsModule.AdsAmount;
			Color roleColor = target.roleManager.CurrentRole.RoleColor;
			DisruptorScopeTrackerExtension.TargetIndicator indicatorForPlayer = this.GetIndicatorForPlayer(target.netId);
			indicatorForPlayer.Image.color = new Color(roleColor.r, roleColor.g, roleColor.b, num);
			indicatorForPlayer.Transform.anchoredPosition = Vector2.Scale(viewportPos, this._canvasScale);
			indicatorForPlayer.Transform.localScale = Vector3.one * this._sizeOverDistance.Evaluate(viewportPos.z);
		}

		private DisruptorScopeTrackerExtension.TargetIndicator GetIndicatorForPlayer(uint netId)
		{
			foreach (DisruptorScopeTrackerExtension.TargetIndicator targetIndicator in this._activeIndicators)
			{
				if (targetIndicator.NetId == netId)
				{
					return targetIndicator;
				}
			}
			DisruptorScopeTrackerExtension.TargetIndicator targetIndicator2;
			DisruptorScopeTrackerExtension.TargetIndicator targetIndicator3;
			if (this._indicatorPool.TryDequeue(out targetIndicator2))
			{
				targetIndicator3 = new DisruptorScopeTrackerExtension.TargetIndicator(targetIndicator2, netId);
			}
			else
			{
				RectTransform rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(this._targetIndicatorTemplate, this._targetIndicatorTemplate.parent);
				targetIndicator3 = new DisruptorScopeTrackerExtension.TargetIndicator(rectTransform, netId);
			}
			this._activeIndicators.Add(targetIndicator3);
			targetIndicator3.GameObject.SetActive(true);
			return targetIndicator3;
		}

		private void RePoolUnusedIndicators()
		{
			for (int i = this._activeIndicators.Count - 1; i >= 0; i--)
			{
				DisruptorScopeTrackerExtension.TargetIndicator targetIndicator = this._activeIndicators[i];
				if (!this._detectedNetIds.Contains(targetIndicator.NetId))
				{
					targetIndicator.GameObject.SetActive(false);
					this._indicatorPool.Enqueue(targetIndicator);
					this._activeIndicators.RemoveAt(i);
				}
			}
		}

		private bool TryGetBestPos(Vector3 mainCamPos, uint netId, FirstPersonMovementModule fpc, out Vector3 pos)
		{
			pos = fpc.Position;
			if (fpc.CharacterModelInstance == null)
			{
				return false;
			}
			Vector3? vector = null;
			float num = float.MaxValue;
			HitboxIdentity[] hitboxes = fpc.CharacterModelInstance.Hitboxes;
			for (int i = 0; i < hitboxes.Length; i++)
			{
				Vector3 centerOfMass = hitboxes[i].CenterOfMass;
				float sqrMagnitude = (centerOfMass - pos).sqrMagnitude;
				if (sqrMagnitude <= num && this.CheckLineOfSight(mainCamPos, centerOfMass, netId))
				{
					vector = new Vector3?(centerOfMass);
					num = sqrMagnitude;
				}
			}
			if (vector == null)
			{
				return this.CheckLineOfSight(mainCamPos, pos, netId);
			}
			pos = vector.Value;
			return true;
		}

		private bool CheckLineOfSight(Vector3 origin, Vector3 pos, uint netId)
		{
			RaycastHit raycastHit;
			HitboxIdentity hitboxIdentity;
			return !Physics.Linecast(origin, pos, DisruptorScopeTrackerExtension.LineOfSightBlockMask) && Physics.Linecast(origin, pos, out raycastHit, DisruptorScopeTrackerExtension.HitboxMask) && raycastHit.collider.TryGetComponent<HitboxIdentity>(out hitboxIdentity) && hitboxIdentity.NetworkId == netId;
		}

		private static readonly CachedLayerMask LineOfSightBlockMask = new CachedLayerMask(new string[] { "Default", "Door" });

		private static readonly CachedLayerMask HitboxMask = new CachedLayerMask(new string[] { "Hitbox" });

		[SerializeField]
		private Camera _mainCamera;

		[SerializeField]
		private AnimationCurve _opacityOverFog;

		[SerializeField]
		private AnimationCurve _sizeOverDistance;

		[SerializeField]
		private AnimationCurve _opacityOverDistance;

		[SerializeField]
		private float _friendlyOpacity;

		[SerializeField]
		private float _enemyOpacity;

		[SerializeField]
		private RectTransform _targetIndicatorTemplate;

		[SerializeField]
		private Vector2 _canvasScale;

		[SerializeField]
		private float _extraFogRange;

		private ReferenceHub _owner;

		private IAdsModule _adsModule;

		private readonly List<DisruptorScopeTrackerExtension.TargetIndicator> _activeIndicators = new List<DisruptorScopeTrackerExtension.TargetIndicator>();

		private readonly Queue<DisruptorScopeTrackerExtension.TargetIndicator> _indicatorPool = new Queue<DisruptorScopeTrackerExtension.TargetIndicator>();

		private readonly HashSet<uint> _detectedNetIds = new HashSet<uint>();

		private struct TargetIndicator
		{
			public readonly float ActiveElapsed
			{
				get
				{
					return (float)(NetworkTime.time - this.CreationTime);
				}
			}

			public TargetIndicator(RectTransform newInstance, uint netId)
			{
				this.Transform = newInstance;
				this.GameObject = this.Transform.gameObject;
				this.Image = this.Transform.GetComponent<Image>();
				this.NetId = netId;
				this.CreationTime = NetworkTime.time;
			}

			public TargetIndicator(DisruptorScopeTrackerExtension.TargetIndicator other, uint newNetId)
			{
				this.GameObject = other.GameObject;
				this.Transform = other.Transform;
				this.Image = other.Image;
				this.NetId = newNetId;
				this.CreationTime = NetworkTime.time;
			}

			public GameObject GameObject;

			public RectTransform Transform;

			public Image Image;

			public uint NetId;

			public double CreationTime;
		}
	}
}
