using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainManu";

    [Header("Auth")]
    [SerializeField] private string fixedPassword = "RoadRunner#2026";

    [Header("Guest")]
    [SerializeField] private string guestProfilePrefix = "guest_";

    // Saves the last successful "real user" profile so you can return from guest easily
    private const string LastUserProfileKey = "LastUserProfile";

    private bool busy = false;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (statusText != null)
            statusText.text = "";
    }

    // =========================
    // Register
    // =========================
    public async void OnClickRegister()
    {
        if (busy) return;
        busy = true;

        try
        {
            string username = ValidateUsername();
            if (username == null) return;

            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(username);

            SetStatus("Registering...");

            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(username, fixedPassword);

            // Remember this user profile (so guest doesn't "erase" it locally)
            SaveLastUserProfile(username);

            SetStatus("Registered!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (AuthenticationException)
        {
            SetStatus("User already exists");
        }
        catch (Exception e)
        {
            SetStatus("Register failed");
            Debug.LogException(e);
        }
        finally
        {
            busy = false;
        }
    }

    // =========================
    // Login
    // =========================
    public async void OnClickLogin()
    {
        if (busy) return;
        busy = true;

        try
        {
            string username = ValidateUsername();
            if (username == null) return;

            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(username);

            SetStatus("Logging in...");

            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, fixedPassword);

            // Remember this user profile (so guest doesn't "erase" it locally)
            SaveLastUserProfile(username);

            SetStatus("Success!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (AuthenticationException)
        {
            SetStatus("User not found or wrong credentials");
        }
        catch (Exception e)
        {
            SetStatus("Login failed");
            Debug.LogException(e);
        }
        finally
        {
            busy = false;
        }
    }

    // =========================
    // Guest Login (Anonymous)
    // =========================
    public async void OnClickGuest()
    {
        if (busy) return;
        busy = true;

        try
        {
            // IMPORTANT:
            // SwitchProfile() only allows: alphanumeric, '-', '_', and max length 30.
            // Guid.ToString("N") is 32 chars -> INVALID.
            // So we take only 12 chars (valid + short).
            string shortId = Guid.NewGuid().ToString("N").Substring(0, 12);
            string guestProfile = guestProfilePrefix + shortId; // e.g. "guest_a1b2c3d4e5f6" (<= 30)

            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(guestProfile);

            SetStatus("Entering as guest...");

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            SetStatus("Guest login success!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (Exception e)
        {
            SetStatus("Guest login failed");
            Debug.LogException(e);
        }
        finally
        {
            busy = false;
        }
    }

    // =========================
    // Optional: Back to last user (useful if you add a button)
    // =========================
    public async void OnClickBackToLastUser()
    {
        if (busy) return;
        busy = true;

        try
        {
            string lastUser = PlayerPrefs.GetString(LastUserProfileKey, "");
            if (string.IsNullOrEmpty(lastUser))
            {
                SetStatus("No saved user on this device");
                return;
            }

            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(lastUser);

            SetStatus("Logging in...");

            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(lastUser, fixedPassword);

            SetStatus("Success!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (AuthenticationException)
        {
            SetStatus("Saved user login failed");
        }
        catch (Exception e)
        {
            SetStatus("Login failed");
            Debug.LogException(e);
        }
        finally
        {
            busy = false;
        }
    }

    // =========================
    // Helpers
    // =========================
    private string ValidateUsername()
    {
        if (usernameInput == null)
        {
            SetStatus("Username input missing");
            return null;
        }

        string username = usernameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Enter username");
            return null;
        }

        // 3-20 chars, allow letters, numbers, dot, underscore, dash, @
        if (!Regex.IsMatch(username, @"^[A-Za-z0-9._\-@]{3,20}$"))
        {
            SetStatus("Invalid username");
            return null;
        }

        return username;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    private void SaveLastUserProfile(string username)
    {
        PlayerPrefs.SetString(LastUserProfileKey, username);
        PlayerPrefs.Save();
    }
}
