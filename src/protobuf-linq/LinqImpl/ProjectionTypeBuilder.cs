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

namespace ProtoBuf.LinqImpl
{
    public sealed class ProjectionTypeBuilder
    {
        private static readonly ConditionalWeakTable<RuntimeTypeModel, ProjectionTypeBuilder> Builders = new ConditionalWeakTable<RuntimeTypeModel, ProjectionTypeBuilder>();
        private const TypeAttributes ProjectionTypeAttributes = TypeAttributes.AnsiClass|TypeAttributes.AutoClass|TypeAttributes.Class|TypeAttributes.BeforeFieldInit|TypeAttributes.Public;
        private readonly ModuleBuilder _module;
        private readonly RuntimeTypeModel _model;
        private readonly ConcurrentDictionary<Key, Type> _cache = new ConcurrentDictionary<Key, Type>();
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
            var key = new Key(originalDeserializedType, projectedMembers);
            return _cache.GetOrAdd(key, BuildType);
        }

        private Type BuildType(Key key)
        {
            var originalTypeModel = _model[key.Type];

            // the new type should have all the needed fields up to the root of the hierarchy
            // all other branches of the tree may not have any members, as they are skipped in the projection
            // NOTE: possible way of optimization is finding selected members across differen _model & different levels, to skip some types being created

            var str = new StringBuilder(1024)
                .Append(originalTypeModel.Type.FullName)
                .Append(string.Join(",", key.TypeMembers.Select(m => m.Name)))
                .ToString();

            string uniqueTypeSuffix;
            using (var sha1 = SHA1.Create())
            {
                uniqueTypeSuffix= BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", "");
            }

            var groupedByType = key.TypeMembers.GroupBy(mi => mi.DeclaringType).ToDictionary(g=>g.Key, g=>g);
            var root = originalTypeModel.GetHierarchyRoot();

            var rootType = BuildType(root, typeof (object), t => _model.Add(t, false), uniqueTypeSuffix, groupedByType);

            return FindInSubtypes(rootType, key.Type);
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

            // define fields which are members of this projection in the new type, with the same type and name as members in the original
            IGrouping<Type, MemberInfo> membersForThisType;
            if (members.TryGetValue(typeBeingTransformed, out membersForThisType))
            {
                foreach (var mi in membersForThisType)
                {
                    DefineField(mi, typeBuilder);
                }
            }

            var type = typeBuilder.CreateType();

            // build protobuf meta type
            var metaType = metaTypeBuilder(type);
            metaType.UseConstructor = false;

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
                Func<Type, MetaType> builder = t => metaType.AddSubType(s.FieldNumber, t, s.GetDataFormat());

                BuildType(originalSubtype.DerivedType, type, builder, uniqueTypeSuffix, members);
            }

            return metaType;
        }

        private static void DefineField(MemberInfo typeGiver, TypeBuilder typeBuilder)
        {
            var memberType = typeGiver is FieldInfo ? ((FieldInfo)typeGiver).FieldType : ((PropertyInfo)typeGiver).PropertyType;
            typeBuilder.DefineField(typeGiver.Name, memberType, FieldAttributes.Public);
        }

        private struct Key : IEquatable<Key>
        {
            public readonly Type Type;
            public readonly MemberInfo[] TypeMembers;

            public Key(Type type, MemberInfo[] typeMembers)
            {
                Type = type;
                TypeMembers = typeMembers.OrderBy(member => member.Name).ToArray();
            }

            public bool Equals(Key other)
            {
                if (Type != other.Type)
                    return false;

                if (TypeMembers.Length != other.TypeMembers.Length)
                    return false;

                return TypeMembers.SequenceEqual(other.TypeMembers);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                return obj is Key && Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((TypeMembers != null ? TypeMembers.Length : 0) * 397) ^
                        (Type != null ? Type.GetHashCode() : 0);
                }
            }
        }
    }
}