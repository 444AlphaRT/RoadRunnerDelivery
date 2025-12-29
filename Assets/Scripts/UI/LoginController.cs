using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainManu";

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
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (username == "" || password == "")
            {
                SetStatus("Enter username and password");
                return;
            }

            SetStatus("Registering...");

            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(username, password);

            SetStatus("Registered!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (Exception e)
        {
            SetStatus("Register failed");
            Debug.Log(e);
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
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (username == "" || password == "")
            {
                SetStatus("Enter username and password");
                return;
            }

            SetStatus("Logging in...");

            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, password);

            SetStatus("Success!");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (Exception e)
        {
            SetStatus("Login failed");
            Debug.Log(e);
        }
        finally
        {
            busy = false;
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}