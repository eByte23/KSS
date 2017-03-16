using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StructureMap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace KSS.Tests
{
    public class TestContext
    {
        public TestContext()
        {
            var currentPath = Path.GetDirectoryName(typeof(TestContext).GetTypeInfo().Assembly.Location);
            var builder = new ConfigurationBuilder()
                .SetBasePath(currentPath)
                .AddJsonFile("appsettings.json", optional:false)
                .AddJsonFile("appsettings.test.json", optional: true, reloadOnChange: false);

            Configuration = builder.Build();

            Container = new Container();
            var services = new ServiceCollection()
                .AddOptions()
                .Configure<Settings>(Configuration.GetSection("settings"));


            Container.Populate(services);

            Container.Configure(c =>
            {
                //c.For<ILoggerFactory>().Use(new LoggerFactory());
                c.For<Settings>().Use(ctx => ctx.GetInstance<IOptions<Settings>>().Value);
                c.For<DownloadLinkCrypto>().Use(new DownloadLinkCrypto());
                c.For<HttpClient>().Use(ctx => CloudFlareHandlerFactory.Build(ctx.GetInstance<Settings>()));
            });
        }

        public IContainer Container { get; private set; }

        public IConfigurationRoot Configuration { get; private set; }
    }
}
