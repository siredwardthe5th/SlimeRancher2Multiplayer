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
    // GUI.TextField calls internally. Each TextField invocation throws an
    // unstripping-failed exception in the IL2CPP-to-managed trampoline -
    // the call still returns, but the log fills with thousands of these.
    // Render a read-only Label instead. Editing input values requires
    // editing the SR2MP MelonPreferences config until Il2CppInterop ships
    // an unstrip implementation for TextEditor.SaveBackup.
    private string SafeTextField(Rect rect, string current)
    {
        // Il2CppInterop's GUIStyle wrapper has no copy ctor; use parameterless.
        var style = new GUIStyle();
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        GUI.Label(rect, string.IsNullOrEmpty(current) ? " " : current, style);
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