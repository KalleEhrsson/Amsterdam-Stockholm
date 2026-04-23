using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EndlessBackgroundScroller2D : MonoBehaviour
{
    [Serializable]
    public class LayerSettings
    {
        public string layerName = "Layer";
        public List<GameObject> prefabs = new();

        [Header("Movement")]
        public float moveSpeed = 1f;
        [Range(0f, 2f)] public float parallaxMultiplier = 1f;

        [Header("Placement")]
        public float spawnY = 0f;
        public float zPosition = 0f;
        public Vector2 positionOffset = Vector2.zero;

        [Header("Spacing")]
        public Vector2 gapRange = new(0.5f, 2f);

        [Header("Rendering")]
        public int sortingOrder = 0;

        [Header("Hierarchy")]
        public Transform container;

        [NonSerialized] public readonly List<GameObject> activeInstances = new();
    }

    #region Inspector

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Spawn Bounds")]
    [SerializeField] private float spawnOffsetRight = 2f;
    [SerializeField] private float despawnOffsetLeft = 2f;

    [Header("Startup")]
    [SerializeField] private bool fillOnStart = true;
    [SerializeField] private int randomSeed = 0;
    [SerializeField] private bool useRandomSeed = true;

    [Header("Layers")]
    [SerializeField] private List<LayerSettings> layers = new();

    #endregion

    #region Unity

    private void Reset()
    {
        targetCamera ??= Camera.main;
        CreateDefaultLayers();
        AutoCreateContainers();
    }

    private void OnValidate()
    {
        targetCamera ??= Camera.main;

        if (layers == null)
        {
            layers = new List<LayerSettings>();
        }

        if (layers.Count == 0)
        {
            CreateDefaultLayers();
        }

        AutoCreateContainers();
    }

    private void Awake()
    {
        targetCamera ??= Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError($"{nameof(EndlessBackgroundScroller2D)} needs a camera.");
            enabled = false;
            return;
        }

        if (!useRandomSeed)
        {
            UnityEngine.Random.InitState(randomSeed);
        }

        AutoCreateContainers();
    }

    private void Start()
    {
        if (fillOnStart)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                FillLayerToRightEdge(layers[i]);
            }
        }
    }

    private void Update()
    {
        float leftBound = GetLeftWorldX() - despawnOffsetLeft;
        float rightBound = GetRightWorldX() + spawnOffsetRight;

        for (int i = 0; i < layers.Count; i++)
        {
            LayerSettings layer = layers[i];

            MoveLayer(layer);
            CleanupLayer(layer, leftBound);
            SpawnIfNeeded(layer, rightBound);
        }
    }

    #endregion

    #region Defaults

    private void CreateDefaultLayers()
    {
        layers = new List<LayerSettings>
        {
            new LayerSettings
            {
                layerName = "Background",
                moveSpeed = 0.6f,
                parallaxMultiplier = 0.45f,
                spawnY = 2.5f,
                zPosition = 0f,
                positionOffset = new Vector2(0f, 0f),
                gapRange = new Vector2(4f, 7f),
                sortingOrder = 0
            },
            new LayerSettings
            {
                layerName = "Midground",
                moveSpeed = 1.2f,
                parallaxMultiplier = 1f,
                spawnY = 0f,
                zPosition = 0f,
                positionOffset = new Vector2(0f, 0f),
                gapRange = new Vector2(2f, 4f),
                sortingOrder = 5
            },
            new LayerSettings
            {
                layerName = "Foreground",
                moveSpeed = 2.2f,
                parallaxMultiplier = 1.5f,
                spawnY = -2f,
                zPosition = 0f,
                positionOffset = new Vector2(0f, 0f),
                gapRange = new Vector2(0.75f, 2f),
                sortingOrder = 10
            }
        };
    }

    #endregion

    #region Setup

    private void AutoCreateContainers()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            LayerSettings layer = layers[i];

            if (layer.container != null)
            {
                continue;
            }

            Transform existing = transform.Find($"BG_{layer.layerName}");
            if (existing != null)
            {
                layer.container = existing;
                continue;
            }

            GameObject containerObject = new($"BG_{layer.layerName}");
            containerObject.transform.SetParent(transform);
            containerObject.transform.localPosition = Vector3.zero;
            layer.container = containerObject.transform;
        }
    }

    #endregion

    #region Runtime

    private void MoveLayer(LayerSettings layer)
    {
        if (layer.activeInstances.Count == 0)
        {
            return;
        }

        float moveAmount = layer.moveSpeed * layer.parallaxMultiplier * Time.deltaTime;

        for (int i = 0; i < layer.activeInstances.Count; i++)
        {
            GameObject instance = layer.activeInstances[i];

            if (instance == null)
            {
                continue;
            }

            instance.transform.position += Vector3.left * moveAmount;
        }
    }

    private void CleanupLayer(LayerSettings layer, float leftBound)
    {
        for (int i = layer.activeInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = layer.activeInstances[i];

            if (instance == null)
            {
                layer.activeInstances.RemoveAt(i);
                continue;
            }

            float rightEdge = GetSpriteRightEdge(instance);

            if (rightEdge < leftBound)
            {
                layer.activeInstances.RemoveAt(i);
                Destroy(instance);
            }
        }
    }

    private void SpawnIfNeeded(LayerSettings layer, float rightBound)
    {
        if (layer.prefabs == null || layer.prefabs.Count == 0)
        {
            return;
        }

        if (layer.activeInstances.Count == 0)
        {
            SpawnNext(layer, rightBound);
            return;
        }

        GameObject lastInstance = GetRightmostInstance(layer);

        if (lastInstance == null)
        {
            SpawnNext(layer, rightBound);
            return;
        }

        float lastRightEdge = GetSpriteRightEdge(lastInstance);
        float gap = GetRandomGap(layer);

        if (lastRightEdge + gap < rightBound)
        {
            SpawnNext(layer, lastRightEdge + gap);
        }
    }

    private void FillLayerToRightEdge(LayerSettings layer)
    {
        if (layer.prefabs == null || layer.prefabs.Count == 0)
        {
            return;
        }

        float leftStart = GetLeftWorldX() - 1f;
        float rightBound = GetRightWorldX() + spawnOffsetRight;
        float currentX = leftStart;

        while (currentX < rightBound)
        {
            GameObject instance = SpawnAtX(layer, currentX);
            if (instance == null)
            {
                break;
            }

            float rightEdge = GetSpriteRightEdge(instance);
            currentX = rightEdge + GetRandomGap(layer);
        }
    }

    private void SpawnNext(LayerSettings layer, float spawnFromX)
    {
        SpawnAtX(layer, spawnFromX);
    }

    private GameObject SpawnAtX(LayerSettings layer, float leftEdgeX)
    {
        GameObject prefab = GetRandomPrefab(layer);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, layer.container);

        Vector3 position = new(
            leftEdgeX + layer.positionOffset.x,
            layer.spawnY + layer.positionOffset.y,
            layer.zPosition
        );

        instance.transform.position = position;

        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = layer.sortingOrder;
        }

        float leftEdge = GetSpriteLeftEdge(instance);
        float offset = leftEdgeX - leftEdge;

        instance.transform.position += new Vector3(offset, 0f, 0f);

        layer.activeInstances.Add(instance);
        return instance;
    }

    #endregion

    #region Helpers

    private GameObject GetRandomPrefab(LayerSettings layer)
    {
        if (layer.prefabs == null || layer.prefabs.Count == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, layer.prefabs.Count);
        return layer.prefabs[index];
    }

    private GameObject GetRightmostInstance(LayerSettings layer)
    {
        GameObject best = null;
        float bestRight = float.MinValue;

        for (int i = 0; i < layer.activeInstances.Count; i++)
        {
            GameObject instance = layer.activeInstances[i];

            if (instance == null)
            {
                continue;
            }

            float rightEdge = GetSpriteRightEdge(instance);

            if (rightEdge > bestRight)
            {
                bestRight = rightEdge;
                best = instance;
            }
        }

        return best;
    }

    private float GetRandomGap(LayerSettings layer)
    {
        float min = Mathf.Min(layer.gapRange.x, layer.gapRange.y);
        float max = Mathf.Max(layer.gapRange.x, layer.gapRange.y);
        return UnityEngine.Random.Range(min, max);
    }

    private float GetLeftWorldX()
    {
        Vector3 point = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, GetCameraDistance()));
        return point.x;
    }

    private float GetRightWorldX()
    {
        Vector3 point = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, GetCameraDistance()));
        return point.x;
    }

    private float GetCameraDistance()
    {
        if (targetCamera.orthographic)
        {
            return Mathf.Abs(targetCamera.transform.position.z);
        }

        return 10f;
    }

    private float GetSpriteLeftEdge(GameObject instance)
    {
        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return instance.transform.position.x;
        }

        return spriteRenderer.bounds.min.x;
    }

    private float GetSpriteRightEdge(GameObject instance)
    {
        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return instance.transform.position.x;
        }

        return spriteRenderer.bounds.max.x;
    }

    #endregion
}