using System;
using System.Globalization;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Evento para UI (por ejemplo el "money" TMP del UIManager)
    public event Action<int, string> OnScoreChanged;

    [SerializeField] private int cents = 0;

    private void Awake()
    {
        // Singleton + persistencia entre escenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        NotifyScoreChanged();
    }

    public int GetCents() => cents;

    public string GetFormattedScore()
    {
        return (cents / 100f).ToString("0.00", CultureInfo.GetCultureInfo("es-ES")) + " €";
    }

    public void AddCents(int amountInCents)
    {
        cents += amountInCents;
        if (cents < 0) cents = 0;

        Debug.Log("Puntuación: " + GetFormattedScore());
        NotifyScoreChanged();
    }

    /// Intenta gastar (restar) una cantidad en céntimos.
    /// Devuelve true si se pudo pagar, false si no había suficiente.
    public bool TrySpendCents(int costInCents)
    {
        if (costInCents <= 0) return true;
        if (cents < costInCents) return false;

        cents -= costInCents;
        NotifyScoreChanged();
        return true;
    }

    private void NotifyScoreChanged()
    {
        OnScoreChanged?.Invoke(cents, GetFormattedScore());
    }
}
