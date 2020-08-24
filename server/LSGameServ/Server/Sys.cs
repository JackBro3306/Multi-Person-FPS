using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.Server {
    class Sys {
        public static long GetTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970,1,1,0,0,0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
    }
}
