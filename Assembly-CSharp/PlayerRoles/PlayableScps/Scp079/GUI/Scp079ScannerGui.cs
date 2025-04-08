using System;
using PlayerRoles.PlayableScps.Scp079.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079ScannerGui : Scp079GuiElementBase
	{
		public static float MapZoom
		{
			get
			{
				CachedValue<Vector3> pos = Scp079ScannerGui._pos;
				if (pos == null)
				{
					return 1f;
				}
				return pos.Value.z;
			}
		}

		public static Vector2 MapPos
		{
			get
			{
				CachedValue<Vector3> pos = Scp079ScannerGui._pos;
				return (pos != null) ? pos.Value : Vector2.zero;
			}
		}

		public static float AnimInterpolant { get; private set; }

		private Bounds GenerateCombinedBounds()
		{
			int num = this._zoneMaps.Value.Length;
			if (num == 0)
			{
				throw new InvalidOperationException("Cannot set cache of Breach Scanner zone maps before they are generated.");
			}
			bool flag = false;
			Bounds bounds = default(Bounds);
			for (int i = 0; i < num; i++)
			{
				IZoneMap zoneMap = this._zoneMaps.Value[i];
				Bounds rectBounds = zoneMap.RectBounds;
				if (!flag)
				{
					bounds = rectBounds;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(rectBounds);
				}
				if (!zoneMap.Ready)
				{
					this._combinedBounds.SetDirty();
				}
			}
			bounds.SetMinMax(new Vector2(bounds.min.x - (float)this._padding.left, bounds.min.y - (float)this._padding.bottom), new Vector2(bounds.max.x + (float)this._padding.right, bounds.max.y + (float)this._padding.top));
			return bounds;
		}

		private Scp079ScannerGui.ToggleInstance[] GenerateToggleInstances()
		{
			IZoneMap[] value = this._zoneMaps.Value;
			Scp079ScannerGui.ToggleInstance[] array = new Scp079ScannerGui.ToggleInstance[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				MonoBehaviour monoBehaviour = value[i] as MonoBehaviour;
				if (monoBehaviour != null)
				{
					GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this._toggleInstanceTemplate, monoBehaviour.transform);
					Button component = gameObject.GetComponent<Button>();
					RectTransform rectTransform = component.transform as RectTransform;
					array[i] = new Scp079ScannerGui.ToggleInstance
					{
						IsSet = true,
						Fader = gameObject.GetComponent<CanvasGroup>(),
						Outline = (component.targetGraphic as Image),
						ToggleImage = rectTransform.GetChild(0).GetComponent<Image>()
					};
					int iCopy = i;
					component.onClick.AddListener(delegate
					{
						this.ProcessButtonEvent(iCopy);
					});
					Bounds rectBounds = value[i].RectBounds;
					rectTransform.anchoredPosition = rectBounds.center;
					rectTransform.sizeDelta = rectBounds.size + Vector3.one * this._zoneBorderWidth;
				}
			}
			return array;
		}

		private void ProcessButtonEvent(int instanceId)
		{
			this._zoneSelector.ToggleZoneStatus(this._zoneMaps.Value[instanceId].Zone);
			this.RefreshZoneToggles();
		}

		private void RefreshZoneToggles()
		{
			Scp079ScannerGui.ToggleInstance[] value = this._toggleInstances.Value;
			for (int i = 0; i < value.Length; i++)
			{
				Scp079ScannerGui.ToggleInstance toggleInstance = value[i];
				if (toggleInstance.IsSet)
				{
					bool zoneStatus = this._zoneSelector.GetZoneStatus(this._zoneMaps.Value[i].Zone);
					toggleInstance.Outline.color = (zoneStatus ? this._onOutlineColor : this._offOutlineColor);
					toggleInstance.ToggleImage.sprite = (zoneStatus ? this._onIcon : this._offIcon);
				}
			}
			int selectedZonesCnt = this._zoneSelector.SelectedZonesCnt;
			Scp079HudTranslation scp079HudTranslation = ((selectedZonesCnt == 0) ? Scp079HudTranslation.ScannerNoZoneSelectedLabel : Scp079HudTranslation.ScannerSelectedZoneCntLabel);
			this._selectionText.text = string.Format(Translations.Get<Scp079HudTranslation>(scp079HudTranslation), selectedZonesCnt);
			if (this._prevZoneCnt == selectedZonesCnt)
			{
				return;
			}
			base.PlaySound(this._zoneSelectClips[Mathf.Min(selectedZonesCnt, this._zoneSelectClips.Length - 1)], 1f);
			this._prevZoneCnt = selectedZonesCnt;
		}

		private void RefreshFilters()
		{
			int num = 0;
			foreach (Scp079ScannerGui.TeamFilter teamFilter in this._teamFilters)
			{
				bool teamStatus = this._teamSelector.GetTeamStatus(teamFilter.Team);
				teamFilter.Checkbox.SetIsOnWithoutNotify(teamStatus);
				if (teamStatus)
				{
					num++;
				}
			}
			this._filtersWarningText.enabled = num == 0;
			if (num == this._prevTeamsCnt)
			{
				return;
			}
			if (this._wasOpen)
			{
				base.PlaySound(this._toggleFliterSound, 1f);
			}
			this._prevTeamsCnt = num;
		}

		private void OnHumanDetected(ReferenceHub hub)
		{
			base.PlaySound(this._detectedHumanSound, 1f);
			Scp079DetectedPlayerIndicator scp079DetectedPlayerIndicator = global::UnityEngine.Object.Instantiate<Scp079DetectedPlayerIndicator>(this._indicator);
			scp079DetectedPlayerIndicator.Setup(hub, this._zoneMaps.Value, base.transform as RectTransform);
			scp079DetectedPlayerIndicator.gameObject.SetActive(true);
		}

		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			Scp079ScannerGui.AnimInterpolant = 0f;
			role.SubroutineModule.TryGetSubroutine<Scp079ScannerTracker>(out this._tracker);
			role.SubroutineModule.TryGetSubroutine<Scp079ScannerZoneSelector>(out this._zoneSelector);
			role.SubroutineModule.TryGetSubroutine<Scp079ScannerTeamFilterSelector>(out this._teamSelector);
			this._tracker.OnDetected += this.OnHumanDetected;
			this._prevZoneCnt = this._zoneSelector.SelectedZonesCnt;
			this._combinedBounds = new CachedValue<Bounds>(new Func<Bounds>(this.GenerateCombinedBounds));
			this._toggleInstances = new CachedValue<Scp079ScannerGui.ToggleInstance[]>(new Func<Scp079ScannerGui.ToggleInstance[]>(this.GenerateToggleInstances));
			this._zoneMaps = new CachedValue<IZoneMap[]>(() => base.GetComponentsInChildren<IZoneMap>());
			Scp079ScannerGui._pos = new CachedValue<Vector3>(delegate
			{
				Bounds value = this._combinedBounds.Value;
				Vector2 vector = this._fillRect.rect.size / value.size;
				float num = Mathf.Min(vector.x, vector.y);
				float num2 = 1f / num;
				Vector3 vector2 = this._fillRect.localPosition * num2;
				Vector2 vector3 = this._fillRect.rect.size * num2 / 2f;
				return new Vector3(-value.center.x - vector3.x, -value.center.y, num) + vector2;
			});
			Scp079ScannerGui.TeamFilter[] teamFilters = this._teamFilters;
			for (int i = 0; i < teamFilters.Length; i++)
			{
				Scp079ScannerGui.TeamFilter filter = teamFilters[i];
				if (!role.IsLocalPlayer)
				{
					filter.Checkbox.interactable = false;
				}
				else
				{
					filter.Checkbox.onValueChanged.AddListener(delegate(bool isOn)
					{
						this._teamSelector.SetTeamStatus(filter.Team, isOn);
						this.RefreshFilters();
					});
				}
			}
		}

		private void OnDestroy()
		{
			this._tracker.OnDetected -= this.OnHumanDetected;
		}

		private void Update()
		{
			bool visible = Scp079ToggleMenuAbilityBase<Scp079ScannerMenuToggler>.Visible;
			float num = (float)(visible ? 1 : 0);
			float animInterpolant = Scp079ScannerGui.AnimInterpolant;
			Scp079ScannerGui.AnimInterpolant = Mathf.MoveTowards(animInterpolant, num, Time.deltaTime * this._lerpSpeed);
			if (visible)
			{
				this.UpdateOpen();
			}
			this._wasOpen = visible;
			this._centerCursor.SetActive(!visible);
			if (animInterpolant == Scp079ScannerGui.AnimInterpolant)
			{
				return;
			}
			this.UpdateLayout();
		}

		private void UpdateOpen()
		{
			IZoneMap[] value = this._zoneMaps.Value;
			for (int i = 0; i < value.Length; i++)
			{
				if (!value[i].Ready)
				{
					return;
				}
			}
			bool flag = !Scp079Role.LocalInstanceActive;
			if (!this._wasOpen || flag)
			{
				CachedValue<Vector3> pos = Scp079ScannerGui._pos;
				if (pos != null)
				{
					pos.SetDirty();
				}
				this.RefreshZoneToggles();
				this.RefreshFilters();
			}
			this._nextScanText.text = this._tracker.StatusText;
		}

		private void UpdateLayout()
		{
			bool flag = Scp079ToggleMenuAbilityBase<Scp079ScannerMenuToggler>.Visible && Scp079Role.LocalInstanceActive;
			this._uiFader.alpha = Scp079ScannerGui.AnimInterpolant;
			this._uiFader.blocksRaycasts = flag;
			foreach (Scp079ScannerGui.ToggleInstance toggleInstance in this._toggleInstances.Value)
			{
				if (toggleInstance.IsSet)
				{
					toggleInstance.Fader.blocksRaycasts = flag;
					toggleInstance.Fader.alpha = Scp079ScannerGui.AnimInterpolant;
				}
			}
			this._targetCounterText.alpha = 1f - Scp079ScannerGui.AnimInterpolant;
		}

		[SerializeField]
		private float _lerpSpeed;

		[SerializeField]
		private RectTransform _fillRect;

		[SerializeField]
		private TMP_Text _selectionText;

		[SerializeField]
		private RectOffset _padding;

		[SerializeField]
		private CanvasGroup _uiFader;

		[SerializeField]
		private GameObject _centerCursor;

		[SerializeField]
		private GameObject _toggleInstanceTemplate;

		[SerializeField]
		private Sprite _onIcon;

		[SerializeField]
		private Sprite _offIcon;

		[SerializeField]
		private Color _onOutlineColor;

		[SerializeField]
		private Color _offOutlineColor;

		[SerializeField]
		private float _zoneBorderWidth;

		[SerializeField]
		private TMP_Text _nextScanText;

		[SerializeField]
		private TMP_Text _targetCounterText;

		[SerializeField]
		private TMP_Text _filtersWarningText;

		[SerializeField]
		private Scp079DetectedPlayerIndicator _indicator;

		[SerializeField]
		private Scp079ScannerGui.TeamFilter[] _teamFilters;

		[SerializeField]
		private AudioClip[] _zoneSelectClips;

		[SerializeField]
		private AudioClip _toggleFliterSound;

		[SerializeField]
		private AudioClip _detectedHumanSound;

		private bool _wasOpen;

		private int _prevZoneCnt;

		private int _prevTeamsCnt;

		private Scp079ScannerTracker _tracker;

		private Scp079ScannerZoneSelector _zoneSelector;

		private Scp079ScannerTeamFilterSelector _teamSelector;

		private CachedValue<Bounds> _combinedBounds;

		private CachedValue<IZoneMap[]> _zoneMaps;

		private CachedValue<Scp079ScannerGui.ToggleInstance[]> _toggleInstances;

		private static CachedValue<Vector3> _pos;

		private struct ToggleInstance
		{
			public CanvasGroup Fader;

			public Image ToggleImage;

			public Graphic Outline;

			public bool IsSet;
		}

		[Serializable]
		private struct TeamFilter
		{
			public Toggle Checkbox;

			public Team Team;
		}
	}
}
