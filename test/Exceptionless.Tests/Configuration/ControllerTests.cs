using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Exceptionless;
using Exceptionless.Logging;
using Exceptionless.Submission;
using Exceptionless.Tests.Utility;

namespace Foundatio.Tests.Hosting {
    public class HostingTests {
        public HostingTests(ITestOutputHelper output) { }

        [Fact]
        public async Task CanSubmitInTest() {
            InMemoryExceptionlessLog logger;
            var builder = new WebHostBuilder()
                .ConfigureServices(s => {
                    s.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
                    s.AddHttpContextAccessor();
                })
                .Configure(app => {
                    ExceptionlessClient.Default.Configuration.Resolver.Register(typeof(ISubmissionClient), typeof(InMemorySubmissionClient));
                    logger = ExceptionlessClient.Default.Configuration.UseInMemoryLogger(LogLevel.Trace);
                    app.UseExceptionless("123");
                    app.UseMvc();
                });

            var server = new TestServer(builder);

            var client = server.CreateClient();
            var response = await client.GetAsync("/api/my");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class MyController : ControllerBase {
        [HttpGet]
        public ActionResult Get() {
            ExceptionlessClient.Default.CreateLog("Navigation", "Home Page", LogLevel.Info).AddObject(User?.Identity, "User").Submit();
            ExceptionlessClient.Default.ProcessQueue();
            return Ok();
        }
    }
}