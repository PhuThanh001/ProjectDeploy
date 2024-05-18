using GenZStyleAPP.BAL.DTOs.Styles;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.Repository.Interfaces
{
    public interface IStyleRepository
    {
        public Task CreateStyleAsync(int accountId,GetStyleRequest styleRequest);
    }
}
