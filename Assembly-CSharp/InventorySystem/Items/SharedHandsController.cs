using System;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items
{
	public class SharedHandsController : MonoBehaviour
	{
		public static SharedHandsController Singleton { get; private set; }

		public static void UpdateInstance(ItemViewmodelBase ivb)
		{
			if (!SharedHandsController._singletonSet)
			{
				return;
			}
			AnimatedViewmodelBase animatedViewmodelBase = ivb as AnimatedViewmodelBase;
			if (animatedViewmodelBase != null && animatedViewmodelBase != null && !animatedViewmodelBase.DisableSharedHands)
			{
				SharedHandsController.Singleton.Hands.gameObject.SetActive(true);
				SharedHandsController.Singleton.Hands.avatar = animatedViewmodelBase.AnimatorAvatar;
				SharedHandsController.Singleton.Hands.runtimeAnimatorController = animatedViewmodelBase.AnimatorRuntimeController;
				SharedHandsController.Singleton._trackedPosition = animatedViewmodelBase.AnimatorTransform;
				SharedHandsController.Singleton.Hands.Rebind();
				return;
			}
			SharedHandsController.Singleton.Hands.gameObject.SetActive(false);
		}

		private void LateUpdate()
		{
			this.UpdateTrackedPosition();
			foreach (SharedHandsController.IKWristArmPair ikwristArmPair in this._ikWrists)
			{
				ikwristArmPair.UpdateIK();
			}
		}

		private void UpdateTrackedPosition()
		{
			if (this._trackedPosition == null)
			{
				return;
			}
			this.Hands.transform.localScale = this._trackedPosition.localScale;
			this.Hands.transform.SetPositionAndRotation(this._trackedPosition.position, this._trackedPosition.rotation);
		}

		private void Awake()
		{
			SharedHandsController.Singleton = this;
			SharedHandsController._singletonSet = true;
			this.Hands.fireEvents = false;
			AnimatedViewmodelBase.OnSwayUpdated += this.UpdateTrackedPosition;
			if (!SharedHandsController._eventAssigned)
			{
				PlayerRoleManager.OnRoleChanged += SharedHandsController.RoleChanged;
				SharedHandsController._eventAssigned = true;
			}
		}

		private void OnDestroy()
		{
			AnimatedViewmodelBase.OnSwayUpdated -= this.UpdateTrackedPosition;
			SharedHandsController._singletonSet = false;
		}

		private static void RoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			if (!hub.isLocalPlayer || SharedHandsController.Singleton == null)
			{
				return;
			}
			SharedHandsController.SetRoleGloves(newRole.RoleTypeId);
		}

		private void SetVisuals(SharedHandsController.HandsVisuals visuals)
		{
			if (this._lastSet != null)
			{
				this._lastSet.GetValueOrDefault().Disable();
			}
			this._lastSet = new SharedHandsController.HandsVisuals?(visuals);
			visuals.Select();
		}

		private void SetVisuals(RoleTypeId role, Team team)
		{
			this._defaultVisuals.Disable();
			foreach (SharedHandsController.RoleOverride roleOverride in this._roleOverrides)
			{
				if (roleOverride.Role == role)
				{
					this.SetVisuals(roleOverride.Visuals);
					return;
				}
			}
			foreach (SharedHandsController.TeamOverride teamOverride in this._teamOverrides)
			{
				if (teamOverride.Team == team)
				{
					this.SetVisuals(teamOverride.Visuals);
					return;
				}
			}
			this.SetVisuals(this._defaultVisuals);
		}

		public static void SetRoleGloves(RoleTypeId id)
		{
			SharedHandsController.Singleton.SetVisuals(id, id.GetTeam());
		}

		public Animator Hands;

		private Transform _trackedPosition;

		private static bool _eventAssigned;

		private static bool _singletonSet;

		[SerializeField]
		private SharedHandsController.HandsVisuals _defaultVisuals;

		[SerializeField]
		private SharedHandsController.TeamOverride[] _teamOverrides;

		[SerializeField]
		private SharedHandsController.RoleOverride[] _roleOverrides;

		[SerializeField]
		private SharedHandsController.IKWristArmPair[] _ikWrists;

		private SharedHandsController.HandsVisuals? _lastSet;

		[Serializable]
		private struct TeamOverride
		{
			public Team Team;

			public SharedHandsController.HandsVisuals Visuals;
		}

		[Serializable]
		private struct RoleOverride
		{
			public RoleTypeId Role;

			public SharedHandsController.HandsVisuals Visuals;
		}

		[Serializable]
		private struct HandsVisuals
		{
			public readonly void Disable()
			{
				GameObject[] gameObjects = this.GameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					gameObjects[i].SetActive(false);
				}
			}

			public readonly void Select()
			{
				Renderer[] renderers = this.Renderers;
				for (int i = 0; i < renderers.Length; i++)
				{
					renderers[i].sharedMaterial = this.Material;
				}
				GameObject[] gameObjects = this.GameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					gameObjects[i].SetActive(true);
				}
			}

			public Material Material;

			public Renderer[] Renderers;

			public GameObject[] GameObjects;
		}

		[Serializable]
		private struct IKWristArmPair
		{
			public readonly void UpdateIK()
			{
				Vector3 position = this.Wrist.position;
				Vector3 vector = this.Forearm.TransformPoint(this.WristOffset) - position;
				Vector3 position2 = this.MovablePart.position;
				if (float.IsNaN(position2.x))
				{
					return;
				}
				this.MovablePart.position = position2 - vector;
			}

			public Transform Wrist;

			public Transform Forearm;

			public Vector3 WristOffset;

			public Transform MovablePart;
		}
	}
}
