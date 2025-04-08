using System;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507Hud : ScpHudBase
	{
		internal override void Init(ReferenceHub hub)
		{
			base.Init(hub);
			SubroutineManagerModule subroutineModule = (base.Hub.roleManager.CurrentRole as Scp1507Role).SubroutineModule;
			Scp1507AttackAbility scp1507AttackAbility;
			subroutineModule.TryGetSubroutine<Scp1507AttackAbility>(out scp1507AttackAbility);
			Scp1507VocalizeAbility scp1507VocalizeAbility;
			subroutineModule.TryGetSubroutine<Scp1507VocalizeAbility>(out scp1507VocalizeAbility);
			subroutineModule.TryGetSubroutine<Scp1507SwarmAbility>(out this._swarmAbility);
			this._trackerRoot = this._allyCounter.transform.parent.gameObject;
			this._attackElement.Setup(scp1507AttackAbility.Cooldown, null);
			this._vocalizeElement.Setup(scp1507VocalizeAbility.Cooldown, null);
		}

		protected override void Update()
		{
			base.Update();
			this._attackElement.Update(false);
			this._vocalizeElement.Update(false);
			if (this._swarmAbility.Multiplier <= 0f)
			{
				this._swarmElement.SetActive(false);
				return;
			}
			this._swarmElement.SetActive(true);
			int flockSize = this._swarmAbility.FlockSize;
			Color color = ((flockSize > 0) ? this._swarmActiveColor : this._swarmInactiveColor);
			this._swarmCircle.fillAmount = this._swarmAbility.Multiplier;
			this._swarmCircle.color = Color.Lerp(this._swarmCircle.color, color, Time.deltaTime * this._colorLerpSpeed);
			this._swarmSizeCounter.text = ((flockSize > 1) ? ("x" + flockSize.ToString()) : string.Empty);
		}

		protected override void UpdateCounter()
		{
		}

		[SerializeField]
		private AbilityHud _attackElement;

		[SerializeField]
		private AbilityHud _vocalizeElement;

		[SerializeField]
		private GameObject _swarmElement;

		[SerializeField]
		private Image _swarmCircle;

		[SerializeField]
		private TMP_Text _swarmSizeCounter;

		[SerializeField]
		private Color _swarmActiveColor;

		[SerializeField]
		private Color _swarmInactiveColor;

		[SerializeField]
		private float _colorLerpSpeed;

		[SerializeField]
		private TMP_Text _allyCounter;

		private Scp1507SwarmAbility _swarmAbility;

		private GameObject _trackerRoot;
	}
}
