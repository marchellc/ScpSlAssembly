using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors
{
	[RequireComponent(typeof(StructurePositionSync))]
	public class SpawnableStructure : NetworkBehaviour
	{
		public static event Action<SpawnableStructure> OnAdded;

		public static event Action<SpawnableStructure> OnRemoved;

		public int MinAmount
		{
			get
			{
				return Mathf.FloorToInt(this.MinMaxProbability.Evaluate(0f));
			}
		}

		public int MaxAmount
		{
			get
			{
				return Mathf.FloorToInt(this.MinMaxProbability.Evaluate(1f));
			}
		}

		public RoomIdentifier ParentRoom
		{
			get
			{
				if (this._parentRoom != null)
				{
					return this._parentRoom;
				}
				RoomIdentifier roomIdentifier;
				if (!RoomUtils.TryGetRoom(base.transform.position, out roomIdentifier))
				{
					return null;
				}
				this._parentRoom = roomIdentifier;
				return this._parentRoom;
			}
		}

		protected virtual void Start()
		{
			SpawnableStructure.AllInstances.Add(this);
			Action<SpawnableStructure> onAdded = SpawnableStructure.OnAdded;
			if (onAdded == null)
			{
				return;
			}
			onAdded(this);
		}

		protected virtual void OnDestroy()
		{
			SpawnableStructure.AllInstances.Remove(this);
			Action<SpawnableStructure> onRemoved = SpawnableStructure.OnRemoved;
			if (onRemoved == null)
			{
				return;
			}
			onRemoved(this);
		}

		public override bool Weaved()
		{
			return true;
		}

		public static readonly HashSet<SpawnableStructure> AllInstances = new HashSet<SpawnableStructure>();

		public StructureType StructureType;

		private RoomIdentifier _parentRoom;

		[Tooltip("Defines the number of minimum and maximum amounts of instances of this structure. The generator chooses a random horizontal point between 0 to 1, and reads its vertical value.")]
		public AnimationCurve MinMaxProbability = AnimationCurve.Constant(0f, 0f, 0f);
	}
}
