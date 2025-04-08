using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public abstract class OverconBase : MonoBehaviour
	{
		public virtual bool IsHighlighted { get; internal set; }

		protected virtual void OnEnable()
		{
			OverconBase.ActiveInstances.Add(this);
		}

		protected virtual void OnDisable()
		{
			OverconBase.ActiveInstances.Remove(this);
		}

		public static readonly HashSet<OverconBase> ActiveInstances = new HashSet<OverconBase>();
	}
}
