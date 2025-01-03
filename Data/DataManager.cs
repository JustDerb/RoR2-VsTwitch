using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace VsTwitch.Data
{
    internal class DataManager
    {
        private readonly static string FILE_EXTENSION = ".json";

        private readonly string filePath;
        private readonly Dictionary<string, object> backingDictionary;

        private DataManager(string filePath, Dictionary<string, object> backingDictionary)
        {
            this.filePath = filePath;
            this.backingDictionary = backingDictionary;
        }

        public static DataManager LoadForModule(string basePath, string moduleName)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            string filePath = Path.Combine(basePath, $"{moduleName}{FILE_EXTENSION}");
            Dictionary<string, object> backingDictionary;
            if (File.Exists(filePath))
            {
                string fileText = File.ReadAllText(filePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileText);
                backingDictionary = new Dictionary<string, object>(dict);
            }
            else
            {
                backingDictionary = new Dictionary<string, object>();
            }
            return new DataManager(filePath, backingDictionary);
        }

        public T Get<T>(string key)
        {
            return (T) backingDictionary[key];
        }

        public bool Contains(string key)
        {
            return backingDictionary.ContainsKey(key);
        }

        public void Set(string key, object value)
        {
            backingDictionary[key] = value;
            Save();
        }

        public void Save()
        {
            string serializedText = JsonConvert.SerializeObject(backingDictionary, Formatting.Indented);
            File.WriteAllText(filePath, serializedText);
        }
    }
}
