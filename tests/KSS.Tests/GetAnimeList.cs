using CloudFlareUtilities;
using HtmlAgilityPack;
using KSS.Tests.utils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace KSS.Tests
{
    public class GetAnimeList : IClassFixture<TestContext>
    {
        public List<ShowData> _shows = new List<ShowData>();
        private TestContext _context;
        private readonly Settings _settings;

        public GetAnimeList(TestContext context)
        {
            _context = context;
            _settings = _context.Container.GetInstance<Settings>();
            
        }

        [Fact]
        public void GetAnimeAllList()
        {
            HtmlDocument currentPage = TestUtils.GetTestDataPage(@"TestData\list-anime.html");
            var all = currentPage.DocumentNode.Descendants().ToList();
            var uls = all.SelectMany(x => x.Elements("ul").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "pager"))).FirstOrDefault();

            Pages page = new Pages();
            page.Page = GetLastPage(uls);
            var a = page.Page / 2;

            for (int i = 1; i < a; i++)
            {
                var doc = TestUtils.GetDataPage(getContentForId(i));
                var alls = currentPage.DocumentNode.Descendants().ToList();

                var getTable = alls.SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();
                var trTags = getTable.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

                _shows.AddRange(trTags.Select(getShowFromRow));
            }

            var al = "";
        }

        [Fact]
        public void GetAnimeListFromPage()
        {
            List<ShowData> shows = new List<ShowData>();
            HtmlDocument currentPage = TestUtils.GetTestDataPage(@"TestData\list-anime.html");
            var all = currentPage.DocumentNode.Descendants().ToList();

            var getTable = all.SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();
            var trTags = getTable.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

            shows.AddRange(trTags.Select(getShowFromRow));

            Assert.True(shows.Count == 50);
        }

        [Fact]
        public void GetLastPageFromListPage()
        {
            HtmlDocument currentPage = TestUtils.GetTestDataPage(@"TestData\list-anime.html");
            var all = currentPage.DocumentNode.Descendants().ToList();
            var uls = all.SelectMany(x => x.Elements("ul").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "pager"))).FirstOrDefault();

            Pages page = new Pages();
            page.Page = GetLastPage(uls);

            Assert.True(page.Page == 142);
        }

        [Fact]
        public void TestLogin()
        {
            var content = string.Empty;
            var cookies = new CookieCollection();

            try
            {

                var client = _context.Container.GetInstance<HttpClient>();
                var result3 = client.GetAsync(_settings.BaseUrl).Result;
                content = result3.Content.ReadAsStringAsync().Result;

                //result.Content
            }
            catch (AggregateException ex) //when (ex.InnerException is CloudFlareClearanceException)
            {
                // After all retries, clearance still failed.
            }
            catch (Exception ex) // (ex.InnerException is TaskCanceledException)
            {
                // Looks like we ran into a timeout. Too many clearance attempts?
                // Maybe you should increase client.Timeout as each attempt will take about five seconds.
            }

            Assert.True(content.Contains(_settings.Username));
        }

        [Fact]
        public void getWeDecrypt()
        {
            string ciphertext = "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";
            string decryptedtext;

            var a = new DownloadLinkCrypto();
            var b = a.Decrypt(ciphertext);

            Assert.Equal(ciphertext, a.Encrypt(b));
        }

        [Fact]
        public void getNewAndDecrypt()
        {
            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync(_settings.BaseUrl + "/Anime/Dragon-Ball-Z/Episode-100?id=107200").Result;
            var content = result.Content.ReadAsStringAsync().Result;
            var all = TestUtils.GetDataPage(content);
            string b = getDownloadLinks(all);

            Assert.NotNull(b);
            Assert.True(b != string.Empty);
            Assert.True(b.Contains("<a"));
        }

        private static string getDownloadLinks(HtmlDocument all)
        {
            var d = all.DocumentNode.Descendants().ToList().Where(x => x.Id == "divDownload").FirstOrDefault();

            var a = d.Element("script").InnerHtml.Trim().Replace("document.write(ovelWrap('", "").Replace("'));", "");

            var crypto = new DownloadLinkCrypto();
            var b = crypto.Decrypt(a);
            return b;
        }

        private string getContentForId(int id)
        {
            var client = new HttpClient(_context.Container.GetInstance<ClearanceHandler>());
            var result = client.GetAsync($"{_settings.BaseUrl}/AnimeList?page={id}").Result;
            return result.Content.ReadAsStringAsync().Result;
        }

        private ShowData getShowFromRow(HtmlNode arg)
        {
            var show = new ShowData(Guid.NewGuid());
            var tdTags = arg.Elements("td").ToList();
            var nameTdTag = tdTags[0];
            var nameATag = nameTdTag.Element("a");
            var name = nameATag.InnerText.Trim();
            var imgTags = nameTdTag.Elements("img");
            var recentlyUpdated = imgTags.Any(x => x.Attributes.Any(y => y.Name == "Title" && y.Value.Trim() == "Just Updated"));
            var popular = imgTags.Any(x => x.Attributes.Any(y => y.Name == "Title" && y.Value.Trim() == "Popular anime"));
            var statusTdTag = tdTags[1];
            var hasEpisodeLink = statusTdTag.Elements("a").Any();
            var status = statusTdTag.InnerText.Trim();

            show.Popular = popular;
            show.JustUpdate = recentlyUpdated;
            show.Name = name;
            show.Link = nameATag.Attributes[0].Value;
            show.Status = status;

            return show;
        }

        private int GetLastPage(HtmlNode uls)
        {
            foreach (HtmlNode aTags in uls.Descendants().Where(x => x.Name == "a"))
            {
                var pageNumber = aTags.Attributes.Where(x => x.Name == "page");
                var isLastTag = aTags.InnerText.ToUpperInvariant().Contains("LAST");

                if (pageNumber.Any() && isLastTag)
                {
                    return int.Parse(pageNumber.First().Value);
                }
            };

            return 0;
        }
    }

    public class Pages
    {
        public int Page { get; set; }
    }

    public class ShowData
    {
        public ShowData()
        {
        }

        public ShowData(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public string Name { get; set; }
        public string OtherName { get; set; }
        public string Summary { get; set; }
        public string Link { get; set; }
        public string Status { get; set; }
        public List<string> Genres { get; set; }
        public bool Popular { get; set; }
        public bool JustUpdate { get; set; }
        public string CoverLink { get; set; }

        public List<string> RelatedLinks { get; set; }

        public ulong Views { get; set; }
    }

    public class Episode
    {
        public Episode()
        {
        }

        public Episode(Guid showId) : this(showId, null)
        {
        }

        public Episode(Guid showId, Guid? episodeId)
        {
            ShowId = showId;
            Id = episodeId.HasValue ? episodeId.Value : Guid.NewGuid();
        }

        public Guid Id { get; private set; }
        public Guid ShowId { get; private set; }
        public int Number { get; set; }
        public string Link { get; set; }
        public string FileName { get; set; }
        public List<EpisodeDownloadLink> DownloadLinks { get; set; }

        public class EpisodeDownloadLink
        {
            public DownloadTypes Type { get; set; }
            public string Link { get; set; }
        }

        public enum DownloadTypes
        {
            FullHD = 0,
            HD = 1,
            SD = 2
        }
    }
}