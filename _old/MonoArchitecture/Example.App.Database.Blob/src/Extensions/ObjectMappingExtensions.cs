using System;
using System.Collections.Generic;
using System.Text;
using Assimalign.Cohesion.ObjectMapping;

namespace Example.App.Database.Blob
{
    public static class ObjectMappingExtensions
    {
        public static IMapper CreateMapper()
        {
            return new MapperBuilder()
                .AddProfile<Test3, Test1>(descriptor =>
                {
                    descriptor.MapMember(target => target.Info.FirstName, source => source.FirstName);
                })
                .Build();
        }
    }


    public class Test1
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class Test3
    {
        public Test3Info Info { get; set; }
    }

    public class Test3Info
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
