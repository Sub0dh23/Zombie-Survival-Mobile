using System.Collections.Generic;
using UnityEngine;

namespace DeadDawn.Core
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [Header("Pool Setup")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 30;
        [SerializeField] private bool canExpand = true;

        private readonly Queue<GameObject> availableObjects = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Prewarm();
        }

        private void Prewarm()
        {
            if (prefab == null) return;

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            availableObjects.Enqueue(obj);
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (canExpand)
            {
                obj = CreateNewObject();
                availableObjects.Dequeue();
            }
            else
            {
                return null;
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }

        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            availableObjects.Enqueue(obj);
        }
    }
}
