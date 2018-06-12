using System;
using System.Linq;
using Newtonsoft.Json;

namespace TypeBuildingJsonDeserializer
{
    internal class TypeBuildingJsonConverter : JsonConverter
    {
        private readonly ImplementationBuilder typeBuilder = new ImplementationBuilder();        
        public void AddKnownType(Type interfaceType, Type implementationType) => typeBuilder.TypeMap.Add(interfaceType, implementationType);
        public void AddKnownType<TInterface, TImplementation>() => AddKnownType(typeof(TInterface), typeof(TImplementation));
        public override bool CanConvert(Type objectType) => typeBuilder.CanBuild(objectType);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => serializer.Deserialize(reader, typeBuilder.GenerateType(objectType));
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();
    }
}