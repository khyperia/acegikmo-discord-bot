using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace AcegikmoDiscordBot
{
    internal class Json<T> where T : new()
    {
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(T));
        private readonly string _jsonFile;
        public T Data { get; }

        public Json(string jsonFile)
        {
            try
            {
                using var stream = File.OpenRead(jsonFile);
                Data = (T)Serializer.ReadObject(stream);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine(jsonFile + " not found, defaulting to empty dict");
                Data = new T();
            }
            _jsonFile = jsonFile;
        }

        public void Save()
        {
            using var stream = File.Create(_jsonFile);
            Serializer.WriteObject(stream, Data);
        }
    }
}
