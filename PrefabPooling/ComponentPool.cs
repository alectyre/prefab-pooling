using System.Collections.Generic;
using UnityEngine;

namespace PrefabPooling
{
    /// <summary>
    /// ObjectPool for prefabs that directly returns instances of a Component type.
    /// Retrieved instances are set active. Released instances are set inactive.
    /// Handles extraneously destroyed instances.
    /// </summary>
    public class ComponentPool
    {
        private readonly Component m_prefab;
        private readonly HashSet<GameObject> m_allInstanceList = new ();
        private readonly HashSet<GameObject> m_pooledInstanceList = new ();
        private readonly Queue<Component> m_pooledInstances = new ();
        
        public int CountAll => m_allInstanceList.Count;
        
        public ComponentPool(Component prefab)
        {
            m_prefab = prefab;
        }
        
        public Component Get()
        {
            while (m_pooledInstances.Count != 0 )
            {
                Component instance = m_pooledInstances.Dequeue();
                if (!instance)
                {
                    m_allInstanceList.RemoveWhere(item => !item);
                    m_pooledInstanceList.RemoveWhere(item => !item);
                    continue;
                }
                m_pooledInstanceList.Remove(instance.gameObject);
                instance.gameObject.SetActive(true);
                
                return instance;
            }
            
            Component newInstance = Object.Instantiate(m_prefab);
            newInstance.transform.name = $"{m_prefab.name} (Pooled)";
            m_allInstanceList.Add(newInstance.gameObject);
            newInstance.gameObject.SetActive(true);
            
            return newInstance;
        }
        
        public void Release(Component instance)
        {
            if (!instance ||
                !m_allInstanceList.Contains(instance.gameObject) ||
                m_pooledInstanceList.Contains(instance.gameObject)) return;
            
            Transform prefabTransform = m_prefab.transform;
            instance.gameObject.SetActive(false);
            instance.transform.localScale = m_prefab.transform.localScale;
            instance.transform.SetPositionAndRotation(prefabTransform.position, prefabTransform.rotation);
            
            m_pooledInstances.Enqueue(instance);
            m_pooledInstanceList.Add(instance.gameObject);
        }
        
        public void Clear()
        {
            foreach (GameObject instance in m_allInstanceList)
            {
                if (!instance) continue;
                Object.Destroy(instance);
            }
            m_pooledInstances.Clear();
            m_pooledInstanceList.Clear();
        }
    }
}