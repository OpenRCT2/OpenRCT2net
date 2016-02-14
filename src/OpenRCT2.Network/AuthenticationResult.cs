namespace OpenRCT2.Network
{
    public enum AuthenticationResult : uint
    {
        None,
        Requested,
        OK,
        BadVersion,
        BadName,
        BadPassword,
        Full,
        RequirePassword,
    }
}
