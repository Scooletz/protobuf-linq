using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ProtoBuf.Linq.LinqImpl
{
    public class MemberInfoGatheringVisitor : ExpressionVisitor
    {
        public readonly List<MemberInfo> Members = new List<MemberInfo>();
        private readonly Type _searchedType;

        public MemberInfoGatheringVisitor(Type searchedType)
        {
            _searchedType = searchedType;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var typeOrPossiblyBaseType = node.Member.DeclaringType;
            if (typeOrPossiblyBaseType != null && typeOrPossiblyBaseType.IsAssignableFrom(_searchedType))
            {
                Members.Add(node.Member);
            }

            return base.VisitMember(node);
        }
    }
}