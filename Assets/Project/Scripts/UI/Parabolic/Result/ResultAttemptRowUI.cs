using TMPro;
using UnityEngine;

public class ResultAttemptRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public void Setup(LabAttemptResult attempt)
    {
        if (text == null || attempt == null)
            return;

        string resultLabel = attempt.hit ? "Acierto" : "Fallo";

        string distanceText =
            attempt.impactDistanceToTarget < 0f
                ? "Fuera del área"
                : $"{attempt.impactDistanceToTarget:0.00} m";

        text.text =
            $"Intento {attempt.attempt}\n" +
            $"Resultado: {resultLabel}\n" +
            $"Velocidad: {attempt.power:0.0} m/s\n" +
            $"Ángulo: {attempt.angle:0.0}°\n" +
            $"Distancia al objetivo: {distanceText}";
    }
}