using MelonLoader;
using SR2E.Utils;

namespace SR2MP.Components.UI;

// TODO: Asset bundle
[RegisterTypeInIl2Cpp(false)]
public sealed partial class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance { get; private set; }

    private bool didUnfocus = false;
    private int _focusedField = -1;
    private const int ChatFieldId = 4;

    private void Awake()
    {
        firstTime = Main.SetupUI;
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

    private string TextField(Rect rect, string text, int id, int maxLength = -1, string placeholder = null)
    {
        var e = Event.current;
        bool focused = _focusedField == id;

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            _focusedField = id;
            e.Use();
        }

        if (focused && e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Backspace:
                    if (text.Length > 0)
                        text = text[..^1];
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Escape:
                    if (id != ChatFieldId)
                        _focusedField = -1;
                    break;
                default:
                    if (e.character != '\0' && e.character != '\n' && !char.IsControl(e.character))
                    {
                        if (maxLength < 0 || text.Length < maxLength)
                            text += e.character;
                        e.Use();
                    }
                    break;
            }
        }

        if (placeholder != null && string.IsNullOrEmpty(text) && !focused)
        {
            var prevColor = GUI.contentColor;
            GUI.contentColor = Color.gray;
            GUI.Label(rect, placeholder, GUI.skin.textField);
            GUI.contentColor = prevColor;
        }
        else
        {
            GUI.Label(rect, focused ? text + "|" : text, GUI.skin.textField);
        }

        return text;
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.MouseDown)
            _focusedField = -1;

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