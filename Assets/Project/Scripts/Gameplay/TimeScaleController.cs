using System.Collections;
using UnityEngine;

public class TimeScaleController : MonoBehaviour
{
    public static TimeScaleController Instance { get; private set; }

    private float normalFixedDeltaTime;
    private Coroutine slowMotionRoutine;

    private void Awake()
    {
        Instance = this;
        normalFixedDeltaTime = Time.fixedDeltaTime;
        RestoreNormalTime();
    }

    public void StartSlowMotion(float scale, float durationRealtime)
    {
        if (slowMotionRoutine != null)
            StopCoroutine(slowMotionRoutine);

        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(scale, durationRealtime));
    }

    public void RestoreNormalTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = normalFixedDeltaTime;
        slowMotionRoutine = null;
    }

    private IEnumerator SlowMotionRoutine(float scale, float durationRealtime)
    {
        Time.timeScale = Mathf.Clamp(scale, 0.05f, 1f);
        Time.fixedDeltaTime = normalFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(durationRealtime);

        RestoreNormalTime();
    }

    private void OnDisable()
    {
        RestoreNormalTime();
    }

    private void OnDestroy()
    {
        RestoreNormalTime();
    }
}