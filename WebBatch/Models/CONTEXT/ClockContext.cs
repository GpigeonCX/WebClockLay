using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace WebBatch.Models
{
    public class ClockContext : DbContext
    {
        public ClockContext() : base("name=MyStrMssqlConn")
        {

        }
        public virtual DbSet<ClockBatch> ClockBatch { get; set; }
    }
}