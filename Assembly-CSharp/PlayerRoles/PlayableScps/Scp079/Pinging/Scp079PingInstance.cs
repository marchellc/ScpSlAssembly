using System;
using System.Collections.Generic;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Pinging
{
	public class Scp079PingInstance : MonoBehaviour
	{
		public static event Action<Scp079PingInstance> OnSpawned;

		public Sprite IconSprite
		{
			set
			{
				this._spriteRenderer.sprite = value;
			}
		}

		private bool IsVisible
		{
			get
			{
				ReferenceHub referenceHub;
				ReferenceHub referenceHub2;
				return ReferenceHub.TryGetLocalHub(out referenceHub) && (referenceHub.IsSCP(true) || (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub2) && (this._startPos - MainCameraController.CurrentCamera.position).sqrMagnitude <= 3000f && referenceHub2.IsSCP(true)));
			}
		}

		private void Start()
		{
			this._startPos = base.transform.position;
			global::UnityEngine.Object.Destroy(base.gameObject, this._destroyTime);
			this._wasVisible = true;
			this.UpdateVisibility();
			Scp079PingInstance.Instances.Add(this);
			Action<Scp079PingInstance> onSpawned = Scp079PingInstance.OnSpawned;
			if (onSpawned == null)
			{
				return;
			}
			onSpawned(this);
		}

		private void OnDestroy()
		{
			Scp079PingInstance.Instances.Remove(this);
		}

		private void Update()
		{
			Transform currentCamera = MainCameraController.CurrentCamera;
			float num = Mathf.Max(this._distanceCap, Vector3.Distance(currentCamera.position, this._icon.position));
			this._icon.LookAt(currentCamera);
			this._icon.localScale = this._sizeOverDistance.Evaluate(num) * num * Vector3.one;
			this.UpdateVisibility();
		}

		private void OnValidate()
		{
			this._renderers = base.GetComponentsInChildren<Renderer>();
		}

		private void UpdateVisibility()
		{
			if (this._wasVisible == this.IsVisible)
			{
				return;
			}
			this._src.mute = this._wasVisible;
			this._wasVisible = !this._wasVisible;
			this._renderers.ForEach(delegate(Renderer x)
			{
				x.enabled = this._wasVisible;
			});
		}

		public static readonly HashSet<Scp079PingInstance> Instances = new HashSet<Scp079PingInstance>();

		private const float MaxRangeSqr = 3000f;

		[SerializeField]
		private float _destroyTime;

		[SerializeField]
		private Transform _icon;

		[SerializeField]
		private SpriteRenderer _spriteRenderer;

		[SerializeField]
		private AnimationCurve _sizeOverDistance;

		[SerializeField]
		private float _distanceCap;

		[SerializeField]
		private Renderer[] _renderers;

		[SerializeField]
		private AudioSource _src;

		private Vector3 _startPos;

		private bool _wasVisible;
	}
}
