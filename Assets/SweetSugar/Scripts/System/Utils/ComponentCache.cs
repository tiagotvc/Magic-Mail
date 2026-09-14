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
using System.Reflection;
using UnityEngine;

namespace SweetSugar.Scripts.System.Utils
{
    /// <summary>
    /// Utility class that provides component caching functionality to avoid repeated GetComponent calls
    /// </summary>
    public static class ComponentCache
    {
        // Dictionary to store cached components for each GameObject
        private static readonly Dictionary<GameObject, Dictionary<Type, Component>> CachedComponents =
            new Dictionary<GameObject, Dictionary<Type, Component>>();
            
        // Dictionary to store cached component arrays for each GameObject
        private static readonly Dictionary<GameObject, Dictionary<Type, Component[]>> CachedComponentArrays =
            new Dictionary<GameObject, Dictionary<Type, Component[]>>();
            
        // Flag to track if scene has changed and cache should be refreshed
        private static bool sceneCacheDirty = true;
            
        /// <summary>
        /// Gets a component of type T from the specified GameObject with caching to avoid performance overhead
        /// </summary>
        /// <typeparam name="T">The type of component to get</typeparam>
        /// <param name="gameObject">The GameObject to get the component from</param>
        /// <returns>The cached component or null if it doesn't exist</returns>
        public static T GetCachedComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;
                
            Type componentType = typeof(T);
            
            // Initialize dictionary for this GameObject if it doesn't exist
            if (!CachedComponents.TryGetValue(gameObject, out Dictionary<Type, Component> components))
            {
                components = new Dictionary<Type, Component>();
                CachedComponents[gameObject] = components;
            }
            
            // Try to get component from cache, otherwise get it and cache it
            if (!components.TryGetValue(componentType, out Component component) || component == null)
            {
                component = gameObject.GetComponent<T>();
                if (component != null)
                {
                    components[componentType] = component;
                }
            }
            
            return component as T;
        }
        
        /// <summary>
        /// Gets a component of type T from the specified MonoBehaviour's GameObject with caching
        /// </summary>
        /// <typeparam name="T">The type of component to get</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour whose GameObject to get the component from</param>
        /// <returns>The cached component or null if it doesn't exist</returns>
        public static T GetCachedComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour != null ? monoBehaviour.gameObject.GetCachedComponent<T>() : null;
        }
        
        /// <summary>
        /// Adds a component of type T to the specified GameObject if it doesn't exist, and caches it
        /// </summary>
        /// <typeparam name="T">The type of component to add</typeparam>
        /// <param name="gameObject">The GameObject to add the component to</param>
        /// <returns>The added component or existing cached component</returns>
        public static T AddCachedComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;
                
            // Try to get existing component first
            T component = gameObject.GetCachedComponent<T>();
            
            // If component doesn't exist, add it and cache it
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
                
                Type componentType = typeof(T);
                
                // Initialize dictionary for this GameObject if it doesn't exist
                if (!CachedComponents.TryGetValue(gameObject, out Dictionary<Type, Component> components))
                {
                    components = new Dictionary<Type, Component>();
                    CachedComponents[gameObject] = components;
                }
                
                // Cache the component
                components[componentType] = component;
            }
            
            return component;
        }
        
        /// <summary>
        /// Adds a component of type T to the specified MonoBehaviour's GameObject if it doesn't exist, and caches it
        /// </summary>
        /// <typeparam name="T">The type of component to add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour whose GameObject to add the component to</param>
        /// <returns>The added component or existing cached component</returns>
        public static T AddCachedComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour != null ? monoBehaviour.gameObject.AddCachedComponent<T>() : null;
        }

        /// <summary>
        /// Gets all components of type T from the specified GameObject with caching to avoid performance overhead
        /// </summary>
        /// <typeparam name="T">The type of components to get</typeparam>
        /// <param name="gameObject">The GameObject to get the components from</param>
        /// <returns>The cached components array or empty array if none exist</returns>
        public static T[] GetCachedComponents<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return new T[0];
                
            Type componentType = typeof(T);
            
            // Initialize dictionary for this GameObject if it doesn't exist
            if (!CachedComponentArrays.TryGetValue(gameObject, out Dictionary<Type, Component[]> componentArrays))
            {
                componentArrays = new Dictionary<Type, Component[]>();
                CachedComponentArrays[gameObject] = componentArrays;
            }
            
            // Try to get components from cache, otherwise get them and cache them
            if (!componentArrays.TryGetValue(componentType, out Component[] components) || components == null)
            {
                components = gameObject.GetComponents<T>();
                if (components != null && components.Length > 0)
                {
                    componentArrays[componentType] = components;
                }
                else
                {
                    // Cache empty array to avoid repeated lookups for components that don't exist
                    componentArrays[componentType] = new Component[0];
                    return new T[0];
                }
            }
            
            return components as T[];
        }
        
        /// <summary>
        /// Gets all components of type T from the specified MonoBehaviour's GameObject with caching
        /// </summary>
        /// <typeparam name="T">The type of components to get</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour whose GameObject to get the components from</param>
        /// <returns>The cached components array or empty array if none exist</returns>
        public static T[] GetCachedComponents<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour != null ? monoBehaviour.gameObject.GetCachedComponents<T>() : new T[0];
        }

        /// <summary>
        /// Gets all components of type T from all active GameObjects in the scene with caching
        /// </summary>
        /// <typeparam name="T">The type of components to get</typeparam>
        /// <param name="includeInactive">Whether to include inactive GameObjects</param>
        /// <returns>Array of all components of type T</returns>
        public static T[] GetAllComponentsOfType<T>(bool includeInactive = false) where T : Component
        {
            Type componentType = typeof(T);
            
            // Check if cache needs refreshing
            if (sceneCacheDirty)
            {
                // Find all components of type T in the scene
                T[] foundComponents = GameObject.FindObjectsOfType<T>(includeInactive);
                
                // Also add them to their respective GameObject caches
                foreach (T component in foundComponents)
                {
                    if (component != null)
                    {
                        GameObject componentGameObject = component.gameObject;
                        
                        // Cache the single component
                        if (!CachedComponents.TryGetValue(componentGameObject, out Dictionary<Type, Component> components))
                        {
                            components = new Dictionary<Type, Component>();
                            CachedComponents[componentGameObject] = components;
                        }
                        components[componentType] = component;
                        
                        // Cache the component array
                        if (!CachedComponentArrays.TryGetValue(componentGameObject, out Dictionary<Type, Component[]> componentArrays))
                        {
                            componentArrays = new Dictionary<Type, Component[]>();
                            CachedComponentArrays[componentGameObject] = componentArrays;
                        }
                        
                        T[] allComponents = component.gameObject.GetComponents<T>();
                        componentArrays[componentType] = allComponents;
                    }
                }
                
                sceneCacheDirty = false;
            }
            
            // Collect all components of type T from CachedComponents
            List<T> result = new List<T>();
            foreach (var gameObjectComponents in CachedComponents.Values)
            {
                if (gameObjectComponents.TryGetValue(componentType, out Component component))
                {
                    T typedComponent = component as T;
                    if (typedComponent != null && (includeInactive || typedComponent.gameObject.activeInHierarchy))
                    {
                        result.Add(typedComponent);
                    }
                }
            }
            
            return result.ToArray();
        }
        
        /// <summary>
        /// Marks the scene cache as dirty, so it will be refreshed on the next GetAllComponentsOfType call
        /// </summary>
        public static void InvalidateSceneCache()
        {
            sceneCacheDirty = true;
        }

        /// <summary>
        /// Clears the cache for a specific GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to clear the cache for</param>
        public static void ClearCache(this GameObject gameObject)
        {
            if (gameObject == null)
                return;
                
            CachedComponents.Remove(gameObject);
            CachedComponentArrays.Remove(gameObject);
        }
        
        /// <summary>
        /// Clears the entire component cache
        /// </summary>
        public static void ClearAllCaches()
        {
            CachedComponents.Clear();
            CachedComponentArrays.Clear();
            sceneCacheDirty = true;
        }
    }

    /// <summary>
    /// Provides a diagnostic Unity object identifier across Unity versions.
    /// </summary>
    public static class UnityObjectIdExtensions
    {
        private static readonly MethodInfo ObjectIdMethod =
            typeof(UnityEngine.Object).GetMethod("GetEntityId", Type.EmptyTypes) ??
            typeof(UnityEngine.Object).GetMethod("GetInstanceID", Type.EmptyTypes);

        public static string GetObjectId(this UnityEngine.Object unityObject)
        {
            if (unityObject == null)
                return "null";

            return ObjectIdMethod?.Invoke(unityObject, null)?.ToString() ?? unityObject.name;
        }
    }
}
