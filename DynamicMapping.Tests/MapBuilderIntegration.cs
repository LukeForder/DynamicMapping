using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DynamicMapping.Tests
{

    public class SomeDomainClass
    {
        public virtual int SomeDomainClassId { get; set; }
        public virtual string Name { get; set; }
        public virtual decimal Value { get; set; }
    }


    public class FooMap : ClassMap<SomeDomainClass>
    {
        public FooMap()
        {
            Id(x => x.SomeDomainClassId);
        }
    }

    //public class SomeDomainClassMap : ClassMap<SomeDomainClass>
    //{
    //    public SomeDomainClassMap()
    //    {
    //        
      //  ParameterExpression parameterExpression = Expression.Parameter(typeof(SomeDomainClass), "x");
      //  PropertyInfo property = typeof(SomeDomainClass).GetProperty("SomeDomainClassId");
      //  Expression body = Expression.Convert(Expression.MakeMemberAccess(parameterExpression, property), typeof(object));
      //  Expression<Func<SomeDomainClass, object>> memberExpression = Expression.Lambda<Func<SomeDomainClass, object>>(body, new ParameterExpression[]
      //  {
      //          parameterExpression
      //  });
		    //base.Id(memberExpression);
    //    }
    //}


    public class MapBuilderIntegration
    {
        [Fact]
        public void it_does_build_assembly()
        {
            MapBuilder mapBuilder = new MapBuilder();

            mapBuilder.Generate(typeof(SomeDomainClass));

        }

        [Fact]
        public void can_create_instance_of_generated_map()
        {
            //SomeDomainClassMap map = new SomeDomainClassMap();

        }
    }
}
