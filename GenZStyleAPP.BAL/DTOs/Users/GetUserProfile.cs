using GenZStyleAPP.BAL.DTOs.Accounts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.DTOs.Users
{
    public class GetUserProfile
    {
        [Key]
        public int UserID { get; set; }
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public decimal? Height { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; }
        public bool? Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string? StyleName { get; set; }

        public GetAccountSuggest? Account { get; set; }
    }
}
