namespace Networking.Network {
    public class ServerState : ISerializable {
        public int serverFrame;
        public int clientFrame;
        public int topClientFrame;
        public World.State worldState = new World.State();

        public byte[] Serialize() {
            Buffer buffer = new Buffer();
            buffer.WriteInt(serverFrame);
            buffer.WriteInt(clientFrame);
            buffer.WriteInt(topClientFrame);
            buffer.WriteBytes(worldState.Serialize());
            return buffer.ToArray();
        }

        public void Deserialize(Buffer buffer) {
            serverFrame = buffer.ReadInt();
            clientFrame = buffer.ReadInt();
            topClientFrame = buffer.ReadInt();
            worldState.Deserialize(buffer);
        }
    }
}