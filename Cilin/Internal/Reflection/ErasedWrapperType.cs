﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class ErasedWrapperType : TypeDelegator {
        public ErasedWrapperType(Type runtimeType, InterpretedType fullType) : base(runtimeType) {
            RuntimeType = runtimeType;
            FullType = fullType;
        }

        public Type RuntimeType { get; }
        public InterpretedType FullType { get; }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
            return RewriteConstructor(base.GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers));
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
            return RewriteConstructors(base.GetConstructors(bindingAttr));
        }

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) {
            return RewriteConstructors(base.GetMember(name, type, bindingAttr));
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
            return RewriteConstructors(base.GetMembers(bindingAttr));
        }

        public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria) {
            return RewriteConstructors(base.FindMembers(memberType, bindingAttr, filter, filterCriteria));
        }

        private TMember[] RewriteConstructors<TMember>(TMember[] members)
            where TMember: MemberInfo
        {
            for (var i = 0; i < members.Length; i++) {
                var constructor = members[i] as ConstructorInfo;
                if (constructor != null)
                    members[i] = (TMember)(object)new ErasedWrapperConstructor(this, constructor);
            }
            return members;
        }
        
        private TMember RewriteConstructor<TMember>(TMember member)
            where TMember : MemberInfo 
        {
            var constructor = member as ConstructorInfo;
            return constructor != null 
                 ? (TMember)(object)new ErasedWrapperConstructor(this, constructor)
                 : member;
        }
    }
}