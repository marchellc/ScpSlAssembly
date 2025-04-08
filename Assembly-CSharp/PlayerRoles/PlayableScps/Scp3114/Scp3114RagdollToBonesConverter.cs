using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114RagdollToBonesConverter : SubroutineBase
	{
		public static void ConvertExisting(DynamicRagdoll ragdoll)
		{
			if (!Scp3114RagdollToBonesConverter._cacheSet)
			{
				if (!Scp3114RagdollToBonesConverter.TryPrepCache())
				{
					return;
				}
				Scp3114RagdollToBonesConverter._cacheSet = true;
			}
			Transform transform = ragdoll.transform;
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				global::UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
			}
			Scp3114RagdollToBonesConverter.ReplacementRbs.Clear();
			Scp3114RagdollToBonesConverter.ReplacementTransforms.Clear();
			Transform[] linkedRigidbodiesTransforms = ragdoll.LinkedRigidbodiesTransforms;
			int num = linkedRigidbodiesTransforms.Length;
			for (int j = 0; j < num; j++)
			{
				string text = linkedRigidbodiesTransforms[j].name.ToLowerInvariant();
				for (int k = 0; k < Scp3114RagdollToBonesConverter._cachedNames.Length; k++)
				{
					if (text.Contains(Scp3114RagdollToBonesConverter._cachedNames[k]))
					{
						Scp3114RagdollToBonesConverter.ProcessNameMatch(ragdoll, linkedRigidbodiesTransforms[j], Scp3114RagdollToBonesConverter._cachedTemplates[k]);
						break;
					}
				}
			}
			ragdoll.Hitboxes = new HitboxData[0];
			ragdoll.LinkedRigidbodies = Scp3114RagdollToBonesConverter.ReplacementRbs.ToArray();
			ragdoll.LinkedRigidbodiesTransforms = Scp3114RagdollToBonesConverter.ReplacementTransforms.ToArray();
		}

		public static void ServerConvertNew(Scp3114Role scp, DynamicRagdoll ragdoll)
		{
			Scp3114RagdollToBonesConverter scp3114RagdollToBonesConverter;
			if (!scp.SubroutineModule.TryGetSubroutine<Scp3114RagdollToBonesConverter>(out scp3114RagdollToBonesConverter))
			{
				return;
			}
			RagdollData info = ragdoll.Info;
			ragdoll.NetworkInfo = new RagdollData(info.OwnerHub, new Scp3114DamageHandler(ragdoll, false), info.RoleType, info.StartPosition, info.StartRotation, info.Nickname, info.CreationTime, 0);
			scp3114RagdollToBonesConverter._syncRagdoll = ragdoll;
			scp3114RagdollToBonesConverter.ServerSendRpc(true);
		}

		private static bool TryPrepCache()
		{
			Scp3114Role scp3114Role;
			if (!PlayerRoleLoader.TryGetRoleTemplate<Scp3114Role>(RoleTypeId.Scp3114, out scp3114Role))
			{
				return false;
			}
			Scp3114RagdollToBonesConverter scp3114RagdollToBonesConverter;
			if (!scp3114Role.SubroutineModule.TryGetSubroutine<Scp3114RagdollToBonesConverter>(out scp3114RagdollToBonesConverter))
			{
				return false;
			}
			Scp3114RagdollToBonesConverter._cachedTemplates = scp3114RagdollToBonesConverter._boneParts;
			Scp3114RagdollToBonesConverter._cachedNames = new string[Scp3114RagdollToBonesConverter._cachedTemplates.Length];
			for (int i = 0; i < Scp3114RagdollToBonesConverter._cachedTemplates.Length; i++)
			{
				Scp3114RagdollToBonesConverter._cachedNames[i] = Scp3114RagdollToBonesConverter._cachedTemplates[i].name.ToLowerInvariant();
			}
			return true;
		}

		private static void ProcessNameMatch(DynamicRagdoll ragdoll, Transform ragdollPart, GameObject matchedBone)
		{
			Transform transform = matchedBone.transform;
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(matchedBone, ragdoll.transform);
			gameObject.SetActive(true);
			Rigidbody component = gameObject.GetComponent<Rigidbody>();
			Transform transform2 = gameObject.transform;
			Scp3114RagdollToBonesConverter.ReplacementRbs.Add(component);
			Scp3114RagdollToBonesConverter.ReplacementTransforms.Add(transform2);
			transform2.SetPositionAndRotation(ragdollPart.TransformPoint(transform.localPosition), ragdollPart.rotation * transform.localRotation);
			Rigidbody rigidbody;
			if (ragdollPart.TryGetComponent<Rigidbody>(out rigidbody))
			{
				component.velocity = rigidbody.velocity;
				component.angularVelocity = rigidbody.angularVelocity;
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteNetworkBehaviour(this._syncRagdoll);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._syncRagdoll = reader.ReadNetworkBehaviour<DynamicRagdoll>();
			if (this._syncRagdoll == null)
			{
				return;
			}
			Scp3114RagdollToBonesConverter.ConvertExisting(this._syncRagdoll);
		}

		[SerializeField]
		private GameObject[] _boneParts;

		private DynamicRagdoll _syncRagdoll;

		private static bool _cacheSet;

		private static string[] _cachedNames;

		private static GameObject[] _cachedTemplates;

		private static readonly List<Transform> ReplacementTransforms = new List<Transform>();

		private static readonly List<Rigidbody> ReplacementRbs = new List<Rigidbody>();
	}
}
