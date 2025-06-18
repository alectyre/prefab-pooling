using System.Collections.Generic;
using UnityEngine;

namespace PrefabPooling
{
    /// <summary>
    /// ObjectPool for prefabs that works with GameObjects.
    /// Retrieved instances are set active. Released instances are set inactive.
    /// Handles extraneously destroyed instances.
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject m_prefab;
        private readonly HashSet<GameObject> m_allInstanceList = new ();
        private readonly HashSet<GameObject> m_pooledInstanceList = new ();
        private readonly Queue<GameObject> m_pooledInstances = new ();
        
        public int CountAll => m_allInstanceList.Count;
        
        public GameObjectPool(GameObject prefab)
        {
            m_prefab = prefab;
        }
        
        public GameObject Get()
        {
            while (m_pooledInstances.Count != 0 )
            {
                GameObject instance = m_pooledInstances.Dequeue();
                if (!instance)
                {
                    m_allInstanceList.RemoveWhere(item => !item);
                    m_pooledInstanceList.RemoveWhere(item => !item);
                    continue;
                }
                m_pooledInstanceList.Remove(instance);
                instance.SetActive(true);
                
                return instance;
            }
            
            GameObject newInstance = Object.Instantiate(m_prefab);
            newInstance.transform.name = $"{m_prefab.name} (Pooled)";
            m_allInstanceList.Add(newInstance);
            newInstance.SetActive(true);
            
            return newInstance;
        }
        
        public void Release(GameObject instance)
        {
            if (!instance ||
                !m_allInstanceList.Contains(instance) ||
                m_pooledInstanceList.Contains(instance)) return;
            
            instance.SetActive(false);
            instance.transform.localScale = m_prefab.transform.localScale;
            instance.transform.SetPositionAndRotation(m_prefab.transform.position, m_prefab.transform.rotation);
            
            m_pooledInstances.Enqueue(instance);
            m_pooledInstanceList.Add(instance);
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
