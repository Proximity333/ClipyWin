using System;
using System.Windows.Markup;

namespace ClipyWin.Utilities;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }
    public LocExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
}
