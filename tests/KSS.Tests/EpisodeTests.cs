using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using KSS.Tests.utils;
using NPoco;
using Xunit;
using Xunit.Abstractions;

namespace KSS.Tests
{
    public class EpisodeTest : IClassFixture<TestContext>
    {
        private readonly TestContext _context;
        private readonly Settings _settings;
        private readonly IDatabase _db;
        private readonly ITestOutputHelper _output;

        public EpisodeTest(TestContext context, ITestOutputHelper output)
        {
            _context = context;
            _settings = _context.Container.GetInstance<Settings>();
            _db = _context.Container.GetInstance<IDatabase>();
            _output = output;
        }
        /*
            1. Got to selected Show Page and get all episodes and Datas
            2. Got To episode link and get download links
         */


        /*
           From TestData/Shows/<Show>/<Episode>.html parse and decrypt download links
           In this case TestData/Shows/Dragon
          */
        [FactAttribute]
        public void GetDownloadLinksFromPage()
        {
            _db.OpenSharedConnection();
            var shows = _db.Query<Show>().Where(x => x.Name.StartsWith("Dragon Ball Z")).ToList();
            var eps = _db.Query<Episode>().ToList().Where(x => shows.Any(y => y.Id == x.ShowId)).ToList();

            Parallel.ForEach(eps, (ep) =>
            {
                try
                {
                    using (var client = _context.Container.GetInstance<HttpClient>())
                    {
                        var result = client.GetAsync(_settings.BaseUrl + ep.Link).Result;
                        var content = result.Content.ReadAsStringAsync().Result;
                        //_output.WriteLine(content);
                        var all = TestUtils.GetDataPage(content);
                        var b = getDownloadLinks(all);

                        ep.DownloadLinks = b;

                        lock (_db)
                        {
                            _db.Update(ep);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine(ex.ToString());
                    _output.WriteLine(ep.Link);
                }
            });


            // Assert.NotNull(b);
            Assert.True(false);
            // Assert.True(b.Contains("<a"));
        }

        private List<Episode.EpisodeDownloadLink> getDownloadLinks(HtmlDocument all)
        {
            var links = new List<Episode.EpisodeDownloadLink>();
            var crypto = _context.Container.GetInstance<DownloadLinkCrypto>();
            var d = all.DocumentNode.Descendants().ToList().Where(x => x.Id == "divDownload").FirstOrDefault();
            //_output.WriteLine(all.DocumentNode.OuterHtml);
            var a = d.Element("script");
            var linksa = a.InnerHtml.Trim().Replace("document.write(ovelWrap('", "").Replace("'));", "");
            var decryptedC = crypto.Decrypt(linksa);
            //_output.WriteLine(decryptedC);
            var doc = TestUtils.GetDataPage(decryptedC);
            doc.DocumentNode.Elements("a")
                .ToList().ForEach(x =>
                {
                    //_output.WriteLine(x.OuterHtml);
                    var encLink = x.GetAttributeValue("href", string.Empty);
                    //var b = crypto.Decrypt(encLink);
                    var dlLink = new Episode.EpisodeDownloadLink(x.InnerText.Trim(), encLink);
                    links.Add(dlLink);
                });


            return links;
        }
    }
}