namespace ObjectDisposedFodyAddin
{
    public enum WeavingErrorCodes : uint
    {
        None,

        NotUseable,

        ContainsBothInterface,

        MustHaveVirtualKeyword
    }
}