using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple implementation of singleton pattern.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    #region Fields
    private static T _instance;
    #endregion

    #region Properties
    /// <summary>
    /// Take you an object that you wish.
    /// If instance previously created then return it,
    /// else try to find object in scene.
    /// If object not exist in scene, then create and
    /// return it.
    /// If match objects more than one, then debug error
    /// and return null.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                T[] findedObjects = FindObjectsOfType<T>();

                // Create singleton.
                if (findedObjects.Length == 0)
                {
                    _instance = new GameObject(string.Format("{0}", typeof(T).ToString())).AddComponent<T>();

                    return _instance;
                }

                // Return singleton.
                if (findedObjects.Length == 1)
                {
                    _instance = findedObjects[0];

                    return _instance;
                }

                // If scene have more than one T components, then show error and return null.
                print(string.Format("Scene have more than one components of <{0}> type", typeof(T).ToString()));

                return null;
            }
        }
    }
    #endregion

    protected virtual void Awake() => _instance = GetComponent<T>();
}