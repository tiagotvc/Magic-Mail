// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SweetSugar.Scripts.System.Pool
{
    /// <summary>
    /// object pool. Uses to keep and activate items and effects
    /// </summary>
    [Serializable]
    public class ObjectPoolItem
    {
        public GameObject objectToPool;
        public string poolName;
        public int amountToPool;
        public bool shouldExpand = true;
        public bool inEditor = true;
    }
    
    /// <summary>
    /// Structure to store pooled object information
    /// </summary>
    [Serializable]
    public class PooledObject
    {
        public GameObject gameObject;
        public string tag;
        public Item itemComponent;
        
        public PooledObject(GameObject obj, string tag)
        {
            this.gameObject = obj;
            this.tag = tag;
            this.itemComponent = obj.GetComponent<Item>();
        }
    }

    [ExecuteInEditMode]
    public class ObjectPooler : MonoBehaviour
    {
        public const string DefaultRootObjectPoolName = "Pooled Objects";

        public static ObjectPooler Instance;
        public string rootPoolName = DefaultRootObjectPoolName;
        
        // Store pooled objects directly rather than through PoolBehaviour
        private List<PooledObject> pooledObjects = new List<PooledObject>();
        private List<ObjectPoolItem> itemsToPool;
        private PoolerScriptable PoolSettings;

        // Cache for frequent object types
        private Dictionary<string, List<PooledObject>> poolByTag = new Dictionary<string, List<PooledObject>>();
        
        // Cache for parent pool objects to avoid using Find
        private Dictionary<string, GameObject> parentPoolObjects = new Dictionary<string, GameObject>();

        void OnEnable()
        {
            LoadFromScriptable();
            Instance = this;
            
            // Initialize the root pool object
            if (!parentPoolObjects.ContainsKey(rootPoolName))
            {
                var rootObject = new GameObject(rootPoolName);
                parentPoolObjects[rootPoolName] = rootObject;
            }
        }

        private void LoadFromScriptable()
        {
            PoolSettings = Resources.Load("Scriptable/PoolSettings") as PoolerScriptable;
            itemsToPool = PoolSettings.itemsToPool;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            ClearNullElements();
            RebuildPoolDictionary();

            foreach (var item in itemsToPool)
            {
                if (item == null) continue;
                if (item.objectToPool == null) continue;
                var pooledCount = pooledObjects.Count(i => i.tag == item.objectToPool.name);
                for (int i = 0; i < item.amountToPool - pooledCount; i++)
                {
                    CreatePooledObject(item);
                }
            }
        }

        private void ClearNullElements()
        {
            pooledObjects.RemoveAll(i => i.gameObject == null);
        }
        
        private void RebuildPoolDictionary()
        {
            poolByTag.Clear();
            foreach (var obj in pooledObjects)
            {
                if (obj.gameObject == null) continue;
                
                if (!poolByTag.TryGetValue(obj.tag, out var list))
                {
                    list = new List<PooledObject>();
                    poolByTag[obj.tag] = list;
                }
                
                list.Add(obj);
            }
        }

        private GameObject GetParentPoolObject(string objectPoolName)
        {
            // Use the cached parent pool object if it exists
            if (parentPoolObjects.TryGetValue(objectPoolName, out GameObject parentObject) && parentObject != null)
            {
                return parentObject;
            }
            
            // Create the parent object if necessary
            parentObject = new GameObject();
            parentObject.name = objectPoolName;

            // Add sub pools to the root object pool if necessary
            if (objectPoolName != rootPoolName)
            {
                // Make sure we have the root object
                if (!parentPoolObjects.TryGetValue(rootPoolName, out GameObject rootObject) || rootObject == null)
                {
                    rootObject = new GameObject(rootPoolName);
                    parentPoolObjects[rootPoolName] = rootObject;
                }
                
                parentObject.transform.parent = transform;
            }
            
            // Cache the new parent object
            parentPoolObjects[objectPoolName] = parentObject;
            
            return parentObject;
        }

        public void HideObjects(string tag)
        {
            if (poolByTag.TryGetValue(tag, out var objectList))
            {
                foreach (var item in objectList)
                {
                    if (item.gameObject != null)
                        item.gameObject.SetActive(false);
                }
            }
        }

        public void PutBack(GameObject obj)
        {
            if (LevelManager.THIS.DebugSettings.FallingLog) DebugLogKeeper.Log(obj + " pooled", DebugLogKeeper.LogType.Falling);
            obj.SetActive(false);
            
            // Find the object in our pool to access cached references
            var pooledObj = pooledObjects.Find(p => p.gameObject == obj);
            
            // Reset object to initial state if it's an Item
            if (pooledObj != null && pooledObj.itemComponent != null)
            {
                if(pooledObj.itemComponent.transform.childCount > 0)
                    pooledObj.itemComponent.transform.GetChild(0).localScale = Vector3.one;
            }
            // Fallback to GetComponent if not found in our pool
            else
            {
                var itemComponent = obj.GetComponent<Item>();
                if (itemComponent != null && itemComponent.transform.childCount > 0)
                    itemComponent.transform.GetChild(0).localScale = Vector3.one;
            }
        }

        public GameObject GetPooledObject(string tag, Object activatedBy=null, bool active = true, bool canBeActive = false)
        {
            // Try to use the cached dictionary first for better performance
            if (!poolByTag.TryGetValue(tag, out var taggedObjects))
            {
                // If dictionary is missing this tag, rebuild it
                ClearNullElements();
                RebuildPoolDictionary();
                
                if (!poolByTag.TryGetValue(tag, out taggedObjects))
                {
                    taggedObjects = new List<PooledObject>();
                }
            }

            PooledObject pooledObj = null;
            
            // Look for available object in the tag-specific list
            foreach (var poolObject in taggedObjects)
            {
                if (poolObject.gameObject == null) continue;
                
                if ((!poolObject.gameObject.activeSelf || canBeActive))
                {
                    // Use cached itemComponent instead of GetComponent
                    if (poolObject.itemComponent != null)
                    {
                        if (poolObject.itemComponent.canBePooled)
                        {
                            pooledObj = poolObject;
                            break;
                        }
                    }
                    else
                    {
                        pooledObj = poolObject;
                        break;
                    }
                }
            }

            if (itemsToPool == null) LoadFromScriptable();
            if (pooledObj == null)
            {
                foreach (var item in itemsToPool)
                {
                    if (item != null && item.objectToPool == null) continue;
                    if (item.objectToPool.name == tag)
                    {
                        if (item.shouldExpand)
                        {
                            pooledObj = CreatePooledObject(item);
                            
                            // Add to the tag-specific list
                            if (!poolByTag.TryGetValue(tag, out var list))
                            {
                                list = new List<PooledObject>();
                                poolByTag[tag] = list;
                            }
                            list.Add(pooledObj);
                            
                            break;
                        }
                    }
                }
            }
            
            if (LevelManager.THIS.DebugSettings.FallingLog) DebugLogKeeper.Log(pooledObj?.gameObject + " unpooled by " + activatedBy, DebugLogKeeper.LogType.Falling);

            if (pooledObj != null)
            {
                pooledObj.gameObject.SetActive(active);
                return pooledObj.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Gets a pooled object and returns the requested component instead of the GameObject
        /// </summary>
        public T GetPooledObjectComponent<T>(string tag, Object activatedBy = null, bool active = true, bool canBeActive = false) where T : Component
        {
            GameObject obj = GetPooledObject(tag, activatedBy, active, canBeActive);
            if (obj != null)
            {
                // Find the cached component if it's an Item component we're looking for
                if (typeof(T) == typeof(Item))
                {
                    var pooledObj = pooledObjects.Find(p => p.gameObject == obj);
                    if (pooledObj?.itemComponent != null)
                    {
                        return pooledObj.itemComponent as T;
                    }
                }
                
                // Otherwise use GetComponent
                T component = obj.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogWarning($"Pooled object '{tag}' doesn't have component of type {typeof(T).Name}");
                }
                return component;
            }
            return null;
        }

        private PooledObject CreatePooledObject(ObjectPoolItem item)
        {
            GameObject obj = Instantiate(item.objectToPool);
            
            // Get the parent for this pooled object and assign the new object to it
            var parentPoolObject = GetParentPoolObject(item.poolName);
            obj.transform.parent = parentPoolObject.transform;
            
            // Create our pooled object structure instead of adding a component
            var pooledObj = new PooledObject(obj, item.objectToPool.name);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.AutomatedAction);
            }
#endif

            obj.SetActive(false);
            pooledObjects.Add(pooledObj);

            return pooledObj;
        }

        public void DestroyObjects(string tag)
        {
            for (int i = pooledObjects.Count - 1; i >= 0; i--)
            {
                if (pooledObjects[i].tag == tag)
                {
                    if (pooledObjects[i].gameObject != null)
                        DestroyImmediate(pooledObjects[i].gameObject);
                    pooledObjects.RemoveAt(i);
                }
            }
            
            ClearNullElements();
            RebuildPoolDictionary();
        }
        
        // Clean up any null references when destroying the pool
        private void OnDestroy()
        {
            // Clean up parent pool objects dictionary to avoid memory leaks
            foreach (var key in parentPoolObjects.Keys.ToList())
            {
                if (!parentPoolObjects[key])
                {
                    parentPoolObjects.Remove(key);
                }
            }
        }
    }
}