using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Entity
{
    public class AppSettings
    {
        public string MysqlCommon { get; set; }
        public long FileSizeLimit { get; set; }
        public string StoredFilesPath { get; set; }
    }
}
