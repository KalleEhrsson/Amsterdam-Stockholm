using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class PickupPromptUI : MonoBehaviour
{
    public enum PromptState
    {
        None,
        CanPickUp,
        HoldingItem
    }

    private const string PickUpText = "Left Click to pick up";
    private const string DropText = "Press Q to drop";

    private static PickupPromptUI instance;

    private BasicInventory inventory;
    private Canvas canvas;
    private RectTransform promptRoot;
    private CanvasGroup promptCanvasGroup;
    private Text legacyText;
    private Component tmpText;
    private MethodInfo tmpSetTextMethod;
    private PromptState currentState = PromptState.None;

    #region Bootstrap
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<PickupPromptUI>() != null) return;
        GameObject go = new GameObject(nameof(PickupPromptUI));
        go.AddComponent<PickupPromptUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveInventory();
        EnsureUI();
        ApplyState(PromptState.None, true);
    }

    private void Update()
    {
        if (inventory == null)
        {
            if (!ResolveInventory())
            {
                ApplyState(PromptState.None, false);
                return;
            }
        }

        PromptState nextState = ResolveState();
        ApplyState(nextState, false);
    }
    #endregion

    #region State
    private bool ResolveInventory()
    {
        inventory = FindFirstObjectByType<BasicInventory>();
        return inventory != null;
    }

    private PromptState ResolveState()
    {
        if (inventory.IsHoldingItem)
        {
            return PromptState.HoldingItem;
        }

        if (inventory.HasPickupCandidate)
        {
            return PromptState.CanPickUp;
        }

        return PromptState.None;
    }

    private void ApplyState(PromptState nextState, bool force)
    {
        if (!force && currentState == nextState) return;
        currentState = nextState;

        if (nextState == PromptState.None)
        {
            promptCanvasGroup.alpha = 0f;
            return;
        }

        promptCanvasGroup.alpha = 1f;
        SetPromptText(nextState == PromptState.HoldingItem ? DropText : PickUpText);
    }
    #endregion

    #region UI
    private void EnsureUI()
    {
        canvas = FindOrCreateCanvas();

        Transform foundRoot = canvas.transform.Find("PickupPromptRoot");
        if (foundRoot != null)
        {
            promptRoot = foundRoot as RectTransform;
        }
        else
        {
            GameObject rootGo = new GameObject("PickupPromptRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(canvas.transform, false);
            promptRoot = rootGo.GetComponent<RectTransform>();
            promptRoot.anchorMin = new Vector2(0.5f, 0f);
            promptRoot.anchorMax = new Vector2(0.5f, 0f);
            promptRoot.pivot = new Vector2(0.5f, 0f);
            promptRoot.anchoredPosition = new Vector2(0f, 70f);
            promptRoot.sizeDelta = new Vector2(560f, 56f);
        }

        promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        if (promptCanvasGroup == null)
        {
            promptCanvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (!TrySetupTMPLabel())
        {
            SetupLegacyLabel();
        }
    }

    private Canvas FindOrCreateCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        GameObject canvasGo = new GameObject("PickupPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas createdCanvas = canvasGo.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    private bool TrySetupTMPLabel()
    {
        Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null) return false;

        Transform existing = promptRoot.Find("LabelTMP");
        GameObject labelGo;
        if (existing != null)
        {
            labelGo = existing.gameObject;
        }
        else
        {
            labelGo = new GameObject("LabelTMP", typeof(RectTransform));
            labelGo.transform.SetParent(promptRoot, false);
        }

        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        tmpText = labelGo.GetComponent(tmpType);
        if (tmpText == null)
        {
            tmpText = labelGo.AddComponent(tmpType);
        }

        tmpSetTextMethod = tmpType.GetMethod("SetText", new[] { typeof(string) });

        PropertyInfo fontSize = tmpType.GetProperty("fontSize");
        PropertyInfo alignment = tmpType.GetProperty("alignment");
        PropertyInfo color = tmpType.GetProperty("color");
        PropertyInfo raycastTarget = tmpType.GetProperty("raycastTarget");

        fontSize?.SetValue(tmpText, 32f);
        color?.SetValue(tmpText, Color.white);
        raycastTarget?.SetValue(tmpText, false);

        Type alignEnum = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignEnum != null && alignment != null)
        {
            object center = Enum.Parse(alignEnum, "Center");
            alignment.SetValue(tmpText, center);
        }

        Transform fallback = promptRoot.Find("LabelLegacy");
        if (fallback != null)
        {
            fallback.gameObject.SetActive(false);
        }

        return true;
    }

    private void SetupLegacyLabel()
    {
        Transform existing = promptRoot.Find("LabelLegacy");
        GameObject labelGo;
        if (existing != null)
        {
            labelGo = existing.gameObject;
        }
        else
        {
            labelGo = new GameObject("LabelLegacy", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(promptRoot, false);
        }

        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        legacyText = labelGo.GetComponent<Text>();
        legacyText.alignment = TextAnchor.MiddleCenter;
        legacyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        legacyText.verticalOverflow = VerticalWrapMode.Truncate;
        legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        legacyText.fontSize = 28;
        legacyText.color = Color.white;
        legacyText.raycastTarget = false;

        if (tmpText != null)
        {
            (tmpText as Behaviour).enabled = false;
        }
    }

    private void SetPromptText(string value)
    {
        if (tmpText != null)
        {
            if (tmpSetTextMethod != null)
            {
                tmpSetTextMethod.Invoke(tmpText, new object[] { value });
            }
            return;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
    #endregion
}