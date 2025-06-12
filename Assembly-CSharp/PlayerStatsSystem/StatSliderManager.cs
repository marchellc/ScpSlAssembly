using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStatsSystem;

public class StatSliderManager : MonoBehaviour
{
	private record SliderTypePair(StatSlider StatSlider, Type Type);

	private readonly List<SliderTypePair> _instances = new List<SliderTypePair>();

	private static StatSliderManager _singleton;

	private void Awake()
	{
		StatSliderManager._singleton = this;
		base.gameObject.ForEachComponentInChildren<StatSlider>(RegisterInstance, includeInactive: true);
	}

	private void RefreshRelations()
	{
		StatusBar masterBar = null;
		foreach (SliderTypePair instance in this._instances)
		{
			if (instance.StatSlider.TryGetComponent<StatusBar>(out var component))
			{
				component.MasterBar = masterBar;
				masterBar = component;
			}
		}
	}

	private void RegisterInstance(StatSlider inst)
	{
		if (!inst.TryGetTypeId(out var val))
		{
			throw new InvalidOperationException("Attempting to register stat without a valid module.");
		}
		this._instances.Add(new SliderTypePair(inst, PlayerStats.DefinedModules[val]));
	}

	public static bool TryAdd(StatSlider template, out StatSlider instance)
	{
		if (StatSliderManager._singleton == null || template == null)
		{
			instance = null;
			return false;
		}
		instance = UnityEngine.Object.Instantiate(template, StatSliderManager._singleton.transform);
		StatSliderManager._singleton.RegisterInstance(instance);
		Transform obj = instance.transform;
		obj.localScale = Vector3.one;
		obj.localRotation = Quaternion.identity;
		StatSliderManager._singleton.RefreshRelations();
		if (instance.TryGetComponent<StatusBar>(out var component))
		{
			component.UpdateBar(bypassAnims: true);
		}
		return true;
	}

	public static bool TryRemove<T>() where T : StatBase
	{
		Type typeFromHandle = typeof(T);
		if (StatSliderManager._singleton == null || typeof(T).IsAbstract)
		{
			return false;
		}
		List<SliderTypePair> instances = StatSliderManager._singleton._instances;
		for (int i = 0; i < instances.Count; i++)
		{
			SliderTypePair sliderTypePair = instances[i];
			if (!(sliderTypePair.Type != typeFromHandle))
			{
				UnityEngine.Object.Destroy(sliderTypePair.StatSlider.gameObject);
				instances.RemoveAt(i--);
				StatSliderManager._singleton.RefreshRelations();
				return true;
			}
		}
		return false;
	}

	public static bool TryForEach(Action<StatSlider> action)
	{
		if (StatSliderManager._singleton == null)
		{
			return false;
		}
		StatSliderManager._singleton._instances.ForEach(delegate(SliderTypePair x)
		{
			action(x.StatSlider);
		});
		return true;
	}
}
