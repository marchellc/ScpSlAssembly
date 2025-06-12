using Interactables.Verification;
using UnityEngine;

namespace Interactables.Interobjects;

public abstract class PopupInterobject : MonoBehaviour, IClientInteractable, IInteractable
{
	protected enum PopupState : byte
	{
		Off,
		Enabling,
		On,
		Disabling
	}

	public float FadeSpeedSeconds = 0.2f;

	private static PopupInterobject _masterInstance;

	public float CloseAutomaticallyDistance;

	protected static Vector4 TrackedPosition;

	private static PopupInterobject _currentInstance;

	private static float _timer;

	private static PopupState _currentState;

	protected static PopupState CurrentState
	{
		get
		{
			return PopupInterobject._currentState;
		}
		set
		{
			if (PopupInterobject._currentState != value)
			{
				PopupInterobject._currentState = value;
				if (PopupInterobject._currentInstance != null)
				{
					PopupInterobject._currentInstance.OnClientStateChange();
				}
			}
		}
	}

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public void ClientInteract(InteractableCollider colliderId)
	{
		if (PopupInterobject.CurrentState != PopupState.Off)
		{
			PopupInterobject.CurrentState = PopupState.Disabling;
			return;
		}
		PopupInterobject._currentInstance = this;
		PopupInterobject._timer = 0f;
		PopupInterobject.CurrentState = PopupState.Enabling;
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
		case PopupState.Enabling:
			PopupInterobject._timer += Time.deltaTime;
			if (PopupInterobject._timer >= PopupInterobject._currentInstance.FadeSpeedSeconds)
			{
				PopupInterobject.CurrentState = PopupState.On;
			}
			PopupInterobject._currentInstance.OnClientUpdate(PopupInterobject._timer / PopupInterobject._currentInstance.FadeSpeedSeconds);
			if (!PopupInterobject.IsInRange())
			{
				PopupInterobject.CurrentState = PopupState.Disabling;
			}
			break;
		case PopupState.Disabling:
			PopupInterobject._timer -= Time.deltaTime;
			if (PopupInterobject._timer <= 0f)
			{
				PopupInterobject.CurrentState = PopupState.Off;
			}
			PopupInterobject._currentInstance.OnClientUpdate(PopupInterobject._timer / PopupInterobject._currentInstance.FadeSpeedSeconds);
			break;
		case PopupState.On:
			if (!PopupInterobject.IsInRange() || Input.GetKeyDown(InteractionCoordinator.InteractKey) || Cursor.visible)
			{
				PopupInterobject.CurrentState = PopupState.Disabling;
			}
			break;
		}
	}

	private static bool IsInRange()
	{
		if (PopupInterobject.TrackedPosition.w <= 0f)
		{
			return true;
		}
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return false;
		}
		return Vector3.Distance(PopupInterobject.TrackedPosition, hub.transform.position) < PopupInterobject.TrackedPosition.w;
	}

	protected abstract void OnClientStateChange();

	protected abstract void OnClientUpdate(float enableRatio);
}
