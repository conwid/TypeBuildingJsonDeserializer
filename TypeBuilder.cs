using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TypeBuildingJsonDeserializer
{
    public class ImplementationBuilder
    {
        public Dictionary<Type, Type> TypeMap { get; } = new Dictionary<Type, Type>();

        private readonly AssemblyName assemblyName;
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        public ImplementationBuilder()
        {
            assemblyName = new AssemblyName("<>_ImplementationAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("<>_Implementations");
        }

        internal bool CanBuild(Type objectType)
        {
            if (!objectType.IsInterface)
                return false;
            if (objectType.GetMethods().Except(objectType.GetProperties().SelectMany(p => new[] { p.GetMethod, p.SetMethod })).Any(m => m != null))
                return false;

            return true;
        }

        public Type GenerateType<T>() => GenerateType(typeof(T));

        public Type GenerateType(Type interfaceType)
        {
            if (!CanBuild(interfaceType))
                throw new ArgumentException($"Cannot build type for {interfaceType}");

            if (TypeMap.TryGetValue(interfaceType, out var generatedType))
                return generatedType;


            var typeBuilder = moduleBuilder.DefineType($"{interfaceType.Name}Impl", TypeAttributes.Class | TypeAttributes.NotPublic, typeof(object), new Type[] { interfaceType });

            var properties = interfaceType.GetProperties();
            List<FieldBuilder> fields = new List<FieldBuilder>();

            foreach (var property in properties)
            {
                GenerateProperty(typeBuilder, fields, property);
            }

            GenerateConstructor(typeBuilder, fields, properties);
            var type = typeBuilder.CreateType();
            TypeMap.Add(interfaceType, type);
            return type;
        }

        private void GenerateProperty(TypeBuilder typeBuilder, List<FieldBuilder> fields, PropertyInfo property)
        {
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
            FieldBuilder fieldBuilder = typeBuilder.DefineField($"_{property.Name.ToLower()}", GetFieldTypeForProperty(property), FieldAttributes.Private);
            fields.Add(fieldBuilder);
            if (property.GetMethod != null)
            {
                GenerateGetter(typeBuilder, property, fieldBuilder, propertyBuilder);
            }
            if (property.SetMethod != null)
            {
                GenerateSetter(typeBuilder, property, fieldBuilder, propertyBuilder);
            }
        }

        private Type GetFieldTypeForProperty(PropertyInfo property)
        {           
            if (TypeMap.TryGetValue(property.PropertyType, out var fieldType))
                return fieldType;

            return property.PropertyType;
        }

        private void GenerateConstructor(TypeBuilder typeBuilder, List<FieldBuilder> fields, PropertyInfo[] properties)
        {
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, properties.Select(p => p.PropertyType).ToArray());
            for (int i = 0; i < properties.Length; i++)
            {
                ctor.DefineParameter(i + 1, ParameterAttributes.None, properties[i].Name);
            }
            var ctorIL = ctor.GetILGenerator();
            for (int i = 0; i < fields.Count; i++)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg, i + 1);
                ctorIL.Emit(OpCodes.Stfld, fields[i]);
            }
            ctorIL.Emit(OpCodes.Ret);
        }

        private PropertyBuilder GenerateGetter(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            MethodBuilder getterBuilder = typeBuilder.DefineMethod(property.GetMethod.Name,
                                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                                property.PropertyType,
                                Type.EmptyTypes);
            ILGenerator getterIL = getterBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(getterBuilder, property.GetMethod);
            propertyBuilder.SetGetMethod(getterBuilder);
            return propertyBuilder;
        }

        private void GenerateSetter(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            MethodBuilder setterBuilder = typeBuilder.DefineMethod(property.SetMethod.Name,
                                                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                                                null,
                                                new Type[] { property.PropertyType });
            ILGenerator setterIL = setterBuilder.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(setterBuilder, property.SetMethod);
            propertyBuilder.SetSetMethod(setterBuilder);
        }

    }
}
