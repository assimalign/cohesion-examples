using Assimalign.Cohesion;
using Assimalign.Cohesion.FileSystem;
using Assimalign.Cohesion.ObjectMapping;
using System.Collections.Generic;







var mapper = new MapperBuilder()
    .AddProfile<Test3Info, Test1>(descriptor =>
    {
        descriptor.MapMember(target => target.FirstName, source => source.FirstName);
    });



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