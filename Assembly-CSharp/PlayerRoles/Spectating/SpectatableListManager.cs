using System.Collections.Generic;
using GameObjectPools;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.Spectating;

public class SpectatableListManager : MonoBehaviour
{
	[SerializeField]
	private SpectatableListElementDefinition[] _definedPairs;

	[SerializeField]
	private float _targetHeight;

	[SerializeField]
	private VerticalLayoutGroup _layoutGroup;

	private int _lastTargetId;

	private readonly List<SpectatableListSpawnedElement> _spawnedTargets = new List<SpectatableListSpawnedElement>();

	private static KeyCode _nextKey;

	private static KeyCode _prevKey;

	private static readonly Dictionary<SpectatableListElementType, SpectatableListElementDefinition> Definitions = new Dictionary<SpectatableListElementType, SpectatableListElementDefinition>();

	public static bool Initialized;

	public static SpectatableListManager Instance { get; private set; }

	private void OnEnable()
	{
		SpectatorTargetTracker.OnTargetChanged += RefreshSize;
		SpectatableModuleBase.OnAdded += AddTarget;
		SpectatableModuleBase.OnRemoved += RemoveTarget;
		if (!Initialized)
		{
			SpectatableListElementDefinition[] definedPairs = _definedPairs;
			for (int i = 0; i < definedPairs.Length; i++)
			{
				SpectatableListElementDefinition value = definedPairs[i];
				Definitions[value.Type] = value;
				PoolManager.Singleton.TryAddPool(value.FullSize);
				PoolManager.Singleton.TryAddPool(value.Compact);
			}
			RefreshKeybinds();
			Initialized = true;
			Instance = this;
		}
		RefreshAllTargets();
	}

	private void OnDisable()
	{
		SpectatorTargetTracker.OnTargetChanged -= RefreshSize;
		SpectatableModuleBase.OnAdded -= AddTarget;
		SpectatableModuleBase.OnRemoved -= RemoveTarget;
	}

	private void LateUpdate()
	{
	}

	private void AddTarget(SpectatableModuleBase target)
	{
		int orderPriority = GetOrderPriority(target.MainRole);
		int num = _spawnedTargets.Count;
		for (int i = 0; i < _spawnedTargets.Count; i++)
		{
			if (_spawnedTargets[i].Priority > orderPriority)
			{
				num = i;
				break;
			}
		}
		if (Definitions.TryGetValue(target.ListElementType, out var value) && value.TryGetFromPools(base.transform, out var full, out var compact))
		{
			SpectatableListSpawnedElement spectatableListSpawnedElement = default(SpectatableListSpawnedElement);
			spectatableListSpawnedElement.Priority = orderPriority;
			spectatableListSpawnedElement.Compact = compact;
			spectatableListSpawnedElement.FullSize = full;
			spectatableListSpawnedElement.Target = target;
			SpectatableListSpawnedElement item = spectatableListSpawnedElement;
			SetupNewTarget(item.Compact, target, num * 2);
			SetupNewTarget(item.FullSize, target, num * 2 + 1);
			_spawnedTargets.Insert(num, item);
			RefreshSize();
		}
	}

	private void RemoveTarget(SpectatableModuleBase target)
	{
		for (int i = 0; i < _spawnedTargets.Count; i++)
		{
			if (!(_spawnedTargets[i].Compact.Target != target))
			{
				_spawnedTargets[i].ReturnToPool();
				_spawnedTargets.RemoveAt(i);
				break;
			}
		}
		RefreshSize();
	}

	private void RefreshAllTargets()
	{
		_spawnedTargets.ForEach(delegate(SpectatableListSpawnedElement x)
		{
			x.ReturnToPool();
		});
		_spawnedTargets.Clear();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is ISpectatableRole spectatableRole)
			{
				AddTarget(spectatableRole.SpectatorModule);
			}
		}
	}

	private void SetupNewTarget(SpectatableListElementBase element, SpectatableModuleBase module, int order)
	{
		Transform obj = element.transform;
		obj.localScale = Vector3.one;
		obj.localPosition = Vector3.zero;
		obj.localRotation = Quaternion.identity;
		obj.SetSiblingIndex(order);
		element.Index = order;
		element.Target = module;
	}

	private void RefreshSize()
	{
		int count = _spawnedTargets.Count;
		if (count == 0)
		{
			return;
		}
		float num;
		float num2 = (num = _layoutGroup.spacing * (float)(count - 1));
		for (int i = 0; i < count; i++)
		{
			SpectatableListSpawnedElement spectatableListSpawnedElement = _spawnedTargets[i];
			num += spectatableListSpawnedElement.Compact.Height;
			num2 += spectatableListSpawnedElement.FullSize.Height;
			if (_spawnedTargets[i].Target == SpectatorTargetTracker.CurrentTarget)
			{
				_lastTargetId = i;
			}
		}
		float num3 = Mathf.InverseLerp(num, num2, _targetHeight);
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
			int num5 = Mathf.Clamp(_lastTargetId, 0, count - 1);
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
			_spawnedTargets[j].FullSize.gameObject.SetActive(flag);
			_spawnedTargets[j].Compact.gameObject.SetActive(!flag);
		}
	}

	private static void RefreshKeybinds()
	{
		_nextKey = NewInput.GetKey(ActionName.Shoot);
		_prevKey = NewInput.GetKey(ActionName.Zoom);
	}

	private static int GetOrderPriority(PlayerRoleBase prb)
	{
		int num = (int)prb.Team.GetFaction() * 65535;
		int num2 = (int)prb.Team * 255;
		int roleTypeId = (int)prb.RoleTypeId;
		return num + num2 + roleTypeId;
	}
}
