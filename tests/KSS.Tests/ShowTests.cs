using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NPoco;
using Xunit;
using Xunit.Abstractions;

namespace KSS.Tests
{
    public class ShowTest : IClassFixture<TestContext>
    {
        private readonly TestContext _context;
        private readonly Settings _settings;
        private readonly IDatabase _db;
        private readonly ITestOutputHelper _output;

        public ShowTest(TestContext context, ITestOutputHelper output)
        {
            _context = context;
            _settings = _context.Container.GetInstance<Settings>();
            _db = _context.Container.GetInstance<IDatabase>();
            _output = output;
        }


        [FactAttribute]
        public void GetAllShowsFromListPage()
        {

        }

        //[FactAttribute]
        public void UpdateShowsFromTestDataPage()
        {
            var shows = _db.Query<Show>().ToList().Where(x => !_db.Query<Episode>().Any(y => y.ShowId == x.Id)).ToList();
            _output.WriteLine(shows.Count().ToString());
            _db.OpenSharedConnection();
            Parallel.ForEach(shows, (show) =>
             {
                 _output.WriteLine(show.Link.Replace("/Anime/", ""));
                 var pathToAnime = Path.Combine(_context.CurrentPath, "TestData", "Shows", show.Link.Replace("/Anime/", ""));
                 var indexFile = Path.Combine(pathToAnime, "index.html");
                 //_output.WriteLine(indexFile);
                 if (!Directory.Exists(pathToAnime) || !File.Exists(indexFile))
                 {
                     _output.WriteLine(show.Link.Replace("/Anime/", ""));
                     return;
                 }
                 try
                 {
                     var document = utils.TestUtils.GetTestDataPage(indexFile);

                     if (document.DocumentNode.InnerHtml.Contains("Please wait 5 seconds..."))
                     {
                         _output.WriteLine("Errored:" + show.Link.Replace("/Anime/", ""));
                         File.Delete(indexFile);
                         return;
                     }

                     var all = document.DocumentNode.Descendants().ToList();
                     var blocks = all.Where(x => x.Attributes.Any(y => y.Name == "class" && y.Value == "bigBarContainer"));
                     var informationBlockTable = blocks.Where(x => x.ChildNodes.Any(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "barTitle") && y.InnerText.Trim().ToUpperInvariant().Contains("INFORMATION"))).First();
                     //var otherNamesBlocks = informationBlockTable.Elements("p").Where(x => x.ChildNodes.Any(y => y.Name == "span" && y.InnerText.Trim().Contains("Other name:"))).First().Elements("a");
                     //var otherNames = string.Join(";", otherNamesBlocks.Select(x => x.InnerText.Trim()).ToArray());
                     //var views = informationBlockTable.Elements("p").Where(x => x.ChildNodes.Any(y => y.Name == "span" && y.InnerText.Trim().Contains("Views:"))).First().InnerText.Trim();
                     //var summary = informationBlockTable.Elements("p").Where(x => x.ChildNodes.Any(y => y.Name == "span" && y.InnerText.Trim().Contains("Summary:"))).First().InnerHtml.Trim();
                     //show.OtherName = otherNames;
                     //show.Views = ulong.Parse(views.Replace(",", ""));
                     //show.Summary = summary;

                     var episodesBlockTable = blocks.Where(x => x.ChildNodes.Any(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "barTitle") && y.InnerText.Trim().ToUpperInvariant().Contains("EPISODES"))).First();

                     var episodesBlockTableNodes = episodesBlockTable.Descendants().SelectMany(x => x.Elements("table").Where(y => y.Attributes.Any(z => z.Name == "class" && z.Value == "listing"))).FirstOrDefault();

                     var trTags = episodesBlockTableNodes.Elements("tr").Where(x => x.InnerText.Trim() != string.Empty && !x.Elements("th").Any());

                     var li = trTags.ToList().Select(x =>
                     {
                         var dd = getEpisodeFromRow(show.Id, x);
                         return dd;
                     }).Where(x => !_db.Query<Episode>().Any(y => y.Link == x.Link));

                     lock (_db)
                     {

                         _db.InsertBatch(li);
                         _db.Update(show);
                     }
                 }
                 catch (Exception ex)
                 {
                     _output.WriteLine(ex.ToString());
                     _output.WriteLine(show.Link.Replace("/Anime/", ""));
                 }
             });
            Assert.False(true);
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
            //_output.WriteLine(link);

            //_output.WriteLine(link.Split('/')[3].ToUpperInvariant());
            var parts = link.Split('?');
            var query = parts[1];
            var linkPart = parts[0].Split('/')[3].ToUpperInvariant();
            //_output.WriteLine(link);
            //_output.WriteLine(linkPart);

            if (linkPart.Trim() == "MOVIE" || !linkPart.Contains("-"))
            {
                epidsode.Number = 0;

            }
            else
            {
                if (int.TryParse(linkPart.Split('-')[1], out int res))
                {
                    epidsode.Number = res;
                }
            }
            epidsode.FileName = "";

            return epidsode;
        }


        [Fact(Skip = "We have already retrived all shows name")]
        public void TempCreateAllShows()
        {
            var shows = _db.Query<Show>().ToList();

            Parallel.ForEach(shows, (show) =>
             {
                 var pathToAnime = Path.Combine(_context.CurrentPath, "TestData", "Shows", show.Link.Replace("/Anime/", ""));
                 if (!Directory.Exists(pathToAnime))
                 {
                     Directory.CreateDirectory(pathToAnime);
                 }
                 var indexFile = Path.Combine(pathToAnime, "index.html");

                 if (!File.Exists(indexFile))
                 {
                     try
                     {
                         File.WriteAllText(Path.Combine(pathToAnime, "index.html"), GetShowPage(show));
                     }
                     catch (Exception)
                     {
                         _output.WriteLine(indexFile);
                         _output.WriteLine(show.Link.Replace("/Anime/", ""));
                     }
                 }
             });

            Assert.False(true);
        }

        public string GetShowPage(Show show)
        {
            var client = _context.Container.GetInstance<HttpClient>();
            var result = client.GetAsync($"{_settings.BaseUrl}{show.Link}").Result;
            return result.Content.ReadAsStringAsync().Result;
        }
    }
}