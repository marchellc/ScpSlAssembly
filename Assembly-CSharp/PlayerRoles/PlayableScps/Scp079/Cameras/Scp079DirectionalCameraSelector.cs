using System.Text;
using MapGeneration;
using NorthwoodLib.Pools;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace PlayerRoles.PlayableScps.Scp079.Cameras;

public class Scp079DirectionalCameraSelector : Scp079KeyAbilityBase
{
	private static string _translationNoCamera;

	private static string _translationPaidSwitch;

	private static string _translationFreeSwitch;

	private static bool _translationsSet;

	private static readonly Vector3Int[] WorldDirections = new Vector3Int[4]
	{
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 0, -1),
		new Vector3Int(1, 0, 0),
		new Vector3Int(0, 0, 1)
	};

	private static readonly CachedUserSetting<bool> AllowKeybindZoneSwitching = new CachedUserSetting<bool>(MiscControlsSetting.Scp079KeybindZoneSwitching);

	[SerializeField]
	private ActionName _key;

	[SerializeField]
	private Vector3 _direction;

	private Scp079Camera _lastCamera;

	private bool _lastValid;

	private float _lastSwitchCost;

	private float _failMessageSwitchCost;

	protected virtual bool AllowSwitchingBetweenZones => Scp079DirectionalCameraSelector.AllowKeybindZoneSwitching.Value;

	public override bool IsReady
	{
		get
		{
			this._lastValid = this.TryGetCamera(out this._lastCamera);
			if (!this._lastValid)
			{
				return false;
			}
			if (!this.AllowSwitchingBetweenZones && this._lastCamera.Room.Zone != base.CurrentCamSync.CurrentCamera.Room.Zone)
			{
				return false;
			}
			this._lastSwitchCost = base.CurrentCamSync.GetSwitchCost(this._lastCamera);
			return this._lastSwitchCost <= base.AuxManager.CurrentAux;
		}
	}

	public override ActionName ActivationKey => this._key;

	public override bool IsVisible => !Scp079CursorManager.LockCameras;

	public override string FailMessage
	{
		get
		{
			if (!(base.AuxManager.CurrentAux < this._failMessageSwitchCost))
			{
				return null;
			}
			return base.GetNoAuxMessage(this._failMessageSwitchCost);
		}
	}

	public override string AbilityName
	{
		get
		{
			if (!this._lastValid)
			{
				return Scp079DirectionalCameraSelector._translationNoCamera;
			}
			return string.Format((this._lastSwitchCost == 0f) ? Scp079DirectionalCameraSelector._translationFreeSwitch : Scp079DirectionalCameraSelector._translationPaidSwitch, this._lastCamera.Label, this._lastSwitchCost);
		}
	}

	protected virtual bool TryGetCamera(out Scp079Camera targetCamera)
	{
		targetCamera = null;
		bool result = false;
		Transform currentCamera = MainCameraController.CurrentCamera;
		Vector3 normalized = currentCamera.TransformDirection(this._direction).normalized;
		Vector3Int vector3Int = Vector3Int.zero;
		float num = -1f;
		Vector3Int[] worldDirections = Scp079DirectionalCameraSelector.WorldDirections;
		foreach (Vector3Int vector3Int2 in worldDirections)
		{
			float num2 = Vector3.Dot(vector3Int2, normalized);
			if (!(num2 < num))
			{
				vector3Int = vector3Int2;
				num = num2;
			}
		}
		if (num <= 0f)
		{
			return false;
		}
		Vector3Int vector3Int3 = RoomUtils.PositionToCoords(currentCamera.position) + vector3Int;
		foreach (CameraOvercon visibleOvercon in CameraOverconRenderer.VisibleOvercons)
		{
			if (visibleOvercon.IsElevator)
			{
				continue;
			}
			Scp079Camera target = visibleOvercon.Target;
			if (!(RoomUtils.PositionToCoords(target.Position) != vector3Int3))
			{
				targetCamera = target;
				result = true;
				if (targetCamera.IsMain)
				{
					return true;
				}
			}
		}
		return result;
	}

	protected override void Trigger()
	{
		base.CurrentCamSync.ClientSwitchTo(this._lastCamera);
	}

	protected override void Start()
	{
		base.Start();
		base.CurrentCamSync.OnCameraChanged += delegate
		{
			this._failMessageSwitchCost = 0f;
		};
		if (Scp079DirectionalCameraSelector._translationsSet)
		{
			return;
		}
		Scp079DirectionalCameraSelector._translationsSet = true;
		Scp079DirectionalCameraSelector._translationNoCamera = Translations.Get(Scp079HudTranslation.NoCamera);
		Scp079DirectionalCameraSelector._translationPaidSwitch = Translations.Get(Scp079HudTranslation.GoToCamera);
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		string[] array = Scp079DirectionalCameraSelector._translationPaidSwitch.Split(' ');
		foreach (string text in array)
		{
			if (!text.Contains("{1}"))
			{
				stringBuilder.Append(text);
				stringBuilder.Append(' ');
			}
		}
		Scp079DirectionalCameraSelector._translationFreeSwitch = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	private void OnDestroy()
	{
		Scp079DirectionalCameraSelector._translationsSet = false;
	}

	public override void OnFailMessageAssigned()
	{
		this._failMessageSwitchCost = (this._lastValid ? this._lastSwitchCost : 0f);
	}
}
