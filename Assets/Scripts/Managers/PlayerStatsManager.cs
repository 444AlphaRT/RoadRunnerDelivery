using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    // Cloud Save keys
    private const string KEY_TOTAL_DELIVERIES = "total_deliveries";
    private const string KEY_TOTAL_EARNED = "total_earned";

    // Defaults (no magic numbers)
    private const int DEFAULT_INT_VALUE = 0;
    private const int ONE_DELIVERY = 1;

    // Run (current run)
    public int RunDeliveries { get; private set; }
    public int RunEarned { get; private set; }

    // Total (per user)
    public int TotalDeliveries { get; private set; }
    public int TotalEarned { get; private set; }

    private bool totalsLoaded = false;
    private bool isSaving = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this after login succeeds (or at scene start if already logged in)
    public async void LoadTotalsFromCloud()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            totalsLoaded = true;
            TotalDeliveries = DEFAULT_INT_VALUE;
            TotalEarned = DEFAULT_INT_VALUE;
            return;
        }

        try
        {
            var keys = new HashSet<string>
            {
                KEY_TOTAL_DELIVERIES,
                KEY_TOTAL_EARNED
            };

            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            TotalDeliveries = ReadInt(data, KEY_TOTAL_DELIVERIES, DEFAULT_INT_VALUE);
            TotalEarned = ReadInt(data, KEY_TOTAL_EARNED, DEFAULT_INT_VALUE);

            totalsLoaded = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("PlayerStatsManager: Load failed: " + e.Message);
            totalsLoaded = true;
        }
    }

    // Call when starting a NEW run (stage select)
    public void ResetRun()
    {
        RunDeliveries = DEFAULT_INT_VALUE;
        RunEarned = DEFAULT_INT_VALUE;
    }

    // Call when a delivery completes and coins were earned
    public async void RegisterDelivery(int coinsEarned)
    {
        int safeCoins = Mathf.Max(DEFAULT_INT_VALUE, coinsEarned);

        // Run stats
        RunDeliveries += ONE_DELIVERY;
        RunEarned += safeCoins;

        // Total stats
        TotalDeliveries += ONE_DELIVERY;
        TotalEarned += safeCoins;

        await SaveTotalsToCloudAsync();
    }

    private async Task SaveTotalsToCloudAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
            return;

        if (isSaving)
            return;

        isSaving = true;

        try
        {
            var payload = new Dictionary<string, object>
            {
                { KEY_TOTAL_DELIVERIES, TotalDeliveries },
                { KEY_TOTAL_EARNED, TotalEarned }
            };

            // FIX: Use SaveAsync (newer Cloud Save API)
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning("PlayerStatsManager: Save failed: " + e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private static int ReadInt(
        Dictionary<string, Unity.Services.CloudSave.Models.Item> data,
        string key,
        int fallback)
    {
        if (data != null && data.TryGetValue(key, out var item))
        {
            try
            {
                return Convert.ToInt32(item.Value);
            }
            catch
            {
                // ignore parse errors
            }
        }

        return fallback;
    }
}