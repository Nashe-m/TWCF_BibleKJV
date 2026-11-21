namespace TWCF_BibleApp.Domain.Enums
{
    /// <summary>How the text is laid out while projecting.</summary>
    public enum ProjectorDisplay
    {
        // The whole chapter, scrolled by hand.
        Scroll = 0,
        // One verse filling the screen, stepped through with the keys, a clicker or a tap.
        SingleVerse = 1
    }
}
