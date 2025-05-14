using System.Text.Json;
using System.IO;
using System;
using OpenTap;

namespace jsonHelper
{
    public static class jsonAider
    {
        public class flags
        {
            public int sleep { get; set; }
        }
        public static void write_json<T>(T data)
        {
            string fileName = @"C:\Program Files\OpenTap\flags.json";
            string json_string = JsonSerializer.Serialize(data);
            File.WriteAllText(fileName, json_string);
            Console.WriteLine("wrote");
        }

        public static T ReadJson<T>(Func<flags, T> propertySelector)
        {
            string jsonString = File.ReadAllText(@"C:\Program Files\OpenTap\flags.json");
            flags data = JsonSerializer.Deserialize<flags>(jsonString);
            return propertySelector(data);
        }
    }
}