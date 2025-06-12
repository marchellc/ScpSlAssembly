using System;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items;

public class SharedHandsController : MonoBehaviour
{
	[Serializable]
	private struct TeamOverride
	{
		public Team Team;

		public HandsVisuals Visuals;
	}

	[Serializable]
	private struct RoleOverride
	{
		public RoleTypeId Role;

		public HandsVisuals Visuals;
	}

	[Serializable]
	private struct HandsVisuals
	{
		public Material Material;

		public Renderer[] Renderers;

		public GameObject[] GameObjects;

		public readonly void Disable()
		{
			GameObject[] gameObjects = this.GameObjects;
			for (int i = 0; i < gameObjects.Length; i++)
			{
				gameObjects[i].SetActive(value: false);
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
				gameObjects[i].SetActive(value: true);
			}
		}
	}

	[Serializable]
	private struct IKWristArmPair
	{
		public Transform Wrist;

		public Transform Forearm;

		public Vector3 WristOffset;

		public Transform MovablePart;

		public readonly void UpdateIK()
		{
			Vector3 position = this.Wrist.position;
			Vector3 vector = this.Forearm.TransformPoint(this.WristOffset) - position;
			Vector3 position2 = this.MovablePart.position;
			if (!float.IsNaN(position2.x))
			{
				this.MovablePart.position = position2 - vector;
			}
		}
	}

	public Animator Hands;

	private Transform _trackedPosition;

	private static bool _eventAssigned;

	private static bool _singletonSet;

	[SerializeField]
	private HandsVisuals _defaultVisuals;

	[SerializeField]
	private TeamOverride[] _teamOverrides;

	[SerializeField]
	private RoleOverride[] _roleOverrides;

	[SerializeField]
	private IKWristArmPair[] _ikWrists;

	private HandsVisuals? _lastSet;

	public static SharedHandsController Singleton { get; private set; }

	public static void UpdateInstance(ItemViewmodelBase ivb)
	{
		if (SharedHandsController._singletonSet)
		{
			if (ivb is AnimatedViewmodelBase animatedViewmodelBase && animatedViewmodelBase != null && !animatedViewmodelBase.DisableSharedHands)
			{
				SharedHandsController.Singleton.Hands.gameObject.SetActive(value: true);
				SharedHandsController.Singleton.Hands.avatar = animatedViewmodelBase.AnimatorAvatar;
				SharedHandsController.Singleton.Hands.runtimeAnimatorController = animatedViewmodelBase.AnimatorRuntimeController;
				SharedHandsController.Singleton._trackedPosition = animatedViewmodelBase.AnimatorTransform;
				SharedHandsController.Singleton.Hands.Rebind();
			}
			else
			{
				SharedHandsController.Singleton.Hands.gameObject.SetActive(value: false);
			}
		}
	}

	private void LateUpdate()
	{
		this.UpdateTrackedPosition();
		IKWristArmPair[] ikWrists = this._ikWrists;
		foreach (IKWristArmPair iKWristArmPair in ikWrists)
		{
			iKWristArmPair.UpdateIK();
		}
	}

	private void UpdateTrackedPosition()
	{
		if (!(this._trackedPosition == null))
		{
			this.Hands.transform.localScale = this._trackedPosition.localScale;
			this.Hands.transform.SetPositionAndRotation(this._trackedPosition.position, this._trackedPosition.rotation);
		}
	}

	private void Awake()
	{
		SharedHandsController.Singleton = this;
		SharedHandsController._singletonSet = true;
		this.Hands.fireEvents = false;
		AnimatedViewmodelBase.OnSwayUpdated += UpdateTrackedPosition;
		if (!SharedHandsController._eventAssigned)
		{
			PlayerRoleManager.OnRoleChanged += RoleChanged;
			SharedHandsController._eventAssigned = true;
		}
	}

	private void OnDestroy()
	{
		AnimatedViewmodelBase.OnSwayUpdated -= UpdateTrackedPosition;
		SharedHandsController._singletonSet = false;
	}

	private static void RoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (hub.isLocalPlayer && !(SharedHandsController.Singleton == null))
		{
			SharedHandsController.SetRoleGloves(newRole.RoleTypeId);
		}
	}

	private void SetVisuals(HandsVisuals visuals)
	{
		this._lastSet?.Disable();
		this._lastSet = visuals;
		visuals.Select();
	}

	private void SetVisuals(RoleTypeId role, Team team)
	{
		this._defaultVisuals.Disable();
		RoleOverride[] roleOverrides = this._roleOverrides;
		for (int i = 0; i < roleOverrides.Length; i++)
		{
			RoleOverride roleOverride = roleOverrides[i];
			if (roleOverride.Role == role)
			{
				this.SetVisuals(roleOverride.Visuals);
				return;
			}
		}
		TeamOverride[] teamOverrides = this._teamOverrides;
		for (int i = 0; i < teamOverrides.Length; i++)
		{
			TeamOverride teamOverride = teamOverrides[i];
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
}
