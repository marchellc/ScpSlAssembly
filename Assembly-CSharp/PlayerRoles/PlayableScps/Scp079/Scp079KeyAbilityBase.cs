using System;
using NetworkManagerUtils.Dummies;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public abstract class Scp079KeyAbilityBase : Scp079AbilityBase, IScp079FailMessageProvider
{
	private enum Category
	{
		Movement,
		SpecialAbility,
		OverconInteraction
	}

	[SerializeField]
	private Category _category;

	private static string _translationNoAux;

	private static UnityEngine.Object TrackedFailMessage
	{
		get
		{
			if (!Scp079AbilityList.TryGetSingleton(out var singleton))
			{
				return null;
			}
			return singleton.TrackedFailMessage as UnityEngine.Object;
		}
		set
		{
			if (Scp079AbilityList.TryGetSingleton(out var singleton))
			{
				singleton.TrackedFailMessage = value as IScp079FailMessageProvider;
			}
		}
	}

	public abstract ActionName ActivationKey { get; }

	public abstract bool IsReady { get; }

	public abstract bool IsVisible { get; }

	public abstract string AbilityName { get; }

	public abstract string FailMessage { get; }

	public virtual bool DummyEmulationSupport => false;

	public int CategoryId => (int)this._category;

	[field: SerializeField]
	public bool UseLeftMenu { get; private set; }

	protected string GetNoAuxMessage(float cost)
	{
		return Scp079KeyAbilityBase._translationNoAux + "\n" + base.AuxManager.GenerateETA(cost);
	}

	protected virtual void Start()
	{
		Scp079KeyAbilityBase._translationNoAux = Translations.Get(Scp079HudTranslation.NotEnoughAux);
	}

	protected virtual void Update()
	{
		if (((!base.Role.IsLocalPlayer || !this.IsVisible) && !base.Role.IsEmulatedDummy) || !base.GetActionDown(this.ActivationKey) || base.LostSignalHandler.Lost || Scp079IntroCutscene.IsPlaying)
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
		}
		else
		{
			Scp079KeyAbilityBase.TrackedFailMessage = this;
		}
	}

	protected abstract void Trigger();

	public virtual void OnFailMessageAssigned()
	{
	}

	public override void PopulateDummyActions(Action<DummyAction> actionAdder, Action<string> categoryAdder)
	{
		if (this.DummyEmulationSupport)
		{
			base.PopulateDummyActions(actionAdder, categoryAdder);
		}
	}
}
