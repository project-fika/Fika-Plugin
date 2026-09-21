using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EFT;
using Il2CppInterop.Runtime.InteropTypes;

namespace Fika.Core.Networking.Http;

public static class FikaJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        Converters = { new MongoIdConverter(), new EnumConverterFactory(), new GameObjectConverterFactory() }
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    private sealed class MongoIdConverter : JsonConverter<MongoID>
    {
        public override MongoID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new MongoID(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, MongoID value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private sealed class EnumConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(typeToConvert));
        }
    }

    private sealed class EnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse<T>(reader.GetString(), true);
            }

            return (T)Enum.ToObject(typeof(T), reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt64(value));
        }
    }

    private sealed class GameObjectConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Il2CppObjectBase).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(MongoID)
                && typeToConvert.Assembly != typeof(FikaJson).Assembly;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(GameObjectConverter<>).MakeGenericType(typeToConvert));
        }
    }

    private sealed class GameObjectConverter<T> : JsonConverter<T> where T : Il2CppObjectBase
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            MainThread.AttachToIl2Cpp();
            return JsonExtensions.ParseJsonTo<T>(document.RootElement.GetRawText());
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            MainThread.AttachToIl2Cpp();
            writer.WriteRawValue(JsonExtensions.ToJson(value));
        }
    }
}
