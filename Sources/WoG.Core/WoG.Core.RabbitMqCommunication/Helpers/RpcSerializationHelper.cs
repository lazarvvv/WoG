using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WoG.Core.RabbitMqCommunication.Helpers
{
    public static class RpcSerializationHelper
    {
        public static byte[] ToByteArray<T>(T obj)
        {
            if (obj == null)
            {
                throw new InvalidOperationException("ToByteArray :: Input value cannot be null when serializing binary information.");
            }

            var stringRepresentation = JsonSerializer.Serialize(obj);

            return Encoding.UTF8.GetBytes(stringRepresentation);
        }

        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("FromByteArray :: Input value cannot be null when deserializing binary information.");
            }

            using MemoryStream ms = new(data);
            var obj = JsonSerializer.Deserialize<T>(ms)
                ?? throw new InvalidCastException("FromByteArray :: Invalid cast attempted when converting from RabbitMq.");

            return obj;
        }
    }
}
