using UnityEngine;
using TMPro;

public class FuelManager : MonoBehaviour
{
    public static FuelManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fuelText;

    [Header("Fuel Settings")]
    [SerializeField] private int startingFuel = 2;

    public int CurrentFuel { get; private set; }

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
        CurrentFuel = startingFuel;
        UpdateFuelText();
    }

    public void UseFuel(int amount)
    {
        CurrentFuel -= amount;
        if (CurrentFuel < 0) CurrentFuel = 0;
        UpdateFuelText();
    }

    public void Refuel(int amount)
    {
        CurrentFuel += amount;
        UpdateFuelText();
    }

    public void SetFuel(int amount)
    {
        CurrentFuel = Mathf.Max(0, amount);
        UpdateFuelText();
    }

    private void UpdateFuelText()
    {
        if (fuelText != null)
        {
            fuelText.text = "x " + CurrentFuel;
        }
    }
}
