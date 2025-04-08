using System;
using Interactables.Verification;
using UnityEngine;

namespace Interactables.Interobjects
{
	public abstract class PopupInterobject : MonoBehaviour, IClientInteractable, IInteractable
	{
		protected static PopupInterobject.PopupState CurrentState
		{
			get
			{
				return PopupInterobject._currentState;
			}
			set
			{
				if (PopupInterobject._currentState == value)
				{
					return;
				}
				PopupInterobject._currentState = value;
				if (PopupInterobject._currentInstance != null)
				{
					PopupInterobject._currentInstance.OnClientStateChange();
				}
			}
		}

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ClientInteract(InteractableCollider colliderId)
		{
			if (PopupInterobject.CurrentState != PopupInterobject.PopupState.Off)
			{
				PopupInterobject.CurrentState = PopupInterobject.PopupState.Disabling;
				return;
			}
			PopupInterobject._currentInstance = this;
			PopupInterobject._timer = 0f;
			PopupInterobject.CurrentState = PopupInterobject.PopupState.Enabling;
			PopupInterobject.TrackedPosition.w = this.CloseAutomaticallyDistance;
		}

		private void Update()
		{
			if (PopupInterobject._masterInstance == null || !PopupInterobject._masterInstance.gameObject.activeSelf)
			{
				PopupInterobject._masterInstance = this;
			}
			if (PopupInterobject._masterInstance != this)
			{
				return;
			}
			switch (PopupInterobject.CurrentState)
			{
			case PopupInterobject.PopupState.Enabling:
				PopupInterobject._timer += Time.deltaTime;
				if (PopupInterobject._timer >= PopupInterobject._currentInstance.FadeSpeedSeconds)
				{
					PopupInterobject.CurrentState = PopupInterobject.PopupState.On;
				}
				PopupInterobject._currentInstance.OnClientUpdate(PopupInterobject._timer / PopupInterobject._currentInstance.FadeSpeedSeconds);
				if (!PopupInterobject.IsInRange())
				{
					PopupInterobject.CurrentState = PopupInterobject.PopupState.Disabling;
					return;
				}
				break;
			case PopupInterobject.PopupState.On:
				if (!PopupInterobject.IsInRange() || Input.GetKeyDown(InteractionCoordinator.InteractKey) || Cursor.visible)
				{
					PopupInterobject.CurrentState = PopupInterobject.PopupState.Disabling;
				}
				break;
			case PopupInterobject.PopupState.Disabling:
				PopupInterobject._timer -= Time.deltaTime;
				if (PopupInterobject._timer <= 0f)
				{
					PopupInterobject.CurrentState = PopupInterobject.PopupState.Off;
				}
				PopupInterobject._currentInstance.OnClientUpdate(PopupInterobject._timer / PopupInterobject._currentInstance.FadeSpeedSeconds);
				return;
			default:
				return;
			}
		}

		private static bool IsInRange()
		{
			ReferenceHub referenceHub;
			return PopupInterobject.TrackedPosition.w <= 0f || (ReferenceHub.TryGetLocalHub(out referenceHub) && Vector3.Distance(PopupInterobject.TrackedPosition, referenceHub.transform.position) < PopupInterobject.TrackedPosition.w);
		}

		protected abstract void OnClientStateChange();

		protected abstract void OnClientUpdate(float enableRatio);

		public float FadeSpeedSeconds = 0.2f;

		private static PopupInterobject _masterInstance;

		public float CloseAutomaticallyDistance;

		protected static Vector4 TrackedPosition;

		private static PopupInterobject _currentInstance;

		private static float _timer;

		private static PopupInterobject.PopupState _currentState;

		protected enum PopupState : byte
		{
			Off,
			Enabling,
			On,
			Disabling
		}
	}
}
