using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace FacilitySoundtrack;

public class Scp106HeartbeatSoundtrack : SoundtrackLayerBase
{
	private const float MaxHighHeartBeatVolume = 1f;

	private const float StaringDesaturation = -90f;

	private const float NotStaringDesaturation = -40f;

	private const float MinimumDistance = 2.5f;

	private const float MaximumDistance = 30f;

	private const float SurfaceMaximumDistance = 60f;

	private const double SustainTime = 2.5;

	private const float MinimumSCP106Distance = 75f;

	private const float ModelSize = 1.44f;

	[SerializeField]
	private float _fadeInLerp;

	[SerializeField]
	private float _fadeOutLerp;

	[SerializeField]
	private AudioSource[] _audioSources;

	private float _weight;

	private Traumatized _traumatized;

	private ColorAdjustments _saturationVolume;

	private bool _isStaring;

	private bool _oldIsStaring;

	private bool _toggled;

	private double _sustainTimer;

	public override bool Additive => !_isStaring;

	public override float Weight => _weight;

	private void Awake()
	{
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private void OnDestroy()
	{
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void Update()
	{
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (userHub.isLocalPlayer)
		{
			_weight = 0f;
			_traumatized = userHub.playerEffectsController.GetEffect<Traumatized>();
			_traumatized.PPVolume.profile.TryGet<ColorAdjustments>(out _saturationVolume);
		}
	}

	private float DistanceTo(GameObject oB)
	{
		return Vector3.Distance(oB.transform.position, MainCameraController.CurrentCamera.position);
	}

	public override void UpdateVolume(float volumeScale)
	{
		_audioSources[0].volume = volumeScale;
	}
}
