
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Scenario
{
	public sealed class ManagedObjects
    {
        //----- params -----

        //----- field -----

		private Dictionary<string, object> managedObjects = null;

        //----- property -----

        //----- method -----

		public ManagedObjects()
		{
			managedObjects = new Dictionary<string, object>();
		}
		
		public object[] GetAll()
		{
			return managedObjects.Values.ToArray();
		}

		public void Clear()
		{
			managedObjects.Clear();
		}

		public object Get(string key)
		{
			var managedObject = managedObjects.GetValueOrDefault(key);

			if (managedObject == null)
			{
				using (new DisableStackTraceScope())
				{
					Debug.LogError($"Object not found.\nKey = {key}");
				}
			}

			return managedObject;
		}

		public T Get<T>(string key) where T : class
		{
			var managedObject = Get(key);

			if (managedObject == null) { return null; }
			
			var target = managedObject as T;

			if (target == null)
			{
				var gameObject = managedObject as GameObject;

				if (gameObject != null)
				{
					target = gameObject.GetComponent(typeof(T)) as T;
				}
			}

			if (target == null)
			{
				using (new DisableStackTraceScope())
				{
					Debug.LogError($"Object not match type.\nKey = {key}\nType = { typeof(T).FullName }");
				}
			}

			return target;
		}

		public void Add(string key, object target)
		{
			managedObjects[key] = target;
		}

		public void Remove(string key)
		{
			if (managedObjects.ContainsKey(key))
			{
				managedObjects.Remove(key);
			}
		}

		public void Remove(object target)
		{
			var list = managedObjects.Where(x => x.Value == target).ToArray();

			foreach (var item in list)
			{
				managedObjects.Remove(item.Key);
			}
		}
	}
}