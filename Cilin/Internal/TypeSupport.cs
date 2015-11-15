﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;

namespace Cilin.Internal {
    public static class TypeSupport {
        public static class References {
            private static ModuleDefinition _mscorlib = ModuleDefinition.ReadModule(typeof(object).Assembly.Location);

            public static TypeReference IEnumerable { get; } = GetTypeFromMscorlib(typeof(IEnumerable));
            public static TypeReference IEnumerableOfT { get; } = GetTypeFromMscorlib(typeof(IEnumerable<>));
            public static TypeReference ICollection { get; } = GetTypeFromMscorlib(typeof(ICollection));
            public static TypeReference IReadOnlyCollectionOfT { get; } = GetTypeFromMscorlib(typeof(IReadOnlyCollection<>));
            public static TypeReference ICollectionOfT { get; } = GetTypeFromMscorlib(typeof(ICollection<>));
            public static TypeReference IList { get; } = GetTypeFromMscorlib(typeof(IList));
            public static TypeReference IReadOnlyListOfT { get; } = GetTypeFromMscorlib(typeof(IReadOnlyList<>));
            public static TypeReference IListOfT { get; } = GetTypeFromMscorlib(typeof(IList<>));

            private static TypeReference GetTypeFromMscorlib(Type type) {
                var result = _mscorlib.GetType(type.Namespace + "." + type.Name);
                if (result == null)
                    throw new Exception($"Failed to find {type} in mscorlib.");

                return result;
            }
        }

        public static object Convert(object value, Type requiredType) {
            if (value == null) {
                if (requiredType == typeof(bool))
                    return false;

                return value; // we are assuming it's convertible as IL should be checked for us
            }

            var typeOfValue = GetTypeOf(value);
            if (IsAssignableFrom(requiredType, typeOfValue))
                return value;

            if (requiredType == typeof(bool)) {
                if (value is int)
                    return (int)value != 0;

                if (!typeOfValue.IsValueType)
                    return true; // not null as null check is separate above
            }

            if (requiredType == typeof(int)) {
                if (value is sbyte)
                    return (int)(sbyte)value;

                if (value is short)
                    return (int)(short)value;

                if (value is long)
                    return (int)(long)value;

                if (value is byte)
                    return (int)(byte)value;

                if (value is ushort)
                    return (int)(ushort)value;

                if (value is uint)
                    return (int)(uint)value;

                if (value is ulong)
                    return (int)(ulong)value;

                if (value is IntPtr)
                    return (int)(IntPtr)value;
            }

            if (requiredType == typeof(IntPtr)) {
                if (value is sbyte)
                    return (IntPtr)(sbyte)value;

                if (value is short)
                    return (IntPtr)(short)value;

                if (value is int)
                    return (IntPtr)(int)value;

                if (value is long)
                    return (IntPtr)(long)value;

                if (value is byte)
                    return (IntPtr)(byte)value;

                if (value is ushort)
                    return (IntPtr)(ushort)value;

                if (value is uint)
                    return (IntPtr)(uint)value;

                if (value is ulong)
                    return (IntPtr)(ulong)value;
            }

            throw new NotImplementedException($"Conversion from {value} (type {typeOfValue}) to {requiredType} is not implemented.");
        }

        public static bool IsAssignableFrom(Type targetType, Type sourceType) {
            if (targetType == typeof(object))
                return true;

            targetType = (targetType as ErasedWrapperType)?.FullType ?? targetType;
            sourceType = (sourceType as ErasedWrapperType)?.FullType ?? sourceType;
            if (targetType == sourceType)
                return true;

            if (!(targetType is INonRuntimeType) && !(sourceType is INonRuntimeType))
                return targetType.IsAssignableFrom(sourceType);

            if (sourceType.IsSubclassOf(targetType))
                return true;

            if (targetType.IsInterface) {
                var interfaces = sourceType.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (((@interface as ErasedWrapperType)?.FullType ?? @interface) == targetType)
                        return true;
                }
            }

            if (targetType.IsGenericParameter) {
                var constraints = targetType.GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++) {
                    if (!IsAssignableFrom(constraints[i], sourceType))
                        return false;
                }

                return true;
            }

            return false;
        }

        public static object CreateArray(Type arrayType, int length) {
            var erased = arrayType as ErasedWrapperType;
            if (erased != null)
                return new ObjectTypeOverride(Array.CreateInstance(erased.RuntimeType.GetElementType(), length), erased.FullType);

            return Array.CreateInstance(arrayType.GetElementType(), length);
        }

        public static IEnumerable<TypeReference> GetArrayInterfaces(TypeReference elementType) {
            yield return References.IEnumerable;
            yield return References.ICollection;
            yield return References.IList;
            yield return MakeGeneric(References.IEnumerableOfT, elementType);
            yield return MakeGeneric(References.IReadOnlyCollectionOfT, elementType);
            yield return MakeGeneric(References.ICollectionOfT, elementType);
            yield return MakeGeneric(References.IReadOnlyListOfT, elementType);
            yield return MakeGeneric(References.IListOfT, elementType);
        }

        private static TypeReference MakeGeneric(TypeReference openType, TypeReference genericArgument) {
            return new GenericInstanceType(openType) { GenericArguments = { genericArgument }};
        }

        public static T Convert<T>(object value) {
            return (T)Convert(value, typeof(T));
        }

        public static object GetDefaultValue(Type type) {
            if (!type.IsValueType)
                return null;

            var interpretedType = type as InterpretedType;
            if (interpretedType == null)
                return Activator.CreateInstance(type);

            return new CilinObject(interpretedType);
        }

        public static Type GetTypeOf(object @object) {
            Argument.NotNull(nameof(@object), @object);
            return (@object as ITypeOverride)?.Type ?? @object.GetType();
        }

        public static string GetFullName(TypeReference reference) {
            if (reference.IsGenericParameter || reference.IsGenericInstance)
                throw new NotSupportedException();

            return AppendFullName(new StringBuilder(), reference).ToString();
        }

        private static StringBuilder AppendFullName(StringBuilder builder, TypeReference reference) {
            if (!string.IsNullOrEmpty(reference.Namespace))
                builder.Append(reference.Namespace).Append('.');

            if (reference.IsNested)
                AppendFullName(builder, reference.DeclaringType).Append('+');

            builder.Append(reference.Name);
            return builder;
        }
    }
}
