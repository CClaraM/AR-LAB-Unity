using UnityEngine;
using TMPro;

public class AndroidBridge : MonoBehaviour
{
    public static AndroidBridge Instance { get; private set; }
    public ExerciseData CurrentExerciseData { get; private set; }

    [SerializeField] private TMP_Text debugText;

    private const string EXTRA_EXERCISE_DATA = "exerciseData";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("AndroidBridge vivo");

        if (debugText != null)
            debugText.text = "AndroidBridge vivo";

#if UNITY_ANDROID && !UNITY_EDITOR
        TryReadIntentData();
#endif
    }

    private void TryReadIntentData()
    {
        try
        {
            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using AndroidJavaObject intent =
                activity.Call<AndroidJavaObject>("getIntent");

            string json = intent.Call<string>("getStringExtra", EXTRA_EXERCISE_DATA);

            if (!string.IsNullOrEmpty(json))
            {
                ReceiveExerciseData(json);
            }
            else
            {
                Debug.LogWarning("No llegó exerciseData en el Intent.");

                if (debugText != null)
                    debugText.text = "AndroidBridge vivo\nSin JSON en Intent";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error leyendo Intent desde Unity: " + e.Message);

            if (debugText != null)
                debugText.text = "Error leyendo Intent:\n" + e.Message;
        }
    }

    public void ReceiveExerciseData(string json)
    {
        Debug.Log("JSON recibido desde Android: " + json);

        CurrentExerciseData = JsonUtility.FromJson<ExerciseData>(json);

        if (debugText != null)
        {
            debugText.text =
                $"Ejercicio: {CurrentExerciseData.exerciseId}\n" +
                $"Potencia: {CurrentExerciseData.initialPower}\n" +
                $"Ángulo: {CurrentExerciseData.initialAngle}\n" +
                $"Trayectoria: {CurrentExerciseData.showTrajectory}";
        }
    }

    public void FinishLabAndReturn()
    {
        string resultJson = "{"
            + "\"exerciseId\":\"PARABOLIC-001\","
            + "\"hit\":true,"
            + "\"attempts\":1,"
            + "\"finalPower\":3.5,"
            + "\"finalAngle\":35.0"
            + "}";

        SendResultToAndroid(resultJson);
    }

    public void OnExitButtonPressed()
    {
        AndroidBridge.Instance.FinishLabAndReturn();
    }

    public void SendResultToAndroid(string resultJson)
    {
        Debug.Log("Enviando resultado a Android: " + resultJson);

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("onUnityLabFinished", resultJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error enviando resultado a Android: " + e.Message);
        }
#endif
    }
}