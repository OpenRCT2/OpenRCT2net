using System.IO;
using System.Text;

namespace OpenRCT2.Network.Extensions
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadUTF8(this BinaryReader br)
        {
            var ms = new MemoryStream(capacity: 32);

            byte ch;
            while ((ch = br.ReadByte()) != 0)
            {
                ms.WriteByte(ch);
            }

            byte[] utf8text = ms.ToArray();
            string text = Encoding.UTF8.GetString(utf8text, 0, utf8text.Length);
            return text;
        }
    }
}
