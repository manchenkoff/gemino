using System.IO; //библиотека для работы с потоками памяти
using System.Runtime.Serialization.Formatters.Binary; //библиотека для работы с сериализацией

namespace Sync {
    public class Serializer {
        /// <summary>
        /// Метод для преобразования объекта в массив байтов
        /// </summary>
        /// <param name="anyObject">Объект любого типа</param>
        /// <returns>Возвращает массив байтов, полученных из MemoryStream</returns>
        public static byte[] Serialize(object anyObject) {
            //используем memoryStream для временного хранения сериализуемых данных
            using (var memoryStream = new MemoryStream()) {
                //создаем бинарный преобразователь и вызываем метод сериализации для anyObject
                (new BinaryFormatter()).Serialize(memoryStream, anyObject);
                //возвращаем готовый массив байтов из потока memoryStream
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Метод для преобразования массива байтов в объект для последующей конвертации
        /// </summary>
        /// <param name="array">Массив для преобразования</param>
        /// <returns>Возвращает переменную типа Object</returns>
        public static object Deserialize(byte[] array) {
            //используем memoryStream для временного хранения сериализуемых данных
            using (var memoryStream = new MemoryStream(array))
                //возвращаем результат выполнения функции десериализации массива байтов из потока memoryStream
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }
    }
}
