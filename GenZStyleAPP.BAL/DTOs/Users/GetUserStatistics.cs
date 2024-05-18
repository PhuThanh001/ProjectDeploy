using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Users
{
    public class GetUserStatistics
    {
        public double UserNormal { get; set; }
        public double UserVIP { get; set; }
        public double UserPremium { get; set; }
    }
}
