using System;

/// <summary> An attribute that overrides the name of the type displayed in the SubclassSelector popup. </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class AddTypeMenuAttribute : Attribute
{
    static readonly char[] Separators = { '/' };

    public string MenuName { get; }
    public int Order { get; }

    public AddTypeMenuAttribute(string menuName, int order = 0)
    {
        MenuName = menuName;
        Order = order;
    }

    /// <summary> Returns the menu name split by the '/' separator. </summary>
    public string[] GetSplitMenuName()
    {
        return !string.IsNullOrWhiteSpace(MenuName) ? MenuName.Split(Separators, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
    }

    /// <summary> Returns the display name without the path. </summary>
    public string GetTypeNameWithoutPath()
    {
        var splitDisplayName = GetSplitMenuName();
        return splitDisplayName.Length != 0 ? splitDisplayName[^1] : null;
    }
}