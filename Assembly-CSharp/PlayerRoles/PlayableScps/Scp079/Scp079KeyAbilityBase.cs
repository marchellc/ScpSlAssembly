using System;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public abstract class Scp079KeyAbilityBase : Scp079AbilityBase, IScp079FailMessageProvider
	{
		private static global::UnityEngine.Object TrackedFailMessage
		{
			get
			{
				return Scp079AbilityList.Singleton.TrackedFailMessage as global::UnityEngine.Object;
			}
			set
			{
				Scp079AbilityList.Singleton.TrackedFailMessage = value as IScp079FailMessageProvider;
			}
		}

		public abstract ActionName ActivationKey { get; }

		public abstract bool IsReady { get; }

		public abstract bool IsVisible { get; }

		public abstract string AbilityName { get; }

		public abstract string FailMessage { get; }

		public int CategoryId
		{
			get
			{
				return (int)this._category;
			}
		}

		public bool UseLeftMenu { get; private set; }

		protected string GetNoAuxMessage(float cost)
		{
			return Scp079KeyAbilityBase._translationNoAux + "\n" + base.AuxManager.GenerateETA(cost);
		}

		protected virtual void Start()
		{
			Scp079KeyAbilityBase._translationNoAux = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.NotEnoughAux);
		}

		protected virtual void Update()
		{
			if (!base.Role.IsLocalPlayer || !this.IsVisible)
			{
				return;
			}
			if (!Input.GetKeyDown(NewInput.GetKey(this.ActivationKey, KeyCode.None)))
			{
				return;
			}
			if (base.LostSignalHandler.Lost || Scp079IntroCutscene.IsPlaying)
			{
				return;
			}
			if (this.IsReady)
			{
				if (Scp079KeyAbilityBase.TrackedFailMessage == this)
				{
					Scp079KeyAbilityBase.TrackedFailMessage = null;
				}
				this.Trigger();
				return;
			}
			Scp079KeyAbilityBase.TrackedFailMessage = this;
		}

		protected abstract void Trigger();

		public virtual void OnFailMessageAssigned()
		{
		}

		[SerializeField]
		private Scp079KeyAbilityBase.Category _category;

		private static string _translationNoAux;

		private enum Category
		{
			Movement,
			SpecialAbility,
			OverconInteraction
		}
	}
}
