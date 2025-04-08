using System;
using System.Collections.Generic;
using GameObjectPools;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.Spectating
{
	public class SpectatableListManager : MonoBehaviour
	{
		private void OnEnable()
		{
			SpectatorTargetTracker.OnTargetChanged += this.RefreshSize;
			SpectatableModuleBase.OnAdded += this.AddTarget;
			SpectatableModuleBase.OnRemoved += this.RemoveTarget;
			if (!SpectatableListManager._initialized)
			{
				foreach (SpectatableListElementDefinition spectatableListElementDefinition in this._definedPairs)
				{
					SpectatableListManager.Definitions[spectatableListElementDefinition.Type] = spectatableListElementDefinition;
					PoolManager.Singleton.TryAddPool(spectatableListElementDefinition.FullSize);
					PoolManager.Singleton.TryAddPool(spectatableListElementDefinition.Compact);
				}
				SpectatableListManager.RefreshKeybinds();
				SpectatableListManager._initialized = true;
			}
			this.RefreshAllTargets();
		}

		private void OnDisable()
		{
			SpectatorTargetTracker.OnTargetChanged -= this.RefreshSize;
			SpectatableModuleBase.OnAdded -= this.AddTarget;
			SpectatableModuleBase.OnRemoved -= this.RemoveTarget;
		}

		private void LateUpdate()
		{
		}

		private void AddTarget(SpectatableModuleBase target)
		{
			int orderPriority = SpectatableListManager.GetOrderPriority(target.MainRole);
			int num = this._spawnedTargets.Count;
			for (int i = 0; i < this._spawnedTargets.Count; i++)
			{
				if (this._spawnedTargets[i].Priority > orderPriority)
				{
					num = i;
					break;
				}
			}
			SpectatableListElementDefinition spectatableListElementDefinition;
			if (!SpectatableListManager.Definitions.TryGetValue(target.ListElementType, out spectatableListElementDefinition))
			{
				return;
			}
			SpectatableListElementBase spectatableListElementBase;
			SpectatableListElementBase spectatableListElementBase2;
			if (!spectatableListElementDefinition.TryGetFromPools(base.transform, out spectatableListElementBase, out spectatableListElementBase2))
			{
				return;
			}
			SpectatableListSpawnedElement spectatableListSpawnedElement = new SpectatableListSpawnedElement
			{
				Priority = orderPriority,
				Compact = spectatableListElementBase2,
				FullSize = spectatableListElementBase,
				Target = target
			};
			this.SetupNewTarget(spectatableListSpawnedElement.Compact, target, num * 2);
			this.SetupNewTarget(spectatableListSpawnedElement.FullSize, target, num * 2 + 1);
			this._spawnedTargets.Insert(num, spectatableListSpawnedElement);
			this.RefreshSize();
		}

		private void RemoveTarget(SpectatableModuleBase target)
		{
			for (int i = 0; i < this._spawnedTargets.Count; i++)
			{
				if (!(this._spawnedTargets[i].Compact.Target != target))
				{
					this._spawnedTargets[i].ReturnToPool();
					this._spawnedTargets.RemoveAt(i);
					break;
				}
			}
			this.RefreshSize();
		}

		private void RefreshAllTargets()
		{
			this._spawnedTargets.ForEach(delegate(SpectatableListSpawnedElement x)
			{
				x.ReturnToPool();
			});
			this._spawnedTargets.Clear();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				ISpectatableRole spectatableRole = referenceHub.roleManager.CurrentRole as ISpectatableRole;
				if (spectatableRole != null)
				{
					this.AddTarget(spectatableRole.SpectatorModule);
				}
			}
		}

		private void SetupNewTarget(SpectatableListElementBase element, SpectatableModuleBase module, int order)
		{
			Transform transform = element.transform;
			transform.localScale = Vector3.one;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.SetSiblingIndex(order);
			element.Index = order;
			element.Target = module;
		}

		private void RefreshSize()
		{
			int count = this._spawnedTargets.Count;
			if (count == 0)
			{
				return;
			}
			float num2;
			float num = (num2 = this._layoutGroup.spacing * (float)(count - 1));
			for (int i = 0; i < count; i++)
			{
				SpectatableListSpawnedElement spectatableListSpawnedElement = this._spawnedTargets[i];
				num += spectatableListSpawnedElement.Compact.Height;
				num2 += spectatableListSpawnedElement.FullSize.Height;
				if (this._spawnedTargets[i].Target == SpectatorTargetTracker.CurrentTarget)
				{
					this._lastTargetId = i;
				}
			}
			float num3 = Mathf.InverseLerp(num, num2, this._targetHeight);
			int num6;
			int num7;
			if (num3 < 1f)
			{
				int num4 = Mathf.FloorToInt(num3 * (float)count) - 1;
				if (num4 > 12)
				{
					num4 = 12;
				}
				else if (num4 % 2 != 0)
				{
					num4--;
				}
				int num5 = Mathf.Clamp(this._lastTargetId, 0, count - 1);
				num6 = num5 - num4 / 2;
				num7 = num5 + num4 / 2;
				if (num6 < 0)
				{
					num7 -= num6;
				}
				if (num7 >= count)
				{
					num6 += 1 + count - num7;
				}
			}
			else
			{
				num6 = 0;
				num7 = count;
			}
			for (int j = 0; j < count; j++)
			{
				bool flag = j >= num6 && j <= num7;
				this._spawnedTargets[j].FullSize.gameObject.SetActive(flag);
				this._spawnedTargets[j].Compact.gameObject.SetActive(!flag);
			}
		}

		private static void RefreshKeybinds()
		{
			SpectatableListManager._nextKey = NewInput.GetKey(ActionName.Shoot, KeyCode.None);
			SpectatableListManager._prevKey = NewInput.GetKey(ActionName.Zoom, KeyCode.None);
		}

		private static int GetOrderPriority(PlayerRoleBase prb)
		{
			int num = (int)prb.Team.GetFaction() * 65535;
			int num2 = (int)(prb.Team * (Team)255);
			int roleTypeId = (int)prb.RoleTypeId;
			return num + num2 + roleTypeId;
		}

		[SerializeField]
		private SpectatableListElementDefinition[] _definedPairs;

		[SerializeField]
		private float _targetHeight;

		[SerializeField]
		private VerticalLayoutGroup _layoutGroup;

		private int _lastTargetId;

		private readonly List<SpectatableListSpawnedElement> _spawnedTargets = new List<SpectatableListSpawnedElement>();

		private static bool _initialized;

		private static KeyCode _nextKey;

		private static KeyCode _prevKey;

		private static readonly Dictionary<SpectatableListElementType, SpectatableListElementDefinition> Definitions = new Dictionary<SpectatableListElementType, SpectatableListElementDefinition>();
	}
}
