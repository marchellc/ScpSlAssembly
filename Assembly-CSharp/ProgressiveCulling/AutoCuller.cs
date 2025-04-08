using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProgressiveCulling
{
	[Serializable]
	public class AutoCuller
	{
		public void Generate(GameObject root, Predicate<GameObject> cullFilter = null, Predicate<GameObject> deactivationRule = null, bool ignoreDoNotCullTag = false)
		{
			this._cullableGameObjects.Clear();
			this._cullableRenderers.Clear();
			this._cullableBehaviors.Clear();
			this.ProcessGameObject(root, cullFilter, deactivationRule, !ignoreDoNotCullTag);
		}

		public void SetVisibility(bool isVisible)
		{
			for (int i = this._cullableGameObjects.Count - 1; i >= 0; i--)
			{
				GameObject gameObject = this._cullableGameObjects[i];
				if (gameObject == null)
				{
					this._cullableGameObjects.RemoveAt(i);
				}
				else
				{
					gameObject.SetActive(isVisible);
				}
			}
			for (int j = this._cullableBehaviors.Count - 1; j >= 0; j--)
			{
				Behaviour behaviour = this._cullableBehaviors[j];
				if (behaviour == null)
				{
					this._cullableBehaviors.RemoveAt(j);
				}
				else
				{
					behaviour.enabled = isVisible;
				}
			}
			for (int k = this._cullableRenderers.Count - 1; k >= 0; k--)
			{
				Renderer renderer = this._cullableRenderers[k];
				if (renderer == null)
				{
					this._cullableRenderers.RemoveAt(k);
				}
				else
				{
					renderer.enabled = isVisible;
				}
			}
		}

		private void ProcessGameObject(GameObject root, Predicate<GameObject> cullFilter, Predicate<GameObject> deactivationFilter, bool checkTag)
		{
			if (cullFilter != null && !cullFilter(root))
			{
				return;
			}
			if (checkTag && root.CompareTag("DoNotCull"))
			{
				return;
			}
			IAutoCullerOverrideComponent autoCullerOverrideComponent;
			if (root.TryGetComponent<IAutoCullerOverrideComponent>(out autoCullerOverrideComponent) && !autoCullerOverrideComponent.AllowAutoCulling)
			{
				return;
			}
			if (deactivationFilter != null && deactivationFilter(root))
			{
				this._cullableGameObjects.Add(root);
				return;
			}
			Transform transform = root.transform;
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				GameObject gameObject = transform.GetChild(i).gameObject;
				this.ProcessGameObject(gameObject, cullFilter, deactivationFilter, checkTag);
			}
			foreach (Component component in root.GetComponents<Component>())
			{
				Renderer renderer = component as Renderer;
				if (renderer != null)
				{
					this._cullableRenderers.Add(renderer);
				}
				Behaviour behaviour = component as Behaviour;
				if (behaviour != null && (component is Light || component is Canvas || component is CanvasScaler || component is UIBehaviour || component is ReflectionProbe))
				{
					this._cullableBehaviors.Add(behaviour);
				}
			}
		}

		[SerializeField]
		private List<GameObject> _cullableGameObjects = new List<GameObject>();

		[SerializeField]
		private List<Renderer> _cullableRenderers = new List<Renderer>();

		[SerializeField]
		private List<Behaviour> _cullableBehaviors = new List<Behaviour>();

		public const string DoNotCullTag = "DoNotCull";
	}
}
