using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RageBaitMode : MonoBehaviour
{
    [Header("Toggle")]
    public bool rageBaitModeToggle = false;

    // =========================
    // FPS GLITCH
    // =========================
    [Header("FPS Glitch")]
    public float lowFps = 1.5f;

    // =========================
    // CONTROLS
    // =========================
    [Header("Controls")]
    public bool invertControls = false;

    [Header("Input Lag")]
    public bool inputLagActive = false;
    public int lagFrames = 10;

    private Queue<Vector2> inputBuffer = new Queue<Vector2>();

    // =========================
    // CAMERA TEARING
    // =========================
    [Header("Screen Tearing")]
    public bool screenTearingActive = false;
    public float tearIntensity = 0.02f;

    private Camera cam;
    private Vector3 camStartPos;

    // =========================
    // FAKE SHUTDOWN
    // =========================
    [Header("Fake Shutdown")]
    public CanvasGroup blackScreenOverlay;
    public float fadeSpeed = 2f;

    // =========================
    // FAKE BSOD
    // =========================
    [Header("Fake BSOD")]
    public CanvasGroup bsodOverlay;
    public bool bsodActive = false;

    void Start()
    {
        cam = Camera.main;

        if (cam != null)
            camStartPos = cam.transform.localPosition;

        if (rageBaitModeToggle)
        {
            StartCoroutine(FpsGlitchLoop());
            StartCoroutine(RandomRestartLoop());
            StartCoroutine(InvertControlsLoop());
            StartCoroutine(InputLagLoop());
            StartCoroutine(ScreenTearingLoop());
            StartCoroutine(FakeShutdownLoop());
            StartCoroutine(BSODLoop());
        }
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (invertControls)
            input *= -1;

        inputBuffer.Enqueue(input);

        if (inputBuffer.Count > 120)
            inputBuffer.Dequeue();

        // Screen tearing effect (camera jitter)
        if (screenTearingActive && cam != null)
        {
            float x = Random.Range(-tearIntensity, tearIntensity);
            float y = Random.Range(-tearIntensity, tearIntensity);

            cam.transform.localPosition = camStartPos + new Vector3(x, y, 0);
        }
        else if (cam != null)
        {
            cam.transform.localPosition = camStartPos;
        }
    }

    // =========================
    // INPUT FOR PLAYER
    // =========================
    public Vector2 GetLaggedInput()
    {
        if (inputBuffer.Count == 0)
            return Vector2.zero;

        if (inputLagActive && inputBuffer.Count > lagFrames)
            return new List<Vector2>(inputBuffer)[0];

        return inputBuffer.Peek();
    }

    // =========================
    // FPS GLITCH
    // =========================
    IEnumerator FpsGlitchLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(1f, 60f));

            float duration = Random.Range(1f, 7f);

            Application.targetFrameRate = (int)lowFps;

            yield return new WaitForSeconds(duration);

            Application.targetFrameRate = 60;
        }
    }

    // =========================
    // RESTART GAME
    // =========================
    IEnumerator RandomRestartLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(1f, 340f));

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // =========================
    // INVERT CONTROLS
    // =========================
    IEnumerator InvertControlsLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(1f, 70f));

            float duration = Random.Range(1f, 15f);

            invertControls = true;
            yield return new WaitForSeconds(duration);
            invertControls = false;
        }
    }

    // =========================
    // INPUT LAG
    // =========================
    IEnumerator InputLagLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(1f, 40f));

            inputLagActive = true;
            lagFrames = Random.Range(50, 300);

            float duration = Random.Range(1f, 12f);

            yield return new WaitForSeconds(duration);

            inputLagActive = false;
        }
    }

    // =========================
    // SCREEN TEARING
    // =========================
    IEnumerator ScreenTearingLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(5f, 90f));

            screenTearingActive = true;

            float duration = Random.Range(1f, 6f);

            yield return new WaitForSeconds(duration);

            screenTearingActive = false;
        }
    }

    // =========================
    // FAKE SHUTDOWN
    // =========================
    IEnumerator FakeShutdownLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(30f, 600f));

            float duration = Random.Range(3f, 10f);

            yield return StartCoroutine(FadeOverlay(blackScreenOverlay, 1f));

            yield return new WaitForSeconds(duration);

            yield return StartCoroutine(FadeOverlay(blackScreenOverlay, 0f));
        }
    }

    // =========================
    // FAKE BSOD
    // =========================
    IEnumerator BSODLoop()
    {
        while (rageBaitModeToggle)
        {
            yield return new WaitForSeconds(Random.Range(60f, 600f));

            float duration = Random.Range(2f, 8f);

            bsodActive = true;

            yield return StartCoroutine(FadeOverlay(bsodOverlay, 1f));

            yield return new WaitForSeconds(duration);

            bsodActive = false;

            yield return StartCoroutine(FadeOverlay(bsodOverlay, 0f));
        }
    }

    // =========================
    // FADE SYSTEM
    // =========================
    IEnumerator FadeOverlay(CanvasGroup cg, float target)
    {
        if (cg == null) yield break;

        while (!Mathf.Approximately(cg.alpha, target))
        {
            cg.alpha = Mathf.MoveTowards(
                cg.alpha,
                target,
                Time.deltaTime * fadeSpeed
            );

            yield return null;
        }
    }
}