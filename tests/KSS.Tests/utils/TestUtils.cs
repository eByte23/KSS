using HtmlAgilityPack;
using System.IO;

namespace KSS.Tests.utils
{
    public static class TestUtils
    {
        public static HtmlDocument GetTestDataPage(string htmlDocPath)
        {
            var doc = new HtmlDocument();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), htmlDocPath);
            var testAnimeListContent = File.ReadAllText(filePath);
            doc.LoadHtml(testAnimeListContent);

            return doc;
        }

        public static HtmlDocument GetDataPage(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            return doc;
        }
    }
}