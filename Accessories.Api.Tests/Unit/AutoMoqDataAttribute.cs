using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Accessories.Api.Controllers;

namespace Accessories.Api.Tests.Unit;

public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() =>
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            // Tell AutoFixture to use the constructor but ignore the base properties
            fixture.Customize<ProductsController>(c => c.OmitAutoProperties());            
            return fixture;
        })
    {
        
    }
}