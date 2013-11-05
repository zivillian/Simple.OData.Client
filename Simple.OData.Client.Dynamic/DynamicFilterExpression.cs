﻿using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Simple.OData.Client
{
    public class DynamicFilterExpression : FilterExpression,  IDynamicMetaObjectProvider
    {
        internal DynamicFilterExpression()
        {
        }

        protected DynamicFilterExpression(object value)
            : base(value)
        {
        }

        protected DynamicFilterExpression(string reference)
            : base(reference)
        {
        }

        protected DynamicFilterExpression(ExpressionFunction function)
            : base(function)
        {
        }

        protected DynamicFilterExpression(FilterExpression left, FilterExpression right, ExpressionOperator expressionOperator)
            : base(left, right, expressionOperator)
        {
        }

        protected DynamicFilterExpression(FilterExpression caller, string reference)
            : base(caller, reference)
        {
        }

        protected DynamicFilterExpression(FilterExpression caller, ExpressionFunction function)
            : base(caller, function)
        {
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicFilterExpression.DynamicDictionaryMetaObject(parameter, this);
        }

        private class DynamicDictionaryMetaObject : DynamicMetaObject
        {
            internal DynamicDictionaryMetaObject(
                Expression parameter,
                DynamicFilterExpression value)
                : base(parameter, BindingRestrictions.Empty, value)
            {
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                ConstructorInfo ctor;
                Expression[] ctorArguments;
                FunctionMapping mapping;
                if (FunctionMapping.SupportedFunctions.TryGetValue(new ExpressionFunction.FunctionCall(binder.Name, 0), out mapping))
                {
                    ctor = CtorWithFilterExpressionAndString;
                    ctorArguments = new[] { Expression.Constant(this.Value), Expression.Constant(binder.Name) };
                }
                else
                {
                    ctor = CtorWithString;
                    ctorArguments = new[] { Expression.Constant(binder.Name) };
                }

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindInvokeMember(
                InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                FunctionMapping mapping;
                if (FunctionMapping.SupportedFunctions.TryGetValue(new ExpressionFunction.FunctionCall(binder.Name, args.Count()), out mapping))
                {
                    var expression = Expression.New(CtorWithFilterExpressionAndExpressionFunction,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ExpressionFunction(binder.Name, args.Select(x => x.Value)))
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else
                {
                    return base.BindInvokeMember(binder, args);
                }
            }
        }

        private static IEnumerable<ConstructorInfo> GetConstructorInfo()
        {
            return _ctors ??
                (_ctors = typeof(DynamicFilterExpression).GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        private static ConstructorInfo CtorWithString
        {
            get
            {
                return _ctorWithString ??
                    (_ctorWithString = GetConstructorInfo().Single(x =>
                    x.GetParameters().Count() == 1 &&
                    x.GetParameters()[0].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithFilterExpressionAndString
        {
            get
            {
                return _ctorWithFilterExpressionAndString ??
                       (_ctorWithFilterExpressionAndString = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(FilterExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithFilterExpressionAndExpressionFunction
        {
            get
            {
                return _ctorWithFilterExpressionAndExpressionFunction ??
                       (_ctorWithFilterExpressionAndExpressionFunction = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(FilterExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(ExpressionFunction)));
            }
        }

        private static ConstructorInfo[] _ctors;
        private static ConstructorInfo _ctorWithString;
        private static ConstructorInfo _ctorWithFilterExpressionAndString;
        private static ConstructorInfo _ctorWithFilterExpressionAndExpressionFunction;
    }
}