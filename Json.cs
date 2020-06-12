using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace AcegikmoDiscordBot
{
    internal class Json<T> where T : new()
    {
        private static readonly DataContractJsonSerializer SimpleSerializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true });
        private static readonly DataContractJsonSerializer OldSerializer = new DataContractJsonSerializer(typeof(T));
        private readonly string _jsonFile;
        public T Data { get; }

        public Json(string jsonFile)
        {
            try
            {
                try
                {
                    using var stream = File.OpenRead(jsonFile);
                    Console.WriteLine("Reading simple");
                    Data = (T)SimpleSerializer.ReadObject(stream);
                    Console.WriteLine("Reading simple success: " + Data);
                }
                catch
                {
                    using var stream = File.OpenRead(jsonFile);
                    Console.WriteLine("Reading old");
                    Data = (T)OldSerializer.ReadObject(stream);
                    Console.WriteLine("Reading old success: " + Data);
                }
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
            SimpleSerializer.WriteObject(stream, Data);
        }
    }
}
