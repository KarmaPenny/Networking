namespace Networking.Network
{
    public interface ISerializable
    {
        byte[] Serialize();
        void Deserialize(Buffer buffer);
    }
}