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

        [FactAttribute(Skip="we can skip now")]
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

            _db.InsertBatch(li);
            var a = "";
            //File.WriteAllText(Path.Combine(_context.CurrentPath, "test.s"), string.Join("", trTags.Select(x => x.OuterHtml).ToArray()));
        }

        private Episode getEpisodeFromRow(Guid showId, HtmlNode node)
        {
            var epidsode = new Episode(showId, Guid.NewGuid());
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

            epidsode.Number = int.Parse(link.Split('/')[3].ToUpperInvariant().Replace("EPISODE-","").Split('?')[0]);
            epidsode.FileName="";

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

        [FactAttribute]
        public void DDDD()
        {
            //new not working
            string ciphertext = "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";
            //old working
            //string ciphertext = "V00qDmblNuTM/roJZvWVZON3ghygHAzk27mQbijAEYu15bBt4TJxdHVyHVVJuxmD0RytpIzh5AgepPVnUBHcYLy1eNKLhWH1ibe140OAUzXjJqISmFjRnwpRvaCWw1w49I3CSv4aCxW9qyhoV9Yb26YLlZEt0EanAbfjx1F5Hli6D5ik6CAYiEKsCalNATLzCU/OGTm0engA6ShD2OogZUlRD9HlOP8ciN5l0TDWNQdX5Oo1Rsjp5SyQ9/8zxjQchI1XM75QlVgtmfCLeVK0BA==";
            var a = new DownloadLinkCrypto();
            var b = a.Decrypt(ciphertext,(x) => { _output.WriteLine(x); return null;});
            _output.WriteLine(b);
            //Assert.NotEqual(ciphertext, a.Encrypt(b));
            Assert.True(false);
        }

        [FactAttribute(Skip="for now")]
        public async Task TestDownloadAsync()
        {
            var link = "https://redirector.googlevideo.com/videoplayback?id=978760c1828a980e&itag=59&source=webdrive&requiressl=yes&ttl=transient&mm=30&mn=sn-ab5l6nzs&ms=nxu&mv=m&pl=49&ei=vGzMWOmxI4arqQWZmou4Cg&mime=video/mp4&lmt=1482478747003967&mt=1489792101&ip=2604:a880:800:a1::796:9001&ipbits=0&expire=1489806588&sparams=ip,ipbits,expire,id,itag,source,requiressl,ttl,mm,mn,ms,mv,pl,ei,mime,lmt&signature=20854702F11C8314315BB2AC65E298259E6D24CC.A07CEACE81DCF67C605D677566B5760ADA626EF9&key=ck2&app=explorer&fmt_list=37/1920x1080&kparams=MTIyLjEwNy4xNTEuMTY4&upx=TW96aWxsYS81LjAgKE1hY2ludG9zaDsgSW50ZWwgTWFjIE9TIFggMTBfMTJfMykgQXBwbGVXZWJLaXQvNTM3LjM2IChLSFRNTCwgbGlrZSBHZWNrbykgQ2hyb21lLzU2LjAuMjkyNC44NyBTYWZhcmkvNTM3LjM2&tr=1";

            HttpClient c = new HttpClient();
            var b = await c.GetStreamAsync(link);

            using(FileStream f = new FileStream("./file.mp4", FileMode.Create))
            {
                await b.CopyToAsync(f);
            }
        }

        [Fact(Skip="a")]
        public void getNewAndDecrypt()
        {

            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync(_settings.BaseUrl + "/Anime/Dragon-Ball-Z/Episode-100?id=107200").Result;
            var content = result.Content.ReadAsStringAsync().Result;
             _output.WriteLine(content);
            var all = TestUtils.GetDataPage(content);
            var b = getDownloadLinks(all);

            // b.ToList().ForEach(x=>{
            //     _output.WriteLine(x.OuterHtml);
            // });

            // Assert.NotNull(b);
            // Assert.True(b != string.Empty);
            // Assert.True(b.Contains("<a"));
        }

        private IEnumerable<HtmlNode> getDownloadLinks(HtmlDocument all)
        {
            var d = all.DocumentNode.Descendants().ToList().Where(x => x.Id == "divDownload").FirstOrDefault();

            var a = d.Element("script").InnerHtml.Trim().Replace("document.write(ovelWrap('", "").Replace("'));", "");
            _output.WriteLine(a);
            var crypto = new DownloadLinkCrypto();
            var b = crypto.Decrypt(a,(x) => { _output.WriteLine(x); return null;});

            var doc = new HtmlDocument();
            doc.LoadHtml(b);

            //_output.WriteLine(doc.DocumentNode.OuterHtml);
            return doc.DocumentNode.Descendants().SelectMany(x=>x.Elements("a"));
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

    [TableNameAttribute("episodes")]
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
        public string Link {get;set;}

        [ColumnAttribute("number")]
        public int Number { get; set; }

        [ColumnAttribute("related_links")]
        public List<string> RelatedLinks { get; set; }

        [ColumnAttribute("file_name")]
        public string FileName { get; set; }

        [ColumnAttribute("download_links"), SerializedColumnAttribute]
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