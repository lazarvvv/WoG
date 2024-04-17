using AutoMapper;
using WoG.Characters.Services.Api;

namespace WoG.Characters.Tests
{
    [TestClass]
    public class AutoMapperTests
    {
        [TestMethod]
        public void AutoMapperConfigIsValid()
        {
            var mapper = MappingConfig.RegisterMaps().CreateMapper();
            mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}