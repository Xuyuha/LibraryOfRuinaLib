public static class LibraryDiceTypeExpansion
{
    public static string String(this LibraryDiceType damageType)=>damageType switch
    {
        LibraryDiceType.Block=>"block",
        LibraryDiceType.Blunt=>"blunt",
        LibraryDiceType.Slash=>"slash",
        LibraryDiceType.Pierce=>"pierce",
        _=>"666"
    };
}
