#nullable enable
namespace Library.Models;

public readonly record struct LibraryRightClickTrigger(bool IsController = false, string? Metadata = null);
