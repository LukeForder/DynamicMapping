using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics.SymbolStore;
using System.Diagnostics;

namespace DynamicMapping
{
    public static class ModuleBuilderExtensions
    {
        public static Type AddMapForType(this ModuleBuilder moduleBuilder, Type toMap)
        {
            string mappedClassName = GetFriendlyMappedTypeName(toMap) ;
            string mappingClassName = mappedClassName + "Map";

            var typeBuilder = 
                moduleBuilder.DefineType(
                    mappingClassName, 
                    TypeAttributes.Class | TypeAttributes.Public,
                    typeof(ClassMap<>).MakeGenericType(toMap));

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            
            var properties = toMap.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            // you might want to check that the property is virtual as well
            
            var id = GetIdMember(mappedClassName, properties);

            if (id == null)
                throw new ApplicationException(
                        string.Format("No id could be determined by convention for {0}, Id should be {0}Id", mappedClassName));
            
            ILGenerator ctorIL = constructorBuilder.GetILGenerator();

            MapId(ctorIL, toMap, id);

            foreach (var other in properties.Except(new[] { id }))
            {
                MapProperty(ctorIL, toMap, other);
            }
            
            // all methods must be terminated with a OpCode.Ret
            ctorIL.Emit(OpCodes.Ret);

            

            return typeBuilder.CreateType();
        }
        
        private static PropertyInfo GetIdMember(string typeName, IEnumerable<PropertyInfo> properties)
        {
            return properties.FirstOrDefault(x => string.Compare(x.Name, typeName + "Id") == 0);
        }

        private static void EmitMemberAccessExpress(ILGenerator il, Type mappedType, PropertyInfo property)
        {
            var genericFunction = typeof(Func<,>).MakeGenericType(mappedType, typeof(object));

            var dec = il.DeclareLocal(typeof(ParameterExpression));
            dec.SetLocalSymInfo("parameterExpression"); // Provide name for the debugger. 

            dec = il.DeclareLocal(typeof(PropertyInfo));
            dec.SetLocalSymInfo("propertyInfo");

            dec = il.DeclareLocal(typeof(Expression));
            dec.SetLocalSymInfo("bodyExpression");

            dec = il.DeclareLocal(typeof(Type));
            dec.SetLocalSymInfo("typeOfLambda");

            dec = il.DeclareLocal(typeof(Expression<>).MakeGenericType(genericFunction));
            dec.SetLocalSymInfo("lamdba");

            dec = il.DeclareLocal(typeof(Type[]));
            dec.SetLocalSymInfo("lambdaTypeArgs");

            dec = il.DeclareLocal(typeof(ParameterExpression[]));
            dec.SetLocalSymInfo("lambdaParameter");

            // Expression.Parameter(mappedType, "x");

            il.Emit(OpCodes.Ldtoken, mappedType);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, "x");

            il.Emit(OpCodes.Call, typeof(Expression).GetMethod("Parameter", new[] { typeof(Type), typeof(string) }));
            il.Emit(OpCodes.Stloc_0);
            

            // mappedType.GetType().GetProperty(keyProperty.Name);
            il.Emit(OpCodes.Ldtoken, mappedType);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Ldstr, property.Name);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetProperty", new[] { typeof(string) }));
            il.Emit(OpCodes.Stloc_1);
            
            ////    // load them back onto stack
           il.Emit(OpCodes.Ldloc_0);
           il.Emit(OpCodes.Ldloc_1);

            //// //// Expression.MakeMemberAccess(parameterExpression, keyPropertyMemberInfo);
            il.Emit(OpCodes.Call, typeof(Expression).GetMethod("MakeMemberAccess", new [] { typeof(Expression), typeof(PropertyInfo) }));

            il.Emit(OpCodes.Ldtoken, typeof(object));
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Call, typeof(Expression).GetMethod("Convert", new[] { typeof(Expression), typeof(Type) }));
            il.Emit(OpCodes.Stloc_2);     
            
            il.Emit(OpCodes.Ldtoken, typeof(Expression<>).MakeGenericType(genericFunction));
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
            il.Emit(OpCodes.Stloc_3);

            //// new ParameterExpression[1]
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, typeof(ParameterExpression));
            il.Emit(OpCodes.Stloc_S, 6);

            //// ParameterExpression[0] = parameterExpression 
            il.Emit(OpCodes.Ldloc_S, 6);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Stelem_Ref);

            var lambdaMethod =
                 typeof(Expression)
                 .GetMethod(
                     "Lambda",
                     new[] { typeof(Type), typeof(Expression), typeof(ParameterExpression[]) });


            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldloc_S, 6);
            il.Emit(OpCodes.Call, lambdaMethod);


            il.Emit(OpCodes.Castclass,
                typeof(Expression<>)
                    .MakeGenericType(
                        typeof(Func<,>).MakeGenericType(mappedType, typeof(object))));

            il.Emit(OpCodes.Stloc_S, 4);

        }

        private static void MapProperty(ILGenerator il, Type mappedType, PropertyInfo keyProperty)
        {

            // store reference to this
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc_S, 7);

            EmitMemberAccessExpress(il, mappedType, keyProperty);

            var expressionType =
                typeof(Expression<>)
                    .MakeGenericType(
                        typeof(Func<,>).MakeGenericType(mappedType, typeof(object)));

            var methodInfo = typeof(ClassMap<>)
                .MakeGenericType(mappedType)
                .GetMethod("Map", new[] { expressionType });

            il.Emit(OpCodes.Ldloc_S, 7);
            il.Emit(OpCodes.Ldloc_S, 4);

            il.Emit(
                OpCodes.Call,
                methodInfo);
        }


        private static void MapId(ILGenerator il, Type mappedType, PropertyInfo keyProperty)
        {
         
            EmitMemberAccessExpress(il, mappedType, keyProperty);

            var expressionType =
                typeof(Expression<>)
                    .MakeGenericType(
                        typeof(Func<,>).MakeGenericType(mappedType, typeof(object)));

            var methodInfo = typeof(ClassMap<>)
                .MakeGenericType(mappedType)
                .GetMethod("Id", new[] { expressionType });

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_S, 4);
            il.Emit(
                OpCodes.Call,
                methodInfo);
        }
        

        private static string GetFriendlyMappedTypeName(Type type)
        {
            string name = type.Name;

            string nonGenericName =
                (type.IsGenericType)
                    ? name.Remove(name.IndexOf('`'))
                    : name;

            return nonGenericName;
        }
    }
}
