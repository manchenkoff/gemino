using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sync {
    public class Serializer {
        public static byte[] Serialize(object anyObject) {
            using (var memoryStream = new MemoryStream()) {
                (new BinaryFormatter()).Serialize(memoryStream, anyObject);
                return memoryStream.ToArray();
            }
        }

        public static object Deserialize(byte[] array) {
            using (var memoryStream = new MemoryStream(array))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }
    }
}
