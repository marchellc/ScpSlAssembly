using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace AdminToys
{
	public class CapybaraToy : AdminToyBase
	{
		public override string CommandName
		{
			get
			{
				return "Capybara";
			}
		}

		public bool CollisionsEnabled
		{
			get
			{
				return this._collisionsEnabled;
			}
			set
			{
				this.SetCollidersState(value);
				this.Network_collisionsEnabled = value;
			}
		}

		public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			base.OnSpawned(admin, arguments);
			IFpcRole fpcRole = admin.roleManager.CurrentRole as IFpcRole;
			Vector3 vector;
			Quaternion quaternion;
			if (fpcRole != null)
			{
				RaycastHit raycastHit;
				if (Physics.Raycast(fpcRole.FpcModule.Position, Vector3.down, out raycastHit, 2f))
				{
					vector = raycastHit.point;
				}
				else
				{
					vector = fpcRole.FpcModule.Position;
				}
				quaternion = Quaternion.Euler(0f, fpcRole.FpcModule.MouseLook.CurrentHorizontal, 0f);
			}
			else
			{
				admin.transform.GetPositionAndRotation(out vector, out quaternion);
			}
			base.transform.SetPositionAndRotation(vector, Quaternion.Euler(0f, quaternion.eulerAngles.y, 0f));
			base.transform.localScale = Vector3.one;
		}

		private void SetCollidersState(bool active)
		{
			foreach (Collider collider in this._colliders)
			{
				collider.enabled = active;
			}
		}

		private void SetCollisionsEnabled(bool oldValue, bool newValue)
		{
			this.SetCollidersState(newValue);
		}

		private void Awake()
		{
			this._colliders = ListPool<Collider>.Shared.Rent();
			base.GetComponentsInChildren<Collider>(true, this._colliders);
		}

		private void OnDestroy()
		{
			ListPool<Collider>.Shared.Return(this._colliders);
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool Network_collisionsEnabled
		{
			get
			{
				return this._collisionsEnabled;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this._collisionsEnabled, 32UL, new Action<bool, bool>(this.SetCollisionsEnabled));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteBool(this._collisionsEnabled);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 32UL) != 0UL)
			{
				writer.WriteBool(this._collisionsEnabled);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._collisionsEnabled, new Action<bool, bool>(this.SetCollisionsEnabled), reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 32L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._collisionsEnabled, new Action<bool, bool>(this.SetCollisionsEnabled), reader.ReadBool());
			}
		}

		[SyncVar(hook = "SetCollisionsEnabled")]
		private bool _collisionsEnabled;

		private List<Collider> _colliders;
	}
}
