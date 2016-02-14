namespace OpenRCT2.Network.Extensions.Endian
{
    internal static class EndianExtensions
    {
        public static ushort ToLittleEndian(this ushort be)
        {
            return (ushort)((be << 8) | (be >> 8));
        }

        public static uint ToLittleEndian(this uint be)
        {
            return ((be << 24) & 0xFF000000) |
                   ((be <<  8) & 0x00FF0000) |
                   ((be >>  8) & 0x0000FF00) |
                   ((be >> 24) & 0x000000FF);
        }

        public static ushort ToBigEndian(this ushort le)
        {
            return (ushort)((le << 8) | (le >> 8));
        }

        public static uint ToBigEndian(this uint le)
        {
            return ((le << 24) & 0xFF000000) |
                   ((le <<  8) & 0x00FF0000) |
                   ((le >>  8) & 0x0000FF00) |
                   ((le >> 24) & 0x000000FF);
        }
    }
}
