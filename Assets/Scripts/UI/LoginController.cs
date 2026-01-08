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

    // Optional: prefix for guest profiles (keeps guest sessions separate)
    [Header("Guest")]
    [SerializeField] private string guestProfilePrefix = "guest_";

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

            // Ensure we're starting clean and using a profile per username
            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(username);

            SetStatus("Registering...");

            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(username, fixedPassword);

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
            // We create a unique profile name for a guest session.
            // Using a profile helps keep different guest sessions separate on the same device.
            string guestProfile = guestProfilePrefix + Guid.NewGuid().ToString("N");

            // Start clean (optional but recommended to avoid mixing accounts)
            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(guestProfile);

            SetStatus("Entering as guest...");

            // Anonymous sign-in creates an account without username/password.
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
}
