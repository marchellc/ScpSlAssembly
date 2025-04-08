using System;
using System.Linq;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class OverconManager : MonoBehaviour
	{
		private static int RaycastMask
		{
			get
			{
				if (OverconManager._raycastMask == 0)
				{
					OverconManager._raycastMask = LayerMask.GetMask(new string[] { "Viewmodel" });
				}
				return OverconManager._raycastMask;
			}
		}

		public OverconBase HighlightedOvercon { get; private set; }

		public static OverconManager Singleton { get; private set; }

		private void OnCameraChanged()
		{
			Scp079Camera currentCamera = this._curCamSync.CurrentCamera;
			OverconRendererBase[] renderers = this._renderers;
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].SpawnOvercons(currentCamera);
			}
		}

		private void Start()
		{
			OverconManager.Singleton = this;
			Scp079Hud.Instance.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
			this._curCamSync.OnCameraChanged += this.OnCameraChanged;
			this.OnCameraChanged();
		}

		private void OnDestroy()
		{
			foreach (OverconBase overconBase in OverconBase.ActiveInstances.ToArray<OverconBase>())
			{
				if (!(overconBase == null))
				{
					global::UnityEngine.Object.Destroy(overconBase.gameObject);
				}
			}
			OverconBase.ActiveInstances.Clear();
			if (this._curCamSync != null)
			{
				this._curCamSync.OnCameraChanged -= this.OnCameraChanged;
			}
		}

		private void Update()
		{
			if (!Scp079Role.LocalInstanceActive)
			{
				return;
			}
			RaycastHit raycastHit;
			OverconBase overconBase = (Physics.Raycast(Scp079Hud.MainCamera.ScreenPointToRay(Input.mousePosition), out raycastHit, 200f, OverconManager.RaycastMask) ? raycastHit.collider.GetComponent<OverconBase>() : null);
			if (overconBase == this.HighlightedOvercon)
			{
				return;
			}
			if (this.HighlightedOvercon != null)
			{
				this.HighlightedOvercon.IsHighlighted = false;
			}
			if (overconBase != null)
			{
				overconBase.IsHighlighted = true;
			}
			this.HighlightedOvercon = overconBase;
		}

		[SerializeField]
		private OverconRendererBase[] _renderers;

		private Scp079CurrentCameraSync _curCamSync;

		private const string OverconLayer = "Viewmodel";

		private const float Range = 200f;

		private static int _raycastMask;
	}
}
