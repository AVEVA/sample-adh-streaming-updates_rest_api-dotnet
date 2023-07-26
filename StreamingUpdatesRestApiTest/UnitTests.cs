using StreamingUpdatesRestApi;
using Xunit;

namespace StreamingUpdatesRestApiTest
{
    public class UnitTests
    {
        [Fact]
        public void AssetsRestApiUnitTest()
        {
            Assert.True(Program.MainAsync(true).Result);
        }
    }
}
