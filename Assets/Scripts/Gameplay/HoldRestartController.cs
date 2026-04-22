using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HoldRestartController : MonoBehaviour
{
    #region Fields
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float resetSpeed = 3f;
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private Vector2 barSize = new(300f, 20f);
    [SerializeField] private Vector2 barOffset = new(0f, 48f);

    private const string CanvasName = "RestartCanvas";
    private const string BackgroundName = "RestartBarBackground";
    private const string FillName = "RestartBarFill";

    private float currentHoldTime;
    private float currentAlpha;
    private float targetAlpha;
    private bool isRestarting;

    private Canvas canvas;
    private Image backgroundImage;
    private Image fillImage;
    private CanvasGroup barCanvasGroup;
    #endregion

    #region Bootstrap
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindAnyObjectByType<HoldRestartController>() != null)
        {
            return;
        }

        GameObject controllerObject = new("HoldRestartController");
        controllerObject.AddComponent<HoldRestartController>();
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureUi();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isRestarting)
        {
            return;
        }

        bool isHoldingRestart = Input.GetKey(KeyCode.R);

        if (isHoldingRestart)
        {
            targetAlpha = 1f;
            currentHoldTime += Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                targetAlpha = 0f;
            }

            currentHoldTime = Mathf.MoveTowards(currentHoldTime, 0f, resetSpeed * Time.deltaTime);
        }

        currentHoldTime = Mathf.Clamp(currentHoldTime, 0f, holdDuration);

        float progress = holdDuration <= 0f ? 1f : currentHoldTime / holdDuration;
        fillImage.fillAmount = Mathf.Clamp01(progress);

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        barCanvasGroup.alpha = Mathf.Clamp01(currentAlpha);

        if (progress >= 1f)
        {
            RestartScene();
        }
    }
    #endregion

    #region Setup
    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        EnsureUi();
        currentHoldTime = 0f;
        isRestarting = false;
        targetAlpha = 0f;
        currentAlpha = 0f;
        fillImage.fillAmount = 0f;
        barCanvasGroup.alpha = 0f;
    }

    private void EnsureUi()
    {
        canvas = FindCanvas();

        Transform backgroundTransform = canvas.transform.Find(BackgroundName);
        if (backgroundTransform == null)
        {
            backgroundTransform = CreateBackground(canvas.transform);
        }

        backgroundImage = backgroundTransform.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = backgroundTransform.gameObject.AddComponent<Image>();
        }

        ConfigureBackgroundImage(backgroundImage);

        Transform fillTransform = backgroundTransform.Find(FillName);
        if (fillTransform == null)
        {
            fillTransform = CreateFill(backgroundTransform);
        }

        fillImage = fillTransform.GetComponent<Image>();
        if (fillImage == null)
        {
            fillImage = fillTransform.gameObject.AddComponent<Image>();
        }

        ConfigureFillImage(fillImage);

        barCanvasGroup = backgroundTransform.GetComponent<CanvasGroup>();
        if (barCanvasGroup == null)
        {
            barCanvasGroup = backgroundTransform.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private Canvas FindCanvas()
    {
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            return existingCanvas;
        }

        GameObject canvasObject = new(CanvasName);
        Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return createdCanvas;
    }

    private Transform CreateBackground(Transform parent)
    {
        GameObject backgroundObject = new(BackgroundName);
        backgroundObject.transform.SetParent(parent, false);

        RectTransform rect = backgroundObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = barOffset;
        rect.sizeDelta = barSize;

        return rect;
    }

    private Transform CreateFill(Transform parent)
    {
        GameObject fillObject = new(FillName);
        fillObject.transform.SetParent(parent, false);

        RectTransform rect = fillObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);

        return rect;
    }

    private static void ConfigureBackgroundImage(Image image)
    {
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = false;
    }

    private static void ConfigureFillImage(Image image)
    {
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0f;
        image.color = new Color(0.25f, 0.95f, 0.45f, 0.95f);
        image.raycastTarget = false;
    }
    #endregion

    #region Restart
    private void RestartScene()
    {
        isRestarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    #endregion
}