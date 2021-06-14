using K4os.Compression.LZ4;

namespace Networking.Network {
    public static class Compression {
        private static byte[] Compress(byte[] data) {
            return LZ4Pickler.Pickle(data);
        }

        private static byte[] Decompress(byte[] data) {
            return LZ4Pickler.Unpickle(data);
        }

        private static byte[] Delta(byte[] data, byte[] referenceData) {
            byte[] delta = new byte[data.Length];
            for (int i = 0; i < delta.Length; i++) {
                byte b = (i < referenceData.Length) ? referenceData[i] : (byte)0;
                delta[i] = (byte)(data[i] ^ b);
            }
            return delta;
        }

        public static byte[] DeltaCompress(byte[] data, byte[] referenceData) {
            return Compress(Delta(data, referenceData));
        }

        public static byte[] DeltaDecompress(byte[] compressedData, byte[] referenceData) {
            return Delta(Decompress(compressedData), referenceData);
        }
    }
}