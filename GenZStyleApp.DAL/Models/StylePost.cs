using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleApp.DAL.Models
{
    public class StylePost
    {
        public int PostId { get; set; }
        public int StyleId { get; set; }

        public DateTime CreateAt { get; set; }

        public Post Post { get; set; }

        public Style Style { get; set; }
    }
}
