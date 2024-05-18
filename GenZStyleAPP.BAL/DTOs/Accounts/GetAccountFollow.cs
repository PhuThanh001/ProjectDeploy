using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Accounts
{
    public class GetAccountFollow
    {   
        public int accountId {  get; set; }
        public string? Username { get; set; }
        public string? avatar { get; set; }

        public string? Firstname { get; set; }
        public string? Lastname { get; set; }

        public bool? isfollow { get; set; }
    }
}
