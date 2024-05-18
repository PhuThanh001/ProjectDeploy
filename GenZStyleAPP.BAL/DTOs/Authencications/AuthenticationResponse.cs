using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Authencications
{
    public class AuthenticationResponse
    {
        [Key]
        public int? AccountId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? AccessToken { get; set; }

        public string? Role { get; set; }

        public string? RefreshToken { get; set; }
    }
}
