using System.Collections.Immutable;
using System.IO;

namespace OpenRCT2.Network
{
    internal class PlayerListPacket
    {
        public ImmutableArray<Player> Players { get; }

        internal PlayerListPacket(BinaryReader br)
        {
            var players = ImmutableArray.CreateBuilder<Player>();

            byte numPlayers = br.ReadByte();
            for (int i = 0; i < numPlayers; i++)
            {
                var player = new Player(br);
                players.Add(player);
            }

            Players = players.ToImmutable();
        }
    }
}
