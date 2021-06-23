namespace Networking.Network {
    public class ClientState : ISerializable {
        public int clientFrame;
        public int topServerFrame;
        public int nextSpawnId;
        public InputState inputState = new InputState();

        public int nextSpawnIdOffset;

        public byte[] Serialize() {
            Buffer buffer = new Buffer();
            buffer.WriteInt(clientFrame);
            buffer.WriteInt(topServerFrame);
            buffer.WriteInt(nextSpawnId);
            buffer.WriteBytes(inputState.Serialize());
            return buffer.ToArray();
        }

        public void Deserialize(Buffer buffer) {
            clientFrame = buffer.ReadInt();
            topServerFrame = buffer.ReadInt();
            nextSpawnId = buffer.ReadInt();
            inputState.Deserialize(buffer);
        }
    }
}