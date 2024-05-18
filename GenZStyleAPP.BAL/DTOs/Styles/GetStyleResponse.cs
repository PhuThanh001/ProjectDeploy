using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Styles
{
    public class GetStyleResponse
    {
        [Key]

        public int StyleId { get; set; }

        public int AccountId { get; set; }

        public string StyleName { get; set; }

        public string Description { get; set; }

        public DateTime CreateAt { get; set; }

    }
}
