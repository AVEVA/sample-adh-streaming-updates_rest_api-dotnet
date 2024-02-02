using Xunit;

namespace ChangeBrokerRestApiTest
{
    public class UnitTests
    {
        [Fact]
        public void ChangeBrokerRestApiUnitTest()
        {
            Assert.True(ChangeBrokerRestApi.Program.MainAsync(true).Result);
        }
    }
}
