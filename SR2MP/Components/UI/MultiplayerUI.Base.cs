using MelonLoader;
using SR2E.Utils;

namespace SR2MP.Components.UI;

// TODO: Asset bundle
[RegisterTypeInIl2Cpp(false)]
public sealed partial class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance { get; private set; }

    private bool didUnfocus = false;

    private void Awake()
    {
        // SR2 1.2.0 / Unity 6 strips TextEditor.SaveBackup, which GUI.TextField
        // depends on. Showing FirstTimeScreen would call GUI.TextField on every
        // OnGUI tick and spam tens of thousands of unstripping-failed exceptions.
        // Auto-resolve the first-time setup (username defaults to "Player") so
        // that screen never appears.
        if (Main.SetupUI)
        {
            Main.SetConfigValue("internal_setup_ui", false);
        }

        firstTime = false;
        usernameInput = Main.Username;
        allowCheatsInput = Main.AllowCheats;
        ipInput = Main.SavedConnectIP;
        portInput = Main.SavedConnectPort;
        hostPortInput = Main.SavedHostPort;

        if (Instance)
        {
            SrLogger.LogError("Tried to create instance of MultiplayerUI, but it already exists!", SrLogTarget.Both);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null!;
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.Layout)
        {
            state = GetState();
            UpdateChatVisibility();
        }

        previousLayoutRect = new Rect(6, 16, WindowWidth, 0);
        previousLayoutHorizontalIndex = 0;

        if (!MenuEUtil.isAnyMenuOpen)
        {
            didUnfocus = false;
            DrawWindow();
            DrawChat();
        }
        else if (!didUnfocus)
        {
            shouldUnfocusChat = true;
            UnfocusChat();
            didUnfocus = true;
        }
    }

    private void DrawWindow()
    {
        if (state == MenuState.Hidden) return;

        GUI.Box(new Rect(6, 6, WindowWidth, WindowHeight), "SR2MP (F4 to toggle)");

        switch (state)
        {
            case MenuState.SettingsInitial:
                FirstTimeScreen();
                break;
            case MenuState.SettingsMain:
                SettingsScreen();
                break;
            case MenuState.DisconnectedMainMenu:
                MainMenuScreen();
                break;
            case MenuState.DisconnectedInGame:
                InGameScreen();
                break;
            case MenuState.ConnectedClient:
                ConnectedScreen();
                break;
            case MenuState.ConnectedHost:
                HostingScreen();
                break;
            default:
                UnimplementedScreen();
                break;
        }

        AdjustInputValues();
    }
}