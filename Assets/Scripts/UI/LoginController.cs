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

    private bool busy = false;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (statusText != null)
            statusText.text = "";
    }

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

    private string ValidateUsername()
    {
        string username = usernameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Enter username");
            return null;
        }

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
