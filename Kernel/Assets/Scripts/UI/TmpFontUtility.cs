using TMPro;
using UnityEngine;

/// <summary>
/// Безопасная установка текста для TMP с кириллицей (шрифт с fallback-атласом).
/// </summary>
public static class TmpFontUtility
{
    private const string FallbackFontResourcePath = "Fonts & Materials/LiberationSans SDF - Fallback";

    private static TMP_FontAsset _fallbackFont;

    public static void EnsureCyrillicFont(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        TMP_FontAsset fallback = GetFallbackFont();
        if (fallback == null)
            return;

        if (text.font != fallback)
            text.font = fallback;
    }

    public static void SetText(TextMeshProUGUI text, string value)
    {
        if (text == null)
            return;

        EnsureCyrillicFont(text);
        text.SetText(value ?? string.Empty);
    }

    private static TMP_FontAsset GetFallbackFont()
    {
        if (_fallbackFont != null)
            return _fallbackFont;

        _fallbackFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
        return _fallbackFont;
    }
}
