using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace ClipyWin.Utilities;

public static class Loc
{
    public static string Culture { get; private set; } = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> _dict = new()
    {
        ["en"] = new()
        {
            ["app.name"] = "Clipy",
            ["menu.preferences"] = "Preferences...",
            ["menu.editSnippets"] = "Edit Snippets...",
            ["menu.snippets"] = "Snippets",
            ["menu.clearHistory"] = "Clear History",
            ["menu.quit"] = "Quit Clipy",
            ["menu.noSnippets"] = "(no snippets)",
            ["menu.noHistory"] = "(history is empty)",
            ["confirm.clearHistoryTitle"] = "Clear history",
            ["confirm.clearHistoryBody"] = "Remove all clipboard items?",
            ["prefs.title"] = "Clipy Preferences",
            ["prefs.tab.general"] = "General",
            ["prefs.tab.menu"] = "Menu",
            ["prefs.tab.excluded"] = "Excluded Apps",
            ["prefs.tab.shortcuts"] = "Shortcuts",
            ["prefs.tab.updates"] = "Updates",
            ["prefs.close"] = "Close",
            ["prefs.language"] = "Language:",
            ["snippets.title"] = "Snippets",
            ["snippets.addFolder"] = "Add Folder",
            ["snippets.addSnippet"] = "Add Snippet",
            ["snippets.delete"] = "Delete",
            ["snippets.title.field"] = "Title:",
            ["snippets.content.field"] = "Content:",
            ["snippets.import"] = "Import XML...",
            ["snippets.export"] = "Export XML...",
        },
        ["tr"] = new()
        {
            ["app.name"] = "Clipy",
            ["menu.preferences"] = "Tercihler...",
            ["menu.editSnippets"] = "Snippet'leri Düzenle...",
            ["menu.snippets"] = "Snippet'ler",
            ["menu.clearHistory"] = "Geçmişi Temizle",
            ["menu.quit"] = "Clipy'yi Kapat",
            ["menu.noSnippets"] = "(snippet yok)",
            ["menu.noHistory"] = "(geçmiş boş)",
            ["confirm.clearHistoryTitle"] = "Geçmişi temizle",
            ["confirm.clearHistoryBody"] = "Tüm panoya kopyalananlar silinsin mi?",
            ["prefs.title"] = "Clipy Tercihleri",
            ["prefs.tab.general"] = "Genel",
            ["prefs.tab.menu"] = "Menü",
            ["prefs.tab.excluded"] = "Hariç Tutulan Uygulamalar",
            ["prefs.tab.shortcuts"] = "Kısayollar",
            ["prefs.tab.updates"] = "Güncellemeler",
            ["prefs.close"] = "Kapat",
            ["prefs.language"] = "Dil:",
            ["snippets.title"] = "Snippet'ler",
            ["snippets.addFolder"] = "Klasör Ekle",
            ["snippets.addSnippet"] = "Snippet Ekle",
            ["snippets.delete"] = "Sil",
            ["snippets.title.field"] = "Başlık:",
            ["snippets.content.field"] = "İçerik:",
            ["snippets.import"] = "XML İçe Aktar...",
            ["snippets.export"] = "XML Dışa Aktar...",
        },
    };

    public static void Set(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) culture = "en";
        culture = culture.ToLowerInvariant();
        if (!_dict.ContainsKey(culture)) culture = "en";
        Culture = culture;
        try
        {
            var ci = CultureInfo.GetCultureInfo(culture == "tr" ? "tr-TR" : "en-US");
            Thread.CurrentThread.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;
        }
        catch { }
    }

    public static string T(string key)
    {
        if (_dict.TryGetValue(Culture, out var d) && d.TryGetValue(key, out var v)) return v;
        return _dict["en"].TryGetValue(key, out var en) ? en : key;
    }

    public static IReadOnlyList<(string Code, string Name)> SupportedLanguages { get; } = new[]
    {
        ("en", "English"),
        ("tr", "Türkçe"),
    };
}
