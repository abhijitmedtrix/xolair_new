using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// It is relatively expensive to instantiate new objects so reuse the objects when possible by placing them in a pool.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    // Static variables
    private static ObjectPool s_Instance;

    private static ObjectPool Instance
    {
        get
        {
#if UNITY_EDITOR
            if (!m_Initialized)
            {
                Debug.LogWarning(
                    "Warning: ObjectPool is null. A GameObject has been created with the component automatically added. Please run Scene Setup from the Start Window.");
                s_Instance = new GameObject("ObjectPool").AddComponent<ObjectPool>();
            }
#endif
            return s_Instance;
        }
    }

    // Internal variables
#if UNITY_EDITOR
    private static bool m_Initialized;
#endif
    private Dictionary<int, Stack<GameObject>> m_GameObjectPool = new Dictionary<int, Stack<GameObject>>();
    private Dictionary<int, int> m_InstantiatedGameObjects = new Dictionary<int, int>();
    private Dictionary<Type, object> m_GenericPool = new Dictionary<Type, object>();

    /// <summary>
    /// Assign the static variables and register for any events that the pool should be aware of.
    /// </summary>
    private void OnEnable()
    {
        s_Instance = this;
#if UNITY_EDITOR
        m_Initialized = true;
#endif
    }

    /// <summary>
    /// Instantiate a new GameObject. Use the object pool if a previously used GameObject is located in the pool, otherwise instaniate a new GameObject.
    /// </summary>
    /// <param name="original">The original GameObject to pooled a copy of.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <returns>The pooled/instantiated GameObject.</returns>
    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
    {
        return Instantiate(original, position, rotation, null);
    }

    /// <summary>
    /// Spawn a new GameObject on the server and persist to the clients. Use the object pool if a previously used GameObject is located in the pool, otherwise instaniate a new GameObject.
    /// </summary>
    /// <param name="original">The original GameObject to pooled a copy of.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <returns>The pooled/instantiated GameObject.</returns>
    public static GameObject Spawn(GameObject original, Vector3 position, Quaternion rotation)
    {
        return Spawn(original, position, rotation, null);
    }

    /// <summary>
    /// Instantiate a new GameObject. Use the object pool if a previously used GameObject is located in the pool, otherwise instaniate a new GameObject.
    /// </summary>
    /// <param name="original">The original GameObject to pooled a copy of.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <param name="parent">The parent to assign to the pooled GameObject.</param>
    /// <returns>The pooled/instantiated GameObject.</returns>
    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
    {
        return Instance.InstantiateInternal(original, position, rotation, parent, false);
    }

    /// <summary>
    /// Spawn a new GameObject on the server and persist to the clients. Use the object pool if a previously used GameObject is located in the pool, otherwise instaniate a new GameObject.
    /// </summary>
    /// <param name="original">The original GameObject to pooled a copy of.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <param name="parent">The parent to assign to the pooled GameObject.</param>
    /// <returns>The pooled/instantiated GameObject.</returns>
    public static GameObject Spawn(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
    {
        return Instance.InstantiateInternal(original, position, rotation, parent, true);
    }

    /// <summary>
    /// Internal method to instantiate a new GameObject. Use the object pool if a previously used GameObject is located in the pool, otherwise instaniate a new GameObject.
    /// </summary>
    /// <param name="original">The original GameObject to pooled a copy of.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <param name="parent">The parent to assign to the pooled GameObject.</param>
    /// <param name="networkSpawn">Should the object be spawned on the server and persisted across clients?</param>
    /// <returns>The pooled/instantiated GameObject.</returns>
    private GameObject InstantiateInternal(GameObject original, Vector3 position, Quaternion rotation, Transform parent,
        bool networkSpawn)
    {
        var originalInstanceID = original.GetInstanceID();
        var instantiatedObject = ObjectFromPool(originalInstanceID, position, rotation, parent, networkSpawn);
        if (instantiatedObject == null)
        {
            instantiatedObject = (GameObject) GameObject.Instantiate(original, position, rotation);
            instantiatedObject.transform.SetParent(parent, false);
            // Map the newly instantiated instance ID to the original instance ID so when the object is returned it knows what pool to go to.
            m_InstantiatedGameObjects.Add(instantiatedObject.GetInstanceID(), originalInstanceID);
        }
        instantiatedObject.GetComponent<IInitiable>()?.Init();
        return instantiatedObject;
    }

    /// <summary>
    /// An object is trying to be popped from the object pool. Return the pooled object if it exists otherwise null meaning one needs to be insantiated.
    /// </summary>
    /// <param name="originalInstanceID">The instance id of the GameObject trying to be popped from the pool.</param>
    /// <param name="position">The position of the pooled GameObject.</param>
    /// <param name="rotation">The rotation of the pooled Gameobject.</param>
    /// <param name="parent">The parent to assign to the pooled GameObject.</param>
    /// <param name="networkSpawn">Should the object be spawned on the server and persisted across clients?</param>
    /// <returns>The pooled GameObject.</returns>
    private GameObject ObjectFromPool(int originalInstanceID, Vector3 position, Quaternion rotation, Transform parent,
        bool networkSpawn)
    {
        Stack<GameObject> pool;
        if (m_GameObjectPool.TryGetValue(originalInstanceID, out pool))
        {
            if (pool.Count > 0)
            {
                var instantiatedObject = pool.Pop();
                instantiatedObject.transform.position = position;
                instantiatedObject.transform.rotation = rotation;
                instantiatedObject.transform.SetParent(parent);
                instantiatedObject.SetActive(true);
                // Map the newly instantiated instance ID to the original instance ID so when the object is returned it knows what pool to go to.
                m_InstantiatedGameObjects.Add(instantiatedObject.GetInstanceID(), originalInstanceID);
                return instantiatedObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Return if the object was instantiated with the ObjectPool.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to check to see if it was instantiated with the ObjectPool.</param>
    /// <returns>True if the object was instantiated with the ObjectPool.</returns>
    public static bool SpawnedWithPool(GameObject instantiatedObject)
    {
        return Instance.SpawnedWithPoolInternal(instantiatedObject);
    }

    /// <summary>
    /// Internal method to return if the object was instantiated with the ObjectPool.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to check to see if it was instantiated with the ObjectPool.</param>
    /// <returns>True if the object was instantiated with the ObjectPool.</returns>
    private bool SpawnedWithPoolInternal(GameObject instantiatedObject)
    {
        return m_InstantiatedGameObjects.ContainsKey(instantiatedObject.GetInstanceID());
    }

    /// <summary>
    /// Return the instance ID of the prefab used to spawn the instantiated object.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to get the original instance ID</param>
    /// <returns>The original instance ID</returns>
    public static int OriginalInstanceID(GameObject instantiatedObject)
    {
        return Instance.OriginalInstanceIDInternal(instantiatedObject);
    }

    /// <summary>
    /// Internal method to return the instance ID of the prefab used to spawn the instantiated object.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to get the original instance ID</param>
    /// <returns>The original instance ID</returns>
    private int OriginalInstanceIDInternal(GameObject instantiatedObject)
    {
        var instantiatedInstanceID = instantiatedObject.GetInstanceID();
        var originalInstanceID = -1;
        if (!m_InstantiatedGameObjects.TryGetValue(instantiatedInstanceID, out originalInstanceID))
        {
            Debug.LogError("Unable to get the original instance ID of " + instantiatedObject +
                           ": has the object already been placed in the ObjectPool?");
            return -1;
        }

        return originalInstanceID;
    }

    /// <summary>
    /// Return the specified GameObject back to the ObjectPool.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to return to the pool.</param>
    public static void Destroy(GameObject instantiatedObject)
    {
        // Objects may be wanting to be destroyed as the game is stopping but the ObjectPool has already been destroyed. Ensure the ObjectPool is still valid.
        if (Instance == null)
        {
            return;
        }

        Instance.DestroyInternal(instantiatedObject);
    }

    /// <summary>
    /// Internal method to return the specified GameObject back to the ObjectPool. Call the corresponding server or client method.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to return to the pool.</param>
    private void DestroyInternal(GameObject instantiatedObject)
    {
        var instantiatedInstanceID = instantiatedObject.GetInstanceID();
        var originalInstanceID = -1;
        if (!m_InstantiatedGameObjects.TryGetValue(instantiatedInstanceID, out originalInstanceID))
        {
            Debug.LogError("Unable to pool " + instantiatedObject + " (instance " + instantiatedInstanceID +
                           "): the GameObject was not instantiated with ObjectPool.Instantiate " + Time.time);
            return;
        }

        // Map the instantiated instance ID back to the orignal instance ID so the GameObject can be returned to the correct pool.
        m_InstantiatedGameObjects.Remove(instantiatedInstanceID);

        DestroyLocal(instantiatedObject, originalInstanceID);
    }

    /// <summary>
    /// Return the specified GameObject back to the ObjectPool.
    /// </summary>
    /// <param name="instantiatedObject">The GameObject to return to the pool.</param>
    /// <param name="originalInstanceID">The instance ID of the original GameObject.</param>
    private void DestroyLocal(GameObject instantiatedObject, int originalInstanceID)
    {
        // This GameObject may have a collider and that collider may be ignoring the collision with other colliders. Revert this setting because the object is going
        // back into the pool.
        /*
        Collider instantiatedObjectCollider;
        if ((instantiatedObjectCollider = Utility.GetComponentForType<Collider>(instantiatedObject)) != null) {
            LayerManager.RevertCollision(instantiatedObjectCollider);
        }
        */
        IDisposable dis = instantiatedObject.GetComponent<IDisposable>();
        if (dis != null)
        {
            dis.Dispose();
            // Debug.Log($"object <{instantiatedObject}> been disposed");
        }

        instantiatedObject.SetActive(false);
        instantiatedObject.transform.SetParent(transform);
        Stack<GameObject> pool;
        if (m_GameObjectPool.TryGetValue(originalInstanceID, out pool))
        {
            pool.Push(instantiatedObject);
        }
        else
        {
            // The pool for this GameObject type doesn't exist yet so it has to be created.
            pool = new Stack<GameObject>();
            pool.Push(instantiatedObject);
            m_GameObjectPool.Add(originalInstanceID, pool);
        }
    }

    /// <summary>
    /// Get a pooled object of the specified type using a generic ObjectPool.
    /// </summary>
    /// <typeparam name="T">The type of object to get.</typeparam>
    /// <returns>A pooled object of type T.</returns>
    public static T Get<T>()
    {
        return Instance.GetInternal<T>();
    }

    /// <summary>
    /// Internal method to get a pooled object of the specified type using a generic ObjectPool.
    /// </summary>
    /// <typeparam name="T">The type of object to get.</typeparam>
    /// <returns>A pooled object of type T.</returns>
    private T GetInternal<T>()
    {
        object value;
        if (m_GenericPool.TryGetValue(typeof(T), out value))
        {
            var pooledObjects = value as Stack<T>;
            if (pooledObjects.Count > 0)
            {
                return pooledObjects.Pop();
            }
        }

        return Activator.CreateInstance<T>();
    }

    /// <summary>
    /// Return the object back to the generic object pool.
    /// </summary>
    /// <typeparam name="T">The type of object to return.</typeparam>
    /// <param name="obj">The object to return.</param>
    public static void Return<T>(T obj)
    {
        Instance.ReturnInternal<T>(obj);
    }

    /// <summary>
    /// Internal method to return the object back to the generic object pool.
    /// </summary>
    /// <typeparam name="T">The type of object to return.</typeparam>
    /// <param name="obj">The object to return.</param>
    private void ReturnInternal<T>(T obj)
    {
        object value;
        if (m_GenericPool.TryGetValue(typeof(T), out value))
        {
            var pooledObjects = value as Stack<T>;
            pooledObjects.Push(obj);
        }
        else
        {
            var pooledObjects = new Stack<T>();
            pooledObjects.Push(obj);
            m_GenericPool.Add(typeof(T), pooledObjects);
        }
    }
}