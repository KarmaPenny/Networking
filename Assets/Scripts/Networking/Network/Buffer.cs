using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Networking.Network
{
    public class Buffer : MemoryStream
    {
        public Buffer() { }

        public Buffer(byte[] bytes) : base(bytes) { }

        // used to ensure consistent endianess accross systems
        static void Normalize(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
        }

        public bool Empty
        {
            get
            {
                return Length - Position == 0;
            }
        }

        #region object
        public object Read(Type T)
        {
            if (T == typeof(bool))
            {
                return ReadBool();
            }

            if (T == typeof(float))
            {
                return ReadFloat();
            }

            if (T == typeof(int))
            {
                return ReadInt();
            }

            if (T == typeof(string))
            {
                return ReadString();
            }

            if (T == typeof(Quaternion))
            {
                return ReadQuaternion();
            }

            if (T == typeof(Vector2))
            {
                return ReadVector2();
            }

            if (T == typeof(Vector3))
            {
                return ReadVector3();
            }

            return null;
        }

        public void Write(object obj)
        {
            if (obj is bool)
            {
                WriteBool((bool)obj);
            }
            else if (obj is float)
            {
                WriteFloat((float)obj);
            }
            else if (obj is int)
            {
                WriteInt((int)obj);
            }
            else if (obj is string)
            {
                WriteString((string)obj);
            }
            else if (obj is Quaternion)
            {
                WriteQuaternion((Quaternion)obj);
            }
            else if (obj is Vector2)
            {
                WriteVector2((Vector2)obj);
            }
            else if (obj is Vector3)
            {
                WriteVector3((Vector3)obj);
            }
        }
        #endregion

        #region bool
        public bool ReadBool()
        {
            int b = ReadByte();
            return b > 0;
        }

        public void WriteBool(bool value)
        {
            byte b = 0;
            if (value)
            {
                b = 1;
            }
            WriteByte(b);
        }
        #endregion

        #region byte[]
        public byte[] ReadAll() {
            return ReadBytes((int)(Length - Position));
        }

        public byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            Read(bytes, 0, count);
            return bytes;
        }

        public void WriteBytes(byte[] bytes)
        {
            Write(bytes, 0, bytes.Length);
        }
        #endregion

        #region float
        public float ReadFloat()
        {
            byte[] b = ReadBytes(sizeof(float));
            Normalize(b);
            return BitConverter.ToSingle(b, 0);
        }

        public void WriteFloat(float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Normalize(b);
            WriteBytes(b);
        }
        #endregion

        #region int
        public int ReadInt()
        {
            byte[] b = ReadBytes(sizeof(int));
            Normalize(b);
            return BitConverter.ToInt32(b, 0);
        }

        public void WriteInt(int value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Normalize(b);
            WriteBytes(b);
        }
        #endregion

        #region string
        public string ReadString()
        {
            // read until terminating null character
            MemoryStream stream = new MemoryStream();
            int b;
            while ((b = ReadByte()) > 0)
            {
                stream.WriteByte((byte)b);
            }

            // convert to ascii string
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        public void WriteString(string value)
        {
            // write ascii encoded string
            WriteBytes(Encoding.ASCII.GetBytes(value));

            // add terminating null character
            WriteByte(0);
        }
        #endregion

        #region Quaternion
        public Quaternion ReadQuaternion()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        public void WriteQuaternion(Quaternion value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
            WriteFloat(value.w);
        }
        #endregion

        #region Vector2
        public Vector2 ReadVector2()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            return new Vector2(x, y);
        }
        
        public void WriteVector2(Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }
        #endregion

        #region Vector3
        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }

        public void WriteVector3(Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }
        #endregion
    }
}