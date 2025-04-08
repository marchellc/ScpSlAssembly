using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class StatSliderManager : MonoBehaviour
	{
		private void Awake()
		{
			StatSliderManager._singleton = this;
			base.gameObject.ForEachComponentInChildren(new Action<StatSlider>(this.RegisterInstance), true);
		}

		private void RefreshRelations()
		{
			StatusBar statusBar = null;
			using (List<StatSliderManager.SliderTypePair>.Enumerator enumerator = this._instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					StatusBar statusBar2;
					if (enumerator.Current.StatSlider.TryGetComponent<StatusBar>(out statusBar2))
					{
						statusBar2.MasterBar = statusBar;
						statusBar = statusBar2;
					}
				}
			}
		}

		private void RegisterInstance(StatSlider inst)
		{
			int num;
			if (!inst.TryGetTypeId(out num))
			{
				throw new InvalidOperationException("Attempting to register stat without a valid module.");
			}
			this._instances.Add(new StatSliderManager.SliderTypePair(inst, PlayerStats.DefinedModules[num]));
		}

		public static bool TryAdd(StatSlider template, out StatSlider instance)
		{
			if (StatSliderManager._singleton == null || template == null)
			{
				instance = null;
				return false;
			}
			instance = global::UnityEngine.Object.Instantiate<StatSlider>(template, StatSliderManager._singleton.transform);
			StatSliderManager._singleton.RegisterInstance(instance);
			Transform transform = instance.transform;
			transform.localScale = Vector3.one;
			transform.localRotation = Quaternion.identity;
			StatSliderManager._singleton.RefreshRelations();
			StatusBar statusBar;
			if (instance.TryGetComponent<StatusBar>(out statusBar))
			{
				statusBar.UpdateBar(true);
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
			List<StatSliderManager.SliderTypePair> instances = StatSliderManager._singleton._instances;
			for (int i = 0; i < instances.Count; i++)
			{
				StatSliderManager.SliderTypePair sliderTypePair = instances[i];
				if (!(sliderTypePair.Type != typeFromHandle))
				{
					global::UnityEngine.Object.Destroy(sliderTypePair.StatSlider.gameObject);
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
			StatSliderManager._singleton._instances.ForEach(delegate(StatSliderManager.SliderTypePair x)
			{
				action(x.StatSlider);
			});
			return true;
		}

		private readonly List<StatSliderManager.SliderTypePair> _instances = new List<StatSliderManager.SliderTypePair>();

		private static StatSliderManager _singleton;

		private class SliderTypePair : IEquatable<StatSliderManager.SliderTypePair>
		{
			public SliderTypePair(StatSlider StatSlider, Type Type)
			{
				this.StatSlider = StatSlider;
				this.Type = Type;
				base..ctor();
			}

			[Nullable(1)]
			protected virtual Type EqualityContract
			{
				[NullableContext(1)]
				[CompilerGenerated]
				get
				{
					return typeof(StatSliderManager.SliderTypePair);
				}
			}

			public StatSlider StatSlider { get; set; }

			public Type Type { get; set; }

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("SliderTypePair");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}

			[NullableContext(1)]
			protected virtual bool PrintMembers(StringBuilder builder)
			{
				builder.Append("StatSlider");
				builder.Append(" = ");
				builder.Append(this.StatSlider);
				builder.Append(", ");
				builder.Append("Type");
				builder.Append(" = ");
				builder.Append(this.Type);
				return true;
			}

			[NullableContext(2)]
			public static bool operator !=(StatSliderManager.SliderTypePair r1, StatSliderManager.SliderTypePair r2)
			{
				return !(r1 == r2);
			}

			[NullableContext(2)]
			public static bool operator ==(StatSliderManager.SliderTypePair r1, StatSliderManager.SliderTypePair r2)
			{
				return r1 == r2 || (r1 != null && r1.Equals(r2));
			}

			public override int GetHashCode()
			{
				return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<StatSlider>.Default.GetHashCode(this.<StatSlider>k__BackingField)) * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(this.<Type>k__BackingField);
			}

			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				return this.Equals(obj as StatSliderManager.SliderTypePair);
			}

			[NullableContext(2)]
			public virtual bool Equals(StatSliderManager.SliderTypePair other)
			{
				return other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<StatSlider>.Default.Equals(this.<StatSlider>k__BackingField, other.<StatSlider>k__BackingField) && EqualityComparer<Type>.Default.Equals(this.<Type>k__BackingField, other.<Type>k__BackingField);
			}

			[NullableContext(1)]
			public virtual StatSliderManager.SliderTypePair <Clone>$()
			{
				return new StatSliderManager.SliderTypePair(this);
			}

			protected SliderTypePair([Nullable(1)] StatSliderManager.SliderTypePair original)
			{
				this.StatSlider = original.<StatSlider>k__BackingField;
				this.Type = original.<Type>k__BackingField;
			}

			public void Deconstruct(out StatSlider StatSlider, out Type Type)
			{
				StatSlider = this.StatSlider;
				Type = this.Type;
			}
		}
	}
}
