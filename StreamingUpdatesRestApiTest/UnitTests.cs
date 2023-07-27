using Xunit;

namespace StreamingUpdatesRestApiTest
{
    public class UnitTests
    {
        [Fact]
        public void StreamingUpdatesRestApiUnitTest()
        {
            Assert.True(StreamingUpdatesRestApi.Program.MainAsync(true).Result);
        }
    }
}
