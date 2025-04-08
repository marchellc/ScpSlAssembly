using System;
using System.Collections.Generic;
using MapGeneration;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public abstract class Scp079InteractableBase : MonoBehaviour
	{
		public ushort SyncId { get; private set; }

		public Vector3 Position { get; private set; }

		public virtual RoomIdentifier Room { get; private set; }

		protected virtual void OnRegistered()
		{
			this.Room = RoomUtils.RoomAtPositionRaycasts(base.transform.position, true);
		}

		protected virtual void Awake()
		{
			Scp079InteractableBase.AllInstances.Add(this);
		}

		protected virtual void OnDestroy()
		{
			if (!Scp079InteractableBase.AllInstances.Remove(this))
			{
				return;
			}
			Scp079InteractableBase.OrderedInstances.Remove(this);
		}

		public override string ToString()
		{
			string text = ((base.transform.parent == null) ? "null" : base.transform.parent.name);
			return string.Concat(new string[]
			{
				base.GetType().Name,
				" @ (",
				base.transform.root.name,
				"/.../",
				text,
				"/",
				base.name,
				")"
			});
		}

		public static bool TryGetInteractable(ushort syncId, out Scp079InteractableBase result)
		{
			if (syncId == 0 || (int)syncId > Scp079InteractableBase._instancesCount || !SeedSynchronizer.MapGenerated)
			{
				result = null;
				return false;
			}
			result = Scp079InteractableBase.OrderedInstances[(int)(syncId - 1)];
			return true;
		}

		public static bool TryGetInteractable<T>(ushort syncId, out T result) where T : Scp079InteractableBase
		{
			Scp079InteractableBase scp079InteractableBase;
			if (Scp079InteractableBase.TryGetInteractable(syncId, out scp079InteractableBase))
			{
				T t = scp079InteractableBase as T;
				if (t != null)
				{
					result = t;
					return true;
				}
			}
			result = default(T);
			return false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationStage += Scp079InteractableBase.OnMapGenStage;
		}

		private static void OnMapGenStage(MapGenerationPhase stage)
		{
			if (stage != MapGenerationPhase.ParentRoomRegistration)
			{
				return;
			}
			Scp079InteractableBase.RegisterIds();
		}

		private static void RegisterIds()
		{
			Scp079InteractableBase.AllInstances.RemoveWhere((Scp079InteractableBase x) => !x.gameObject.activeInHierarchy);
			Scp079InteractableBase.OrderedInstances.Clear();
			Scp079InteractableBase._instancesCount = 0;
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079InteractableBase.HandleInstance(scp079InteractableBase);
			}
			ushort num = 1;
			while ((int)num <= Scp079InteractableBase._instancesCount)
			{
				Scp079InteractableBase scp079InteractableBase2 = Scp079InteractableBase.OrderedInstances[(int)(num - 1)];
				scp079InteractableBase2.SyncId = num;
				scp079InteractableBase2.OnRegistered();
				num += 1;
			}
		}

		private static void HandleInstance(Scp079InteractableBase instance)
		{
			instance.Position = instance.transform.position;
			for (int i = 0; i < Scp079InteractableBase._instancesCount; i++)
			{
				if (Scp079InteractableBase.CheckPriority(instance, Scp079InteractableBase.OrderedInstances[i]))
				{
					Scp079InteractableBase.OrderedInstances.Insert(i, instance);
					Scp079InteractableBase._instancesCount++;
					return;
				}
			}
			Scp079InteractableBase.OrderedInstances.Add(instance);
			Scp079InteractableBase._instancesCount++;
		}

		private static bool CheckPriority(Scp079InteractableBase target, Scp079InteractableBase other)
		{
			Vector3 position = target.Position;
			Vector3 position2 = other.Position;
			for (int i = 0; i < 3; i++)
			{
				if (!Mathf.Approximately(position[i], position2[i]))
				{
					return position[i] < position2[i];
				}
			}
			throw new InvalidOperationException(string.Format("Position signature collision detected between {0} and {1}!", target, other));
		}

		public static readonly List<Scp079InteractableBase> OrderedInstances = new List<Scp079InteractableBase>();

		public static readonly HashSet<Scp079InteractableBase> AllInstances = new HashSet<Scp079InteractableBase>();

		private static int _instancesCount;
	}
}
