using Assimalign.Cohesion.ObjectMapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace Example.App.Database.Blob;

internal partial class TestMapperProfile : MapperProfile<Test3, Test1>
{

    protected override void Configure(MapperProfileDescriptor<Test3, Test1> descriptor)
    {
        descriptor
            .MapMember(target => target.Info.FirstName, source => source.FirstName)
            .MapMember(target => target.Info.LastName, source => source.LastName);
    }
}
