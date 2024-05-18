using GenZStyleAPP.BAL.DTOs.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.UserRelations
{
    public class GetFollowRequest
    {
        public ICollection<GetAccountFollow> Followers { get; set; }
        public ICollection<GetAccountFollow> Following { get; set; }
    }
}
