namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private Rect previousLayoutRect;
    private Rect previousLayoutChatRect;
    private int previousLayoutHorizontalIndex;

    private void DrawText(string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        GUI.Label(CalculateTextLayout(6, text, horizontalShare, horizontalIndex), text);
    }

    // SR2 1.2.0 / Unity 6 strips UnityEngine.TextEditor.SaveBackup, which
    // GUI.TextField calls internally. We can't use GUI.TextField on this
    // build, so we drive input ourselves: render with GUI.Label (which is
    // safe — it's a no-op on non-Repaint event types internally) and read
    // Event.current to capture keystrokes. Bypasses TextEditor entirely.
    //
    // Critical IMGUI rule: every control must be invoked unconditionally on
    // EVERY event type (Layout / MouseDown / Repaint / KeyDown / ...) so
    // that internal control IDs stay in sync across passes. Earlier we
    // wrapped the render in `if (ev.type == EventType.Repaint)`, which
    // caused control IDs of subsequent buttons to drift between passes —
    // buttons stopped firing because IMGUI's MouseDown pass thought the
    // button was at a different ID than the Layout pass.
    //
    // Focus model: a single static `_focusedField` holds the controlName of
    // the currently-active field. Click any SafeTextField rect to focus it;
    // click outside (or hit Enter / Escape / Tab) to defocus.
    private static string? _focusedField;
    private static GUIStyle? _safeTextStyle;

    private string SafeTextField(Rect rect, string current, string controlName)
    {
        current ??= string.Empty;
        bool isFocused = _focusedField == controlName;
        var ev = Event.current;

        // Plain GUI.Label, no custom style — minimum chance of upsetting
        // IMGUI's internal state.
        var caret = isFocused && ((int)(UnityEngine.Time.unscaledTime * 2) % 2 == 0) ? "|" : string.Empty;
        GUI.Label(rect, current + caret);

        if (ev.type == EventType.MouseDown)
        {
            var hitMe = rect.Contains(ev.mousePosition);
            // Diagnostic — gated on flag, to be removed once button issue is solved
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-UI] MouseDown @ {ev.mousePosition} | field='{controlName}' rect={rect} hitMe={hitMe} wasFocused={isFocused}");
            if (hitMe)
            {
                _focusedField = controlName;
                ev.Use();
            }
            else if (isFocused)
            {
                _focusedField = null;
            }
        }

        if (isFocused && ev.type == EventType.KeyDown)
        {
            switch (ev.keyCode)
            {
                case KeyCode.Backspace:
                    if (current.Length > 0) current = current.Substring(0, current.Length - 1);
                    ev.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Escape:
                case KeyCode.Tab:
                    _focusedField = null;
                    ev.Use();
                    break;
                default:
                    var ch = ev.character;
                    if (ch >= 32 && ch != 127)
                    {
                        current += ch;
                        ev.Use();
                    }
                    break;
            }
        }

        return current;
    }

    private Rect CalculateTextLayout(float originalX, string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);
        var style = GUI.skin.label;
        var height = style.CalcHeight(new GUIContent(text), maxWidth / horizontalShare);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = maxWidth / horizontalShare;
        float h = height;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }

    private Rect CalculateInputLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = maxWidth / horizontalShare;
        const float h = InputHeight;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }

    private Rect CalculateButtonLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = maxWidth / horizontalShare;
        const float h = ButtonHeight;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }
}