# prefab-pooling
Simple to use Prefab Pooling for Unity.
#### Get an instance from the pool:
```
GameObject prefabInstance = PrefabPooling.Get(prefab);
```
#### Release an instance to the pool:
```
PrefabPooling.Release(prefabInstance);
```
#### Release an instance to the pool after 2 seconds:
```
PrefabPooling.Release(prefabInstance, 2);
```
#### Get an instance from the pool and release it to the pool after 2 seconds:
```
GameObject prefabInstance = PrefabPooling.GetAndRelease(prefab, 2);
```
#### (Optional) Initialize a pool with 10 instances, instantiating 1 instance per frame 1:
```
PrefabPooling.Initialize(prefab, 10, 1);
```
#### Clear all pools:
```
PrefabPooling.Clear()
```
### Notes:
* All instances of a prefab share a pool.
* Pools are uncapped and grow as needed.
* Destroyed istances will be automatically removed from the pool.
