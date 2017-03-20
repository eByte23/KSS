using NPoco;
using System;
using System.Collections.Generic;
using System.Text;

namespace KSS.Temp.Db
{
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

        [ColumnAttribute("downloads_expire")]
        public DateTime DownloadsExpire { get; set; }

        [ColumnAttribute("date_added")]
        public DateTime DateAdded { get; set; }

        public class EpisodeDownloadLink
        {
            public const string RES_1080p = "1080";
            public const string RES_720p = "720";
            public const string RES_480p = "480";
            public const string RES_360p = "360";

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
                if(typeName.Contains(RES_360p) || typeName.Contains(RES_480p))
                {
                    Type = DownloadTypes.SD;
                }
                else if (typeName.Contains(RES_720p))
                {
                    Type = DownloadTypes.HD;
                }
                else if (typeName.Contains(RES_1080p))
                {
                    Type = DownloadTypes.FullHD;
                }
                else
                {
                    Type = DownloadTypes.Unknown;
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


    }
}

