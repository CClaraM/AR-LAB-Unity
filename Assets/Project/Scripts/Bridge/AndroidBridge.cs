using UnityEngine;
using TMPro;

public class AndroidBridge : MonoBehaviour
{
    public static AndroidBridge Instance { get; private set; }
    public LabBridgeInput CurrentLabInput { get; private set; }

    [SerializeField] private TMP_Text debugText;

    private const string EXTRA_EXERCISE_DATA = "exerciseData";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

        CurrentLabInput = JsonUtility.FromJson<LabBridgeInput>(json);

        if (debugText != null && CurrentLabInput != null)
        {
            string labName = CurrentLabInput.scene != null
                ? CurrentLabInput.scene.displayName
                : "Sin laboratorio";

            string participantName = CurrentLabInput.participant != null
                ? CurrentLabInput.participant.displayName
                : "Sin participante";

            int attempts = CurrentLabInput.exercise != null
                ? CurrentLabInput.exercise.maxAttempts
                : 0;

            debugText.text =
                $"Laboratorio: {labName}\n" +
                $"Participante: {participantName}\n" +
                $"Intentos: {attempts}\n" +
                $"RunId: {CurrentLabInput.runId}";
        }
    }

    public void FinishLabAndReturn(string resultJson)
    {
        if (string.IsNullOrEmpty(resultJson))
        {
            Debug.LogWarning("AndroidBridge: resultJson está vacío.");
            return;
        }

        SendResultToAndroid(resultJson);
    }

    //Borrar
    public void FinishLabAndReturnDebug()
    {
        string debugResultJson = "{"
            + "\"debug\":true,"
            + "\"message\":\"Resultado de prueba desde Unity\""
            + "}";

        SendResultToAndroid(debugResultJson);
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