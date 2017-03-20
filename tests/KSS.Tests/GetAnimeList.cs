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
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NPoco;
using Xunit.Abstractions;

namespace KSS.Tests
{
    public class GetAnimeList : IClassFixture<TestContext>
    {
        public List<Show> _shows = new List<Show>();
        private TestContext _context;
        private readonly Settings _settings;
        private readonly IDatabase _db;
        private readonly ITestOutputHelper _output;

        public GetAnimeList(TestContext context, ITestOutputHelper output)
        {
            _context = context;
            _settings = _context.Container.GetInstance<Settings>();
            _db = _context.Container.GetInstance<IDatabase>();
            _output = output;
        }

        //[Fact]//(Skip="already done")]
        public void Move()
        {
            var pp = Parallel.For(1, 143, (i) =>
            {
                var doc = TestUtils.GetDataPage(File.ReadAllText(Path.Combine(_context.CurrentPath, "TestData", "2017-03-16", "lists", $"page-{i}.html")));

                var alls = doc.DocumentNode.Descendants().ToList();

                var getTable = alls.SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();
                var trTags = getTable.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

                var li = trTags.ToList().Select(getShowFromRow);

                lock (_db)
                {
                    _db.InsertBatch(li);
                }
            });
        }

        //       [Fact(Skip = "justdo")]
        //[Fact]
        public void GetAnimeAllList()
        {
            HtmlDocument currentPage = TestUtils.GetTestDataPage(Path.Combine("TestData", "list-anime.html"));
            var all = currentPage.DocumentNode.Descendants().ToList();
            var uls = all.SelectMany(x => x.Elements("ul").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "pager"))).FirstOrDefault();

            Pages page = new Pages();
            page.Page = GetLastPage(uls);
            var a = page.Page + 1;
            if (!Directory.Exists(Path.Combine(_context.CurrentPath, "lists")))
            {
                Directory.CreateDirectory(Path.Combine(_context.CurrentPath, "lists"));
            }

            _db.OpenSharedConnection();

            var pp = Parallel.For(1, a, (i) =>
             {
                 // lock(_shows){
                 //     if(i % 20 == 0){
                 //         File.WriteAllText($"./test-{i}.json",JsonConvert.SerializeObject(_shows));
                 //         _shows.Clear();
                 //     }
                 // }
                 //var doc = TestUtils.GetDataPage(File.ReadAllText(Path.Combine(_context.CurrentPath, "TestData", "2017-03-16", "lists", $"page-{i}.html")));
                 var doc = TestUtils.GetDataPage(getContentForId(i));
                 File.ReadAllText(Path.Combine(_context.CurrentPath, "TestData", "2017-03-17", "lists", $"page-{i}.html"));

                 var alls = doc.DocumentNode.Descendants().ToList();

                 var getTable = alls.SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();
                 var trTags = getTable.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

                 var li = trTags.ToList().Select(x =>
                 {
                     var rr = getShowFromRow(x);
                     return new Store(rr.Id)
                     {
                         OurObject = rr
                     };
                 });
                 lock (_db)
                 {
                     _db.InsertBatch(li);
                 }
             });

            //if(_shows.Any()){
            //   File.WriteAllText($"./test-{a}.json",JsonConvert.SerializeObject(_shows));
            //}
        }

        //[Fact(Skip = "lets not ddos")]
        public void GetAnimeListFromPage()
        {
            List<Show> shows = new List<Show>();
            HtmlDocument currentPage = TestUtils.GetTestDataPage(Path.Combine("TestData", "list-anime.html"));
            var all = currentPage.DocumentNode.Descendants().ToList();

            var getTable = all.SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();
            var trTags = getTable.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

            trTags.ToList().ForEach(x =>
            {
                var rr = getShowFromRow(x);

                _db.Insert(new Store(rr.Id)
                {
                    OurObject = rr
                });
            });
        }

        //[FactAttribute]
        public void UpdateDownloadLinksForId()
        {
            var id = Guid.Parse("162e7fd0-0b04-11e7-a3c3-843835644532");
            var ep = _db.SingleById<Episode>(id);
            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync($"{_settings.BaseUrl}{ep.Link}").Result;




        }

        [FactAttribute(Skip = "we can skip now")]
        public void GetAllEpisodesAndExtraData()
        {
            HtmlDocument currentPage = TestUtils.GetTestDataPage(Path.Combine("TestData", "selected-anime-multi.html"));
            var all = currentPage.DocumentNode.Descendants().ToList();
            var blocks = all.Where(x => x.Attributes.Any(y => y.Name == "class" && y.Value == "bigBarContainer"));
            var informationBlockTable = blocks.Where(x => x.ChildNodes.Any(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "barTitle") && y.InnerText.Trim().ToUpperInvariant().Contains("INFORMATION"))).First();

            var episodesBlockTable = blocks.Where(x => x.ChildNodes.Any(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "barTitle") && y.InnerText.Trim().ToUpperInvariant().Contains("EPISODES"))).First();

            var episodesBlockTableNodes = episodesBlockTable.Descendants().SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();

            var trTags = episodesBlockTableNodes.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());
            var showId = Guid.Parse("d051c568-5c61-4b68-9063-f6216747e1f5");
            var li = trTags.ToList().Select(x =>
            {
                var dd = getEpisodeFromRow(showId, x);
                return dd;
            });

            _db.InsertBulk(li);
            var a = "";
            //File.WriteAllText(Path.Combine(_context.CurrentPath, "test.s"), string.Join("", trTags.Select(x => x.OuterHtml).ToArray()));
        }

        private Episode getEpisodeFromRow(Guid showId, HtmlNode node)
        {
            var epidsode = new Episode(showId);
            var tdTags = node.Elements("td").ToList();
            var nameTdTag = tdTags[0];
            var nameATag = nameTdTag.Element("a");
            var name = nameATag.InnerText.Trim();
            var link = nameATag.Attributes.Where(x => x.Name == "href").First().Value;

            // var imgTags = nameTdTag.Elements("img");
            // var recentlyUpdated = imgTags.Any(x => x.Attributes.Any(y => y.Name == "Title" && y.Value.Trim() == "Just Updated"));
            // var popular = imgTags.Any(x => x.Attributes.Any(y => y.Name == "Title" && y.Value.Trim() == "Popular anime"));
            // var statusTdTag = tdTags[1];
            // var hasEpisodeLink = statusTdTag.Elements("a").Any();
            // var status = statusTdTag.InnerText.Trim();

            //e.g. /Anime/Dragon-Ball-Z/Episode-291?id=107391
            epidsode.Link = link;
            _output.WriteLine(link);

            _output.WriteLine(link.Split('/')[3].ToUpperInvariant());

            epidsode.Number = int.Parse(link.Split('/')[3].ToUpperInvariant().Replace("EPISODE-", "").Split('?')[0]);
            epidsode.FileName = "";

            return epidsode;
        }

        [Fact(Skip = "lets not ddos")]
        public void GetLastPageFromListPage()
        {
            HtmlDocument currentPage = TestUtils.GetTestDataPage(Path.Combine("TestData", "list-anime.html"));
            var all = currentPage.DocumentNode.Descendants().ToList();
            var uls = all.SelectMany(x => x.Elements("ul").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "pager"))).FirstOrDefault();

            Pages page = new Pages();
            page.Page = GetLastPage(uls);

            Assert.True(page.Page == 142);
        }

        [Fact(Skip = "lets not ddos")]
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


        [FactAttribute(Skip = " aaa")]
        public void DDDD()
        {
            //new not working
            string ciphertext = "oW2Tu6QesavjvmoDt2dKwI5Bx2jWmyFt5GwdX8k+nHSoC4woSjh23GuTxnUigv7jU2DInSOTihimHmc76hpc9WDd6+Jaq4on1mJyNLE6tOwaoYY4x+SK3BVhZQ3xtDZ8prd5THiqluxjK+w2n6ZUyljfpupeeZ0qATmNGoch0zbL7hBpagAc/ck1aAw6eAv2mcOcU4Ss9z0f48AKjLIqSnz+zsMfoU7HKtIa3xzxngdSfdx+h6gkG9DVK+las1MusXOCxXxQEPz4kFPDtUp2kzh9iCIeyIICyD4YoMw23szG0xp0+ZZsBFqAkU5Gs4k6hlgtB5jn/2LVsXOxpsuU7p2pt0MCV+HIOsrRe8vBmHCxFimOfJIvvgFYd8wNm+ZJmRuAhMZZZD7LCm1CAfjsrUGaXK+ai2Hb9EdwFnSigdyGw9jM++7yCS+BIUvIlUD8xuNCyKcoaXoRH2FAYMl2oYtPn6X4fYzuI3M/hchRl1cs7dwUreb2ntAQuQFKl3h/UfjbJ0RHfrmKiVtfw0K1S+SiJkk59u8qBvlDDJsvJXcwfJ+lAOeOpQD729NUlAKjQcydrFho3CqGc+vzDPf0DcpGWJrL1359hi8FErxDOoPoKZLm670i4bm9dfR1KDb6fZAXSBqWnt9xpp0FckHvG8jjhzoFA57raALOi6/9wv4R+GhfddW1ahLYo84QrzFIU3V0Pr/RHsWVQHbQngMuGYVPUeaNZY/uagVahOrA9U7QBr4ugMqlYEcJFg/36+RMxfItexph115u7Iomy9nuYeH7mfedXPZIqPyOYmO5qoUVKEWrzVBLsHKzJLBPRiqyZ3EJdijdB/lv9pBfRmVBhQo80XN5a9fw3fJfxe5mH+hFxhnHwYJj41O0+JNvycPFGWpoSOerHSuJONFFWYyDqg==";
            //old working
            //string ciphertext = "V00qDmblNuTM/roJZvWVZON3ghygHAzk27mQbijAEYu15bBt4TJxdHVyHVVJuxmD0RytpIzh5AgepPVnUBHcYLy1eNKLhWH1ibe140OAUzXjJqISmFjRnwpRvaCWw1w49I3CSv4aCxW9qyhoV9Yb26YLlZEt0EanAbfjx1F5Hli6D5ik6CAYiEKsCalNATLzCU/OGTm0engA6ShD2OogZUlRD9HlOP8ciN5l0TDWNQdX5Oo1Rsjp5SyQ9/8zxjQchI1XM75QlVgtmfCLeVK0BA==";
            var a = new DownloadLinkCrypto();
            var b = a.Decrypt(ciphertext);
            _output.WriteLine(b);
            //Assert.NotEqual(ciphertext, a.Encrypt(b));
            Assert.True(false);
        }

        //[FactAttribute(Skip="for now")]
        public async Task TestDownloadAsync()
        {
            var link = "https://redirector.googlevideo.com/videoplayback?id=978760c1828a980e&itag=59&source=webdrive&requiressl=yes&ttl=transient&mm=30&mn=sn-ab5l6nzs&ms=nxu&mv=m&pl=49&ei=vGzMWOmxI4arqQWZmou4Cg&mime=video/mp4&lmt=1482478747003967&mt=1489792101&ip=2604:a880:800:a1::796:9001&ipbits=0&expire=1489806588&sparams=ip,ipbits,expire,id,itag,source,requiressl,ttl,mm,mn,ms,mv,pl,ei,mime,lmt&signature=20854702F11C8314315BB2AC65E298259E6D24CC.A07CEACE81DCF67C605D677566B5760ADA626EF9&key=ck2&app=explorer&fmt_list=37/1920x1080&kparams=MTIyLjEwNy4xNTEuMTY4&upx=TW96aWxsYS81LjAgKE1hY2ludG9zaDsgSW50ZWwgTWFjIE9TIFggMTBfMTJfMykgQXBwbGVXZWJLaXQvNTM3LjM2IChLSFRNTCwgbGlrZSBHZWNrbykgQ2hyb21lLzU2LjAuMjkyNC44NyBTYWZhcmkvNTM3LjM2&tr=1";

            HttpClient c = new HttpClient();
            var b = await c.GetStreamAsync(link);

            using (FileStream f = new FileStream("./file.mp4", FileMode.Create))
            {
                await b.CopyToAsync(f);
            }
        }

        //[Fact]
        public void getNewAndDecrypt()
        {

            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync(_settings.BaseUrl + "/Anime/Dragon-Ball-Z/Episode-100?id=107300").Result;
            var content = result.Content.ReadAsStringAsync().Result;
            //_output.WriteLine(content);
            var all = TestUtils.GetDataPage(content);
            var b = getDownloadLinks(all);

            b.ToList().ForEach(x =>
            {
                _output.WriteLine(x.TypeName);
                _output.WriteLine(x.Type.ToString());
                _output.WriteLine(x.Link);
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
            _output.WriteLine(decryptedC);
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

        private string getContentForId(int id)
        {
            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync($"{_settings.BaseUrl}/AnimeList?page={id}").Result;
            return result.Content.ReadAsStringAsync().Result;
        }

        private Show getShowFromRow(HtmlNode arg)
        {
            var show = new Show(Guid.NewGuid());
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

    [TableNameAttribute("shows"), PrimaryKeyAttribute("id", AutoIncrement = false)]
    public class Show
    {
        public Show()
        {
        }

        public Show(Guid id)
        {
            Id = id;
        }
        [ColumnAttribute("id")]
        public Guid Id { get; set; }
        [ColumnAttribute("name")]
        public string Name { get; set; }
        [ColumnAttribute("other_name")]
        public string OtherName { get; set; }
        [ColumnAttribute("summary")]
        public string Summary { get; set; }
        [ColumnAttribute("link")]
        public string Link { get; set; }
        [ColumnAttribute("status")]
        public string Status { get; set; }
        [ColumnAttribute("genres"), SerializedColumnAttribute]
        public List<string> Genres { get; set; }
        [ColumnAttribute("popular")]
        public bool Popular { get; set; }
        [ColumnAttribute("just_updated")]
        public bool JustUpdate { get; set; }
        [ColumnAttribute("cover_link")]
        public string CoverLink { get; set; }

        [ColumnAttribute("related_links")]
        public List<string> RelatedLinks { get; set; }
        [ColumnAttribute("views")]
        public ulong Views { get; set; }
    }

    [TableNameAttribute("episodes"), PrimaryKey("id", AutoIncrement = false)]
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

        [ColumnAttribute("id")]
        public Guid Id { get; set; }

        [ColumnAttribute("show_id")]
        public Guid ShowId { get; set; }

        [ColumnAttribute("link")]
        public string Link { get; set; }

        [ColumnAttribute("number")]
        public int Number { get; set; }

        [ColumnAttribute("related_links")]
        //public List<string> RelatedLinks { get; set; }
        public string RelatedLinks { get; set; }

        [ColumnAttribute("file_name")]
        public string FileName { get; set; }

        [ColumnAttribute("download_links"), SerializedColumnAttribute]
        public List<EpisodeDownloadLink> DownloadLinks { get; set; }

        [ColumnAttribute("date_added")]
        public DateTime DateAdded { get; set; }

        public class EpisodeDownloadLink
        {
            public EpisodeDownloadLink() { }
            public EpisodeDownloadLink(string typeName, string link)
            {
                TypeName = typeName;
                SetDownloadType(TypeName);
                Link = link;
            }

            public DownloadTypes Type { get; set; }
            public string TypeName { get; set; }
            public string Link { get; set; }

            private void SetDownloadType(string typeName)
            {
                switch (typeName)
                {
                    case RES_360p:
                    case RES_480p:
                        Type = DownloadTypes.SD;
                        break;

                    case RES_720p:
                        Type = DownloadTypes.SD;
                        break;

                    case RES_1080p:
                        Type = DownloadTypes.SD;
                        break;

                    default:
                        Type = DownloadTypes.Unknown;
                        break;
                }
            }
        }

        public enum DownloadTypes
        {
            FullHD = 0,
            HD = 1,
            SD = 2,
            Unknown = 4
        }
        public const string RES_1080p = "1080";
        public const string RES_720p = "720";
        public const string RES_480p = "480";
        public const string RES_360p = "360";

    }

    [TableName("store"), PrimaryKey("id", AutoIncrement = false)]
    public class Store
    {
        public Store()
        {

        }
        public Store(Guid id)
        {
            this.Id = id;
        }

        [Column("id")]
        public Guid Id { get; private set; }

        [Column("our_object"), SerializedColumnAttribute]
        public object OurObject { get; set; }
    }
}