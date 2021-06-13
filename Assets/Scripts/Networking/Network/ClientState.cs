namespace Networking.Network {
    public class ClientState : ISerializable {
        public int clientFrame;
        public int topServerFrame;
        // TODO: add character state
        public InputState inputState = new InputState();

        public byte[] Serialize() {
            Buffer buffer = new Buffer();
            buffer.WriteInt(clientFrame);
            buffer.WriteInt(topServerFrame);
            buffer.WriteBytes(inputState.Serialize());
            return buffer.ToArray();
        }

        public void Deserialize(Buffer buffer) {
            clientFrame = buffer.ReadInt();
            topServerFrame = buffer.ReadInt();
            inputState.Deserialize(buffer);
        }
    }
}