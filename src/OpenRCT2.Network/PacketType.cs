namespace OpenRCT2.Network
{
    internal enum PacketType : uint
    {
        Auth,
        Map,
        Chat,
        GameCommand,
        Tick,
        PlayerList,
        Ping,
        PingList,
        SetDisconnectMessage,
        GameInfo,
        ShowError,
        GroupList,
    }
}
