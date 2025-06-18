using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PrefabPooling
{
    public static class PrefabPooling
    {
        // Object pools for GameObjects.
        private static readonly Dictionary<GameObject, GameObjectPool> GameObjectPools = new();
        private static readonly Dictionary<GameObject, GameObject> GameObjectPrefabMap = new();
        private static readonly List<(GameObject, float)> GameObjectInstancesToRelease = new();
        private static readonly List<(GameObject prefab, int count, int perFrame)> GameObjectsToInitialize = new();

        // Object pools for Components.
        private static readonly Dictionary<GameObject, ComponentPool> ComponentPools = new();
        private static readonly Dictionary<Component, GameObject> ComponentPrefabMap = new();
        private static readonly List<(Component, float)> ComponentInstancesToRelease = new();
        private static readonly List<(Component prefab, int count, int perFrame)> ComponentsToInitialize = new();

        private static PlayerLoopSystem m_loopSystem;


        #region GameObject Pooling

        public static GameObject Get(GameObject prefab)
        {
            if (!prefab)
            {
                Debug.LogWarning("Attempted to get a GameObject from a null prefab.");
                return null;
            }

            // If the pool for the prefab doesn't exist, create it.
            if (!GameObjectPools.TryGetValue(prefab, out GameObjectPool pool))
            {
                pool = CreatePool(prefab);
            }

            // Get an instance from the pool and map it to the prefab.
            GameObject instance = pool.Get();
            GameObjectPrefabMap[instance] = prefab.gameObject;

            return instance;
        }

        public static void Release(GameObject instance)
        {
            if (GameObjectPrefabMap.TryGetValue(instance, out GameObject prefab) &&
                GameObjectPools.TryGetValue(prefab, out GameObjectPool pool))
            {
                pool.Release(instance);
            }
            else if (instance)
            {
                Debug.LogWarning($"No pool found for instance: {instance.name}. Destroying instead.");
                Object.Destroy(instance);
            }
            else
            {
                Debug.LogWarning("Attempted to release a null GameObject instance.");
            }
        }

        public static void Release(GameObject instance, float delay)
        {
            if (delay <= 0f && GameObjectPools.TryGetValue(instance, out GameObjectPool pool))
            {
                pool.Release(instance);
                return;
            }

            GameObjectInstancesToRelease.Add((instance, Time.time + delay));
        }

        public static GameObject GetAndRelease(GameObject prefab, float delay)
        {
            GameObject instance = Get(prefab);
            Release(instance, delay);
            return instance;
        }

        public static void Initialize(GameObject prefab, int targetCount, int instantiationsPerFrame = 1)
        {
            if (GetPoolCountAll(prefab) >= targetCount) return;

            if (instantiationsPerFrame <= 0)
            {
                InitializeInternal(prefab, targetCount);
            }
            else
            {
                for (int i = 0; i < GameObjectsToInitialize.Count; i++)
                {
                    var (existingPrefab, count, perFrame) = GameObjectsToInitialize[i];
                    if (existingPrefab != prefab) continue;
                    GameObjectsToInitialize[i] = (
                        existingPrefab,
                        Mathf.Max(count, targetCount),
                        Mathf.Min(perFrame, instantiationsPerFrame));
                    return;
                }

                GameObjectsToInitialize.Add((prefab, targetCount, instantiationsPerFrame));
            }
        }

        private static void InitializeInternal(GameObject prefab, int targetCount)
        {
            if (!GameObjectPools.TryGetValue(prefab, out GameObjectPool pool))
            {
                pool = CreatePool(prefab);
            }

            while (pool.CountAll < targetCount)
            {
                GetAndRelease(prefab, 0f).gameObject.SetActive(false);
            }
        }

        private static GameObjectPool CreatePool(GameObject prefab)
        {
            GameObjectPool pool = new(prefab);
            GameObjectPools.Add(prefab, pool);
            return pool;
        }

        public static int GetPoolCountAll(GameObject prefab)
        {
            return GameObjectPools.TryGetValue(prefab, out GameObjectPool pool) ? pool.CountAll : 0;
        }

        #endregion


        #region Component Pooling

        public static T Get<T>(T prefab) where T : Component
        {
            if (!prefab)
            {
                Debug.LogWarning("Attempted to get a Component from a null prefab.");
                return null;
            }

            // If the pool for the prefab doesn't exist, create it.
            if (!ComponentPools.TryGetValue(prefab.gameObject, out ComponentPool pool))
            {
                pool = CreatePool(prefab);
            }

            // Get an instance from the pool and map it to the prefab.
            Component instance = pool.Get();
            ComponentPrefabMap[instance] = prefab.gameObject;

            return instance as T;
        }

        public static T GetAndRelease<T>(T prefab, float delay) where T : Component
        {
            T instance = Get(prefab);
            Release(instance, delay);
            return instance;
        }

        public static void Release<T>(T instance) where T : Component
        {
            if (ComponentPrefabMap.TryGetValue(instance, out GameObject prefab) &&
                ComponentPools.TryGetValue(prefab, out ComponentPool pool))
            {
                pool.Release(instance);
            }
            else if (instance)
            {
                Debug.LogWarning($"No pool found for instance: {instance.name}. Destroying instead.");
                Object.Destroy(instance.gameObject);
            }
            else
            {
                Debug.LogWarning("Attempted to release a null Component instance.");
            }
        }

        public static void Release<T>(T instance, float delay) where T : Component
        {
            if (delay <= 0f && ComponentPools.TryGetValue(instance.gameObject, out ComponentPool pool))
            {
                pool.Release(instance);
            }

            ComponentInstancesToRelease.Add((instance, Time.time + delay));
        }

        public static void Initialize(Component prefab, int targetCount, int instantiationsPerFrame = 0)
        {
            if (GetPoolCountAll(prefab) >= targetCount) return;

            if (instantiationsPerFrame <= 0)
            {
                InitializeInternal(prefab, targetCount);
            }
            else
            {
                for (int i = 0; i < ComponentsToInitialize.Count; i++)
                {
                    var (existingPrefab, count, perFrame) = ComponentsToInitialize[i];
                    if (existingPrefab != prefab) continue;
                    ComponentsToInitialize[i] = (
                        existingPrefab,
                        Mathf.Max(count, targetCount),
                        Mathf.Min(perFrame, instantiationsPerFrame));
                    return;
                }

                ComponentsToInitialize.Add((prefab, targetCount, instantiationsPerFrame));
            }
        }

        private static void InitializeInternal(Component prefab, int targetCount)
        {
            if (!ComponentPools.TryGetValue(prefab.gameObject, out ComponentPool pool))
            {
                pool = CreatePool(prefab);
            }

            while (pool.CountAll < targetCount)
            {
                GetAndRelease(prefab, 0f).gameObject.SetActive(false);
            }
        }

        private static ComponentPool CreatePool<T>(T prefab) where T : Component
        {
            ComponentPool pool = new ComponentPool(prefab);
            ComponentPools.Add(prefab.gameObject, pool);
            return pool;
        }

        public static int GetPoolCountAll(Component prefab)
        {
            return ComponentPools.TryGetValue(prefab.gameObject, out ComponentPool pool) ? pool.CountAll : 0;
        }

        #endregion


        public static void Clear()
        {
            foreach (ComponentPool pool in ComponentPools.Values)
            {
                pool.Clear();
            }

            ComponentPools.Clear();
            ComponentPrefabMap.Clear();
            ComponentInstancesToRelease.Clear();
            ComponentsToInitialize.Clear();

            foreach (GameObjectPool pool in GameObjectPools.Values)
            {
                pool.Clear();
            }

            GameObjectPools.Clear();
            GameObjectPrefabMap.Clear();
            GameObjectInstancesToRelease.Clear();
            GameObjectsToInitialize.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void InitializePrefabPooling()
        {
            // Set up the player loop system.
            // Grab the current player loop and insert our custom system.
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            m_loopSystem = new PlayerLoopSystem()
            {
                type = typeof(PrefabPooling),
                updateDelegate = Update,
                subSystemList = null
            };
            PlayerLoopUtility.InsertSystem<Update>(ref currentPlayerLoop, in m_loopSystem, 0);
            // Set the modified player loop back to the system.
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);

#if UNITY_EDITOR
            // Prevent double subscription. 
            EditorApplication.playModeStateChanged -= OnPlayModeState;
            EditorApplication.playModeStateChanged += OnPlayModeState;

            static void OnPlayModeState(PlayModeStateChange state)
            {
                // In Editor, we need to ensure the loop system is removed when exiting play mode.
                if (state != PlayModeStateChange.ExitingPlayMode) return;
                PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                PlayerLoopUtility.RemoveSystem<Update>(ref currentPlayerLoop, in m_loopSystem);
                PlayerLoop.SetPlayerLoop(currentPlayerLoop);

                // Clean static data to avoid memory leaks.
                Clear();
            }
#endif
        }

        private static void Update()
        {
            // Release any GameObject instances that are ready to be released.
            for (int i = GameObjectInstancesToRelease.Count - 1; i >= 0; i--)
            {
                (GameObject instance, float releaseTime) = GameObjectInstancesToRelease[i];
                if (!instance)
                {
                    GameObjectInstancesToRelease.RemoveAt(i);
                    continue;
                }

                if (!(releaseTime <= Time.time)) continue;
                Release(instance);
                GameObjectInstancesToRelease.RemoveAt(i);
            }

            // Release any Component instances that are ready to be released.
            for (int i = ComponentInstancesToRelease.Count - 1; i >= 0; i--)
            {
                (Component instance, float releaseTime) = ComponentInstancesToRelease[i];
                if (!instance)
                {
                    ComponentInstancesToRelease.RemoveAt(i);
                    continue;
                }

                if (!(releaseTime <= Time.time)) continue;
                Release(instance);
                ComponentInstancesToRelease.RemoveAt(i);
            }

            // Add instances to GameObject pools
            for (int i = GameObjectsToInitialize.Count - 1; i >= 0; i--)
            {
                var (prefab, targetCount, perFrame) = GameObjectsToInitialize[i];
                int currentCount = GetPoolCountAll(prefab);
                int instancesToCreate = Mathf.Min(perFrame, targetCount - currentCount);
                if (instancesToCreate <= 0)
                {
                    GameObjectsToInitialize.RemoveAt(i);
                    continue;
                }

                InitializeInternal(prefab, currentCount + instancesToCreate);
            }

            // Add instances to Component pools
            for (int i = ComponentsToInitialize.Count - 1; i >= 0; i--)
            {
                var (prefab, targetCount, perFrame) = ComponentsToInitialize[i];
                int currentCount = GetPoolCountAll(prefab);
                int instancesToCreate = Mathf.Min(perFrame, targetCount - currentCount);
                if (instancesToCreate <= 0)
                {
                    ComponentsToInitialize.RemoveAt(i);
                    continue;
                }

                InitializeInternal(prefab, currentCount + instancesToCreate);
            }
        }
    }
}