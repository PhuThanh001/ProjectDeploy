using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Styles
{
    public class GetStyleRequest
    {
        public string? Description { get; set; }

        public string? SyleName { get; set; }

        public IFormFile? Video { get; set; }

        //public DateTime CreateAt { get; set; }
    }
}
