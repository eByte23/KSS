using NPoco;
using System;
using System.Collections.Generic;

namespace KSS.Temp.Db
{
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
}
