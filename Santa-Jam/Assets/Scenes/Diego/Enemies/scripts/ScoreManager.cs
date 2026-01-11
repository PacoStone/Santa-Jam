using UnityEngine;
using System.Globalization;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private int cents = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddCents(int amountInCents)
    {
        cents += amountInCents;
        Debug.Log("Puntuación: " + GetFormattedScore());
    }

    public int GetCents()
    {
        return cents;
    }

    public float GetEuros()
    {
        return cents / 100f;
    }

    public string GetFormattedScore()
    {
        return (cents / 100f).ToString("0.00", CultureInfo.GetCultureInfo("es-ES")) + " €";
    }
}
