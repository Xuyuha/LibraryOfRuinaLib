public static class LibraryDamageTypeExpansion
{
    public static string String(this LibraryDamageType damageType)=>damageType switch
    {
        LibraryDamageType.Blunt=>"blunt",
        LibraryDamageType.Slash=>"slash",
        LibraryDamageType.Pierce=>"pierce",
        _=>"666"
    };
}
