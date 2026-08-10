using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class WarboardSessionService : MonoBehaviour
{
    public static WarboardSessionService Instance
    {
        get;
        private set;
    }

    public ISession Session
    {
        get;
        private set;
    }

    public bool IsBusy
    {
        get;
        private set;
    }

    public string LastError
    {
        get;
        private set;
    } = "";

    public string JoinCode
    {
        get
        {
            return Session != null
                ? Session.Code
                : "";
        }
    }

    public bool IsInSession
    {
        get { return Session != null; }
    }

    public bool IsHost
    {
        get
        {
            return Session != null &&
                   Session.IsHost;
        }
    }

    public event Action SessionChanged;

    private bool servicesReady;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public async Task EnsureServicesAsync()
    {
        if (servicesReady &&
            AuthenticationService.Instance
                .IsSignedIn)
        {
            return;
        }

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance
                .IsSignedIn)
        {
            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();
        }

        servicesReady = true;
    }

    public async Task<string> HostAsync()
    {
        if (IsBusy)
            return "";

        IsBusy = true;
        LastError = "";

        try
        {
            await EnsureServicesAsync();

            EnsureNetworkManager();

            SessionOptions options =
                new SessionOptions
                {
                    MaxPlayers = 2,
                    Name = "Warboard",
                    IsPrivate = true
                }
                .WithRelayNetwork();

            Session =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            HookSession(Session);

            SessionChanged?.Invoke();

            Debug.Log(
                "Warboard multiplayer host ready. Join code: " +
                Session.Code
            );

            return Session.Code;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogException(exception);
            return "";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> JoinAsync(
        string code)
    {
        if (IsBusy)
            return false;

        code =
            (code ?? "")
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code))
        {
            LastError = "Enter a session join code.";
            return false;
        }

        IsBusy = true;
        LastError = "";

        try
        {
            await EnsureServicesAsync();

            EnsureNetworkManager();

            Session =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(code);

            HookSession(Session);

            SessionChanged?.Invoke();

            Debug.Log(
                "Joined Warboard multiplayer session " +
                Session.Id
            );

            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogException(exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LeaveAsync()
    {
        if (Session == null ||
            IsBusy)
        {
            return;
        }

        IsBusy = true;
        LastError = "";

        try
        {
            UnhookSession(Session);

            await Session.LeaveAsync();

            Session = null;

            SessionChanged?.Invoke();
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogException(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HookSession(
        ISession session)
    {
        if (session == null)
            return;

        session.PlayerJoined +=
            OnPlayerJoined;

        session.PlayerHasLeft +=
            OnPlayerLeft;

        session.SessionHostChanged +=
            OnSessionHostChanged;

        session.SessionMigrated +=
            OnSessionMigrated;

        session.RemovedFromSession +=
            OnRemovedFromSession;
    }

    private void UnhookSession(
        ISession session)
    {
        if (session == null)
            return;

        session.PlayerJoined -=
            OnPlayerJoined;

        session.PlayerHasLeft -=
            OnPlayerLeft;

        session.SessionHostChanged -=
            OnSessionHostChanged;

        session.SessionMigrated -=
            OnSessionMigrated;

        session.RemovedFromSession -=
            OnRemovedFromSession;
    }

    private void OnPlayerJoined(
        string playerId)
    {
        Debug.Log(
            "Warboard player joined: " +
            playerId
        );

        SessionChanged?.Invoke();
    }

    private void OnPlayerLeft(
        string playerId)
    {
        Debug.Log(
            "Warboard player left: " +
            playerId
        );

        SessionChanged?.Invoke();
    }

    private void OnSessionHostChanged(
        string playerId)
    {
        Debug.Log(
            "Warboard session host changed: " +
            playerId
        );

        WarboardNetworkBridge bridge =
            WarboardNetworkBridge.Instance;

        if (bridge != null)
            bridge.NotifyHostChanged();

        SessionChanged?.Invoke();
    }

    private void OnSessionMigrated()
    {
        Debug.Log(
            "Warboard session migration completed."
        );

        WarboardNetworkBridge bridge =
            WarboardNetworkBridge.Instance;

        if (bridge != null)
            bridge.NotifyHostChanged();

        SessionChanged?.Invoke();
    }

    private void OnRemovedFromSession()
    {
        Debug.LogWarning(
            "Local player was removed from the Warboard session."
        );

        Session = null;
        SessionChanged?.Invoke();
    }

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            throw new InvalidOperationException(
                "Warboard NetworkManager is missing."
            );
        }
    }
}
