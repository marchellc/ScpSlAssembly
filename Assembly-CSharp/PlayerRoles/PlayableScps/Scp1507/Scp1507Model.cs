using System;
using AudioPooling;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507Model : CharacterModel
{
	[SerializeField]
	private float _minPosDiff;

	[SerializeField]
	private float _minAngularDiff;

	[SerializeField]
	private float _relocationSpeed;

	[SerializeField]
	private AnimationCurve _relocationLerpOverProgress;

	[SerializeField]
	private AnimationCurve _relocationHeightAnim;

	[SerializeField]
	private Color _outlineColor;

	[SerializeField]
	private AudioClip[] _footsteps;

	[SerializeField]
	private Animator _animator;

	[SerializeField]
	private Transform _footstepOrigin;

	[SerializeField]
	private AnimationCurve _hitboxesSizeMultiplierOverTime;

	private Scp1507Role _role;

	private FirstPersonMovementModule _fpc;

	private Scp1507AttackAbility _peck;

	private Scp1507VocalizeAbility _vocalize;

	private Transform _fpcTr;

	private Transform _ownTr;

	private RelativePosition _prevRelPosition;

	private Quaternion _prevRelRotation;

	private float _relocationProgress;

	private double _lastRelocation;

	private float _lastSize;

	private static readonly int SelectionColorId = Shader.PropertyToID("_SelectionColor");

	private static readonly int WalkAnimHash = Animator.StringToHash("flamingo|walk");

	private static readonly int AttackAnimHash = Animator.StringToHash("Attack");

	private static readonly int VocalizeAnimHash = Animator.StringToHash("Vocalize");

	private static bool _hitboxCacheSet;

	private static float[] _hitboxRadiuses;

	private static float[] _hitboxHeights;

	public const int FootstepRange = 30;

	public static event Action<Scp1507Model> OnFootstepPlayed;

	private void LateUpdate()
	{
		if (base.Pooled)
		{
			return;
		}
		this.UpdateHitboxes();
		Vector3 position = this._prevRelPosition.Position;
		Quaternion worldRotation = WaypointBase.GetWorldRotation(this._prevRelPosition.WaypointId, this._prevRelRotation);
		Vector3 position2 = this._fpc.Position;
		Quaternion rotation = this._fpcTr.rotation;
		if (this._fpc.IsGrounded && !(this._relocationProgress > 0f) && !(Quaternion.Angle(worldRotation, rotation) > this._minAngularDiff) && !(Vector3.Distance(position, position2) > this._minPosDiff))
		{
			Vector3 position3 = new Vector3(position.x, position2.y, position.z);
			this._ownTr.SetPositionAndRotation(position3, worldRotation);
			return;
		}
		this._relocationProgress = Mathf.Clamp01(this._relocationProgress + Time.deltaTime * this._relocationSpeed);
		float t = this._relocationLerpOverProgress.Evaluate(this._relocationProgress);
		Vector3 vector = Vector3.up * this._relocationHeightAnim.Evaluate(this._relocationProgress);
		Vector3 position4 = Vector3.Lerp(position, position2 + vector, t);
		Quaternion rotation2 = Quaternion.Lerp(worldRotation, rotation, t);
		this._ownTr.SetPositionAndRotation(position4, rotation2);
		this._animator.Play(Scp1507Model.WalkAnimHash, 0, this._relocationProgress);
		if (!(this._relocationProgress < 1f) && this._fpc.IsGrounded)
		{
			this._prevRelPosition = new RelativePosition(this._fpc.Position);
			this._prevRelRotation = WaypointBase.GetRelativeRotation(this._prevRelPosition.WaypointId, this._ownTr.rotation);
			AudioSourcePoolManager.PlayOnTransform(this._footsteps.RandomItem(), this._footstepOrigin, 30f, 1f, FalloffType.Footstep);
			Scp1507Model.OnFootstepPlayed?.Invoke(this);
			this._relocationProgress = 0f;
			this._lastRelocation = NetworkTime.time;
		}
	}

	private void UpdateHitboxes()
	{
		float time = (float)(NetworkTime.time - this._lastRelocation);
		float num = this._hitboxesSizeMultiplierOverTime.Evaluate(time);
		if (num != this._lastSize)
		{
			this._lastSize = num;
			for (int i = 0; i < base.Hitboxes.Length; i++)
			{
				CapsuleCollider obj = base.Hitboxes[i].TargetColliders[0] as CapsuleCollider;
				obj.height = Scp1507Model._hitboxHeights[i] * num;
				obj.radius = Scp1507Model._hitboxRadiuses[i] * num;
			}
		}
	}

	private void PlayPeckAnim(AttackResult result)
	{
		this._animator.SetTrigger(Scp1507Model.AttackAnimHash);
	}

	private void PlayVocalizeAnim()
	{
		this._animator.SetTrigger(Scp1507Model.VocalizeAnimHash);
	}

	protected override void Awake()
	{
		base.Awake();
		this._ownTr = base.transform;
		ReadOnlySpan<Renderer> renderers = base.Renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer obj = renderers[i];
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			obj.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetColor(Scp1507Model.SelectionColorId, this._outlineColor);
			obj.SetPropertyBlock(materialPropertyBlock);
		}
		if (!Scp1507Model._hitboxCacheSet)
		{
			Scp1507Model._hitboxCacheSet = true;
			Scp1507Model._hitboxHeights = new float[base.Hitboxes.Length];
			Scp1507Model._hitboxRadiuses = new float[base.Hitboxes.Length];
			for (int j = 0; j < base.Hitboxes.Length; j++)
			{
				CapsuleCollider component = base.Hitboxes[j].GetComponent<CapsuleCollider>();
				Scp1507Model._hitboxHeights[j] = component.height;
				Scp1507Model._hitboxRadiuses[j] = component.radius;
			}
		}
	}

	public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		base.Setup(owner, role, localPos, localRot);
		this._role = role as Scp1507Role;
		this._fpc = this._role.FpcModule;
		this._fpcTr = this._fpc.transform;
		this._role.SubroutineModule.TryGetSubroutine<Scp1507AttackAbility>(out this._peck);
		this._peck.OnAttacked += PlayPeckAnim;
		this._role.SubroutineModule.TryGetSubroutine<Scp1507VocalizeAbility>(out this._vocalize);
		Scp1507VocalizeAbility vocalize = this._vocalize;
		vocalize.OnVocalized = (Action)Delegate.Combine(vocalize.OnVocalized, new Action(PlayVocalizeAnim));
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._peck.OnAttacked -= PlayPeckAnim;
		Scp1507VocalizeAbility vocalize = this._vocalize;
		vocalize.OnVocalized = (Action)Delegate.Remove(vocalize.OnVocalized, new Action(PlayVocalizeAnim));
	}
}
