using System.IO;
using System.Text;
using OpenRCT2.Network.Extensions.Endian;

namespace OpenRCT2.Network.Extensions
{
    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter bw, PacketType packetType)
        {
            bw.Write(((uint)packetType).ToBigEndian());
        }

        public static void WriteUTF8(this BinaryWriter bw, string s)
        {
            if (s != null)
            {
                byte[] utf8text = Encoding.UTF8.GetBytes(s);
                bw.Write(utf8text);
            }
            bw.Write((byte)0);
        }
    }
}
