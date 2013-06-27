using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq.LinqImpl
{
    public sealed class ProjectionTypeBuilder
    {
        private static readonly ConditionalWeakTable<RuntimeTypeModel, ProjectionTypeBuilder> Builders = new ConditionalWeakTable<RuntimeTypeModel, ProjectionTypeBuilder>();
        private const TypeAttributes ProjectionTypeAttributes = TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.Class | TypeAttributes.BeforeFieldInit | TypeAttributes.Public;
        private static readonly Type[] EmptyTypes = new Type[0];

        private readonly ModuleBuilder _module;
        private readonly RuntimeTypeModel _model;
        private readonly ConcurrentDictionary<Key, MetaType> _hierarchyRootCache = new ConcurrentDictionary<Key, MetaType>();
        private readonly string _namespace;

        /// <summary>
        /// Gets the <see cref="ProjectionTypeBuilder"/> built for the specified <paramref name="model"/>.
        /// </summary>
        public static ProjectionTypeBuilder GetCachedFor(RuntimeTypeModel model)
        {
            ProjectionTypeBuilder builder;
            if (Builders.TryGetValue(model, out builder))
                return builder;

            lock (Builders)
            {
                if (Builders.TryGetValue(model, out builder))
                    return builder;

                builder = new ProjectionTypeBuilder(model);
                Builders.Add(model, builder);
                return builder;
            }
        }

        public ProjectionTypeBuilder(RuntimeTypeModel model)
        {
            _model = model;
            var uniquefier = model.GetHashCode().ToString(CultureInfo.InvariantCulture);
            _namespace = "protobuf-linq.DynamicTypes" + uniquefier;

            _module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(_namespace),
                AssemblyBuilderAccess.Run).DefineDynamicModule("main");
        }

        public Type GetTypeForProjection(Type originalDeserializedType, MemberInfo[] projectedMembers)
        {
            var key = new Key(projectedMembers);
            var hierarchyRoot = _hierarchyRootCache.GetOrAdd(key, k => BuildType(originalDeserializedType, k.Value, projectedMembers));

            return FindInSubtypes(hierarchyRoot, originalDeserializedType);
        }

        private MetaType BuildType(Type originalDeserializedType, string uniqueSuffix, MemberInfo[] members)
        {
            var originalTypeModel = _model[originalDeserializedType];
            var root = originalTypeModel.GetHierarchyRoot();

            var groupedByType = members.GroupBy(m => m.DeclaringType).ToDictionary(g => g.Key);
            return BuildType(root, typeof(object), t => _model.Add(t, false), uniqueSuffix, groupedByType);
        }

        private static Type FindInSubtypes(MetaType mt, Type type)
        {
            if (IsBasedOn(mt, type))
                return mt.Type;

            if (mt.HasSubtypes == false)
                return null;

            foreach (var subType in mt.GetSubtypes())
            {
                var found = FindInSubtypes(subType.DerivedType, type);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool IsBasedOn(MetaType mt, Type t)
        {
            return mt.Type.FullName.Contains(t.Name); // to refactor, as it's blach magic knowing that BuildType uses name.
        }

        private MetaType BuildType(MetaType originalMetaType, Type baseType, Func<Type, MetaType> metaTypeBuilder, string uniqueTypeSuffix, Dictionary<Type, IGrouping<Type, MemberInfo>> members)
        {
            var typeBeingTransformed = originalMetaType.Type;

            var typeName = _namespace + "." + typeBeingTransformed.Name + uniqueTypeSuffix;
            var typeBuilder = _module.DefineType(typeName, ProjectionTypeAttributes, baseType);

            var fields = new List<FieldBuilder>();
            // define fields which are members of this projection in the new type, with the same type and name as members in the original
            IGrouping<Type, MemberInfo> membersForThisType;
            if (members.TryGetValue(typeBeingTransformed, out membersForThisType))
            {
                foreach (var mi in membersForThisType)
                {
                    fields.Add(DefineField(mi, typeBuilder));
                }
            }

            AddProtoLinqObjectImplementation(typeBuilder, fields);

            var type = typeBuilder.CreateType();

            // build protobuf meta type
            var metaType = metaTypeBuilder(type);
            metaType.UseConstructor = false;

            //// skip construction when no members at all
            var anyMember = members.Count > 0;
            if (anyMember == false)
            {
                metaType.SetFactory(GetGetSingletonInstance(type));
            }

            // copy ValueMembers contained in the 
            var originalMetaTypeFields = originalMetaType.GetFields().ToDictionary(vm => vm.Name);
            if (membersForThisType != null)
            {
                foreach (var memberInfo in membersForThisType)
                {
                    ValueMember originalValueMember;
                    if (originalMetaTypeFields.TryGetValue(memberInfo.Name, out originalValueMember) == false)
                    {
                        throw new ArgumentException(string.Format("The projection contains a member {0} which is not a proto member. This can hide logic behind property which is currently not supported",
                                memberInfo.Name));
                    }

                    metaType.AddFieldCopy(originalValueMember);
                }
            }

            if (originalMetaType.HasSubtypes == false)
                return metaType;

            foreach (var originalSubtype in originalMetaType.GetSubtypes())
            {
                var s = originalSubtype;
                Func<Type, MetaType> builder = t =>
                    {
                        metaType.AddSubType(s.FieldNumber, t, s.GetDataFormat());
                        return metaType.GetSubtypes().Single(st => st.DerivedType.Type == t).DerivedType;
                    };

                BuildType(originalSubtype.DerivedType, type, builder, uniqueTypeSuffix, members);
            }

            return metaType;
        }

        private static void AddProtoLinqObjectImplementation(TypeBuilder typeBuilder, IEnumerable<FieldBuilder> fields)
        {
            typeBuilder.AddInterfaceImplementation(typeof (IProtoLinqObject));
            var clearMethod = typeBuilder.DefineMethod("Clear",
                MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard,
                typeof (void), EmptyTypes);

            var il = clearMethod.GetILGenerator();

            foreach (var field in fields)
            {
                if (field.FieldType.IsClass)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stfld, field);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, field);
                    il.Emit(OpCodes.Initobj, field.FieldType);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private static FieldBuilder DefineField(MemberInfo typeGiver, TypeBuilder typeBuilder)
        {
            var memberType = typeGiver is FieldInfo ? ((FieldInfo)typeGiver).FieldType : ((PropertyInfo)typeGiver).PropertyType;
            return typeBuilder.DefineField(typeGiver.Name, memberType, FieldAttributes.Public);
        }

        private static MethodInfo GetGetSingletonInstance(Type typeToGet)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            var singletonType = typeof(Singleton<>).MakeGenericType(typeToGet);
            return singletonType.GetMethod("GetInstance", flags);
        }

        private struct Key : IEquatable<Key>
        {
            public readonly string Value;

            public Key(MemberInfo[] typeMembers)
            {
                var str = new StringBuilder(1024)
                    .Append(string.Join(",", typeMembers.Select(m => m.Name)))
                    .Append(string.Join(",", typeMembers.Select(m => m.DeclaringType.FullName)))
                    .ToString();

                using (var sha1 = SHA1.Create())
                {
                    Value = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", "");
                }
            }

            public bool Equals(Key other)
            {
                return other.Value == Value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                return obj is Key && Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }
    }
}