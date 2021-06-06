namespace Networking.Network {
    public class Message<T> : ISerializable where T : ISerializable, new()
    {
        public int worldFrame;
        public int inputFrame;
        public T content;

        public byte[] Serialize()
        {
            Buffer buffer = new Buffer();
            buffer.WriteInt(worldFrame);
            buffer.WriteInt(inputFrame);
            buffer.WriteBytes(content.Serialize());
            return buffer.ToArray();
        }

        public void Deserialize(Buffer buffer)
        {
            worldFrame = buffer.ReadInt();
            inputFrame = buffer.ReadInt();
            content = new T();
            content.Deserialize(buffer);
        }
    }
}