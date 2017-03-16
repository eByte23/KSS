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
using NPoco;

namespace KSS.Tests
{
    public class TestContext
    {
        public string CurrentPath {get;private set;}
        public TestContext()
        {
            CurrentPath = Path.GetDirectoryName(typeof(TestContext).GetTypeInfo().Assembly.Location);
            var builder = new ConfigurationBuilder()
                .SetBasePath(CurrentPath)
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
                c.For<IDatabase>().Use(new Database("Server=127.0.0.1;Port=3306;Database=kss_test;Uid=root;Pwd=Kkk**881;Server=localhost;",DatabaseType.MySQL, MySql.Data.MySqlClient.MySqlClientFactory.Instance));
            });
        }

        public IContainer Container { get; private set; }

        public IConfigurationRoot Configuration { get; private set; }
    }
}
