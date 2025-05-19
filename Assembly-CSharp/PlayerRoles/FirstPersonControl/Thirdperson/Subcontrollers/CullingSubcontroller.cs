using System;
using System.Diagnostics;
using ProgressiveCulling;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class CullingSubcontroller : CullableSimpleDynamic, IAnimatedModelSubcontroller
{
	private AnimatedCharacterModel _model;

	private readonly Stopwatch _culledElapsed = new Stopwatch();

	private const float MaxDeltaTime = 5f;

	public override bool ShouldBeVisible
	{
		get
		{
			if (_model.HasOwner)
			{
				return CullingCamera.CheckBoundsVisibility(base.WorldspaceBounds);
			}
			return true;
		}
	}

	public bool AnimCulled { get; private set; }

	public event Action OnAnimatorUpdated;

	public event Action OnBeforeAnimatorUpdated;

	public void Init(AnimatedCharacterModel model, int index)
	{
		_model = model;
	}

	public void OnReassigned()
	{
		_model.Animator.enabled = false;
	}

	protected override bool AllowDeactivationFilter(GameObject go)
	{
		if (go != base.gameObject)
		{
			return go.activeInHierarchy;
		}
		return false;
	}

	protected override bool AllowCullingFilter(GameObject go)
	{
		if (base.AllowCullingFilter(go))
		{
			return go.activeInHierarchy;
		}
		return false;
	}

	private void EvaluateCulling(out bool allowCulling)
	{
		if (!_model.HasOwner)
		{
			allowCulling = false;
			return;
		}
		float footstepLoudnessDistance = _model.FootstepLoudnessDistance;
		float num = footstepLoudnessDistance * footstepLoudnessDistance;
		Vector3 position = _model.CachedTransform.position;
		Vector3 lastCamPosition = CullingCamera.LastCamPosition;
		allowCulling = (position - lastCamPosition).sqrMagnitude > num;
	}

	private void LateUpdate()
	{
		EvaluateCulling(out var allowCulling);
		bool flag = base.IsCulled && allowCulling;
		double num = _model.LastMovedDeltaT;
		if (flag != AnimCulled)
		{
			AnimCulled = flag;
			if (flag)
			{
				_culledElapsed.Restart();
			}
			else
			{
				num += _culledElapsed.Elapsed.TotalSeconds;
				_culledElapsed.Reset();
			}
		}
		if (!AnimCulled)
		{
			this.OnBeforeAnimatorUpdated?.Invoke();
			num = Math.Min(num, 5.0);
			_model.Animator.Update((float)num);
			this.OnAnimatorUpdated?.Invoke();
		}
	}
}
