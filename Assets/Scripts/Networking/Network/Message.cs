namespace Networking.Network {
    public class Message<T> : ISerializable where T : ISerializable, new()
    {
        public int referenceFrame;
        public byte[] compressedData;

        public Message() {}

        public Message(StateList<T> states, int referenceFrame) {
            this.referenceFrame = referenceFrame;
            byte[] data = states.GetTop().Serialize();
            byte[] referenceData = states.Get(referenceFrame).Serialize();
            compressedData = Compression.DeltaCompress(data, referenceData);
        }

        public T GetContent(StateList<T> states) {
            byte[] referenceData = states.Get(referenceFrame).Serialize();
            byte[] data = Compression.DeltaDecompress(compressedData, referenceData);
            Buffer buffer = new Buffer(data);
            T result = new T();
            result.Deserialize(buffer);
            return result;
        }

        public byte[] Serialize()
        {
            Buffer buffer = new Buffer();
            buffer.WriteInt(referenceFrame);
            buffer.WriteBytes(compressedData);
            return buffer.ToArray();
        }

        public void Deserialize(Buffer buffer)
        {
            referenceFrame = buffer.ReadInt();
            compressedData = buffer.ReadAll();
        }
    }
}