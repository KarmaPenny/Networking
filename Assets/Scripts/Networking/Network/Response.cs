namespace Networking.Network {
    public class Response<T> where T : ISerializable, new()
    {
        public string address;
        public Message<T> message;

        public Response(string address, byte[] data)
        {
            this.address = address;
            message = new Message<T>();
            Buffer buffer = new Buffer(data);
            message.Deserialize(buffer);
        }
    }
}