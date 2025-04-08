using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using GameObjectPools;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class RipplePlayer : SubroutineBase, IPoolSpawnable
	{
		private bool CanHear
		{
			get
			{
				return !this._deafened.IsEnabled;
			}
		}

		public void Play(Vector3 position, Color color)
		{
			if (!this.CanHear)
			{
				return;
			}
			bool flag = false;
			RippleInstance rippleInstance = null;
			while (this._poolCount != 0 && !(flag = (rippleInstance = this._pool.Peek()) != null))
			{
				this._pool.Dequeue();
				this._poolCount--;
			}
			if (flag && !rippleInstance.InUse)
			{
				this._pool.Dequeue();
				this._poolCount--;
			}
			else
			{
				rippleInstance = global::UnityEngine.Object.Instantiate<RippleInstance>(this._rippleTemplate);
			}
			rippleInstance.Set(position, (this._focus.State < 1f) ? Color.red : color);
			this._pool.Enqueue(rippleInstance);
			this._poolCount++;
		}

		public void Play(HumanRole human)
		{
			this.Play(human.FpcModule.Position, human.RoleColor);
		}

		public void SpawnObject()
		{
			ReferenceHub referenceHub;
			base.Role.TryGetOwner(out referenceHub);
			this._deafened = referenceHub.playerEffectsController.GetEffect<Deafened>();
		}

		protected override void Awake()
		{
			base.Awake();
			(base.Role as ISubroutinedRole).SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focus);
		}

		[SerializeField]
		private RippleInstance _rippleTemplate;

		private Scp939FocusAbility _focus;

		private Deafened _deafened;

		private int _poolCount;

		private readonly Queue<RippleInstance> _pool = new Queue<RippleInstance>();
	}
}
