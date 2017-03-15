using System;
using System.Linq;
using Xunit;

namespace KSS.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var a = new HtmlAgilityPack.HtmlDocument();
            a.LoadHtml("/Users/elijahbate/Documents/Dev/KSS/tests/data/list-anime.html");
            var all = a.DocumentNode.Descendants().ToList();
            var telms = a.DocumentNode.Descendants().SelectMany(x => x.Elements("table"));

            var filtered = telms.Where(x => x.Attributes.AttributesWithName("class").Any(y => y.Value == "list"));
        }
    }
}
