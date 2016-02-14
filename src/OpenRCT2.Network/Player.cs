using System.IO;
using OpenRCT2.Network.Extensions;

namespace OpenRCT2.Network
{
    public class Player
    {
        public string Name { get; }
        public byte Id { get; }
        public byte Flags { get; }
        public byte Group { get; }

        internal Player(BinaryReader br)
        {
            Name = br.ReadUTF8();
            Id = br.ReadByte();
            Flags = br.ReadByte();
            Group = br.ReadByte();
        }
    }
}
