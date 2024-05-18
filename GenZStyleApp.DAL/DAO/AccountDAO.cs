using GenZStyleApp.DAL.Enums;
using GenZStyleApp.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GenZStyleApp.DAL.DAO
{
    public class AccountDAO
    {
        private GenZStyleDbContext _dbContext;
        public AccountDAO(GenZStyleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<Account> GetAccountAsync(int emailAddress)
        {
            try
            {
                return await this._dbContext.Accounts.FirstOrDefaultAsync(x => x.Email.Equals(emailAddress));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Account?> AddAccountAsync(Account newAccount)
        {
            try
            {
                 var rs = await this._dbContext.Accounts.AddAsync(newAccount);
                 return rs.Entity;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Account> GetAccountById(int accountId)
        {
            try
            {
                return await _dbContext.Accounts.Include(a => a.User).ThenInclude(u => u.Role)
                                           .Include(a => a.Posts).ThenInclude(a => a.HashPosts)
                                           .Include(a => a.Posts).ThenInclude(p => p.Likes)
                                           .Include(a => a.Tokens)
                                           .SingleOrDefaultAsync(a => a.AccountId == accountId);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        #region DeleteAccount
        public void DeleteAccount(Account account)
        {
            try
            {
                this._dbContext.Accounts.Remove(account);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        public async Task<List<Account>> GetSuggestionAccount(List<UserRelation> accounts,int accountId)
        {
            try
            {
                List<int> accountIds = accounts.Select(a => a.FollowingId).ToList();

                var allAccounts = await _dbContext.Accounts
                .Include(a => a.User)
                .ThenInclude(u => u.Role)
                .ToListAsync();
                // Lọc ra các tài khoản không nằm trong danh sách accountIds và có AccountId != 55
                var suggestionAccounts = allAccounts.Where(a => !accountIds.Contains(a.AccountId) && a.AccountId != 9 && a.AccountId != accountId && a.IsActive == true).ToList();

                return suggestionAccounts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task<List<Account>>? GetListRestAccount(List<Account> accounts, int accountId)
        {
            try
            {
                // Lấy danh sách Id của các tài khoản được truyền vào
                List<int> excludedAccountIds = accounts.Select(a => a.AccountId).ToList();

                // Lấy tất cả các tài khoản ngoại trừ những tài khoản đã được truyền vào
                var restAccounts = await _dbContext.Accounts
                    .Where(a => !excludedAccountIds.Contains(a.AccountId) && a.AccountId != 9 && a.AccountId != accountId && a.IsActive == true)
                    .ToListAsync();

                return restAccounts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task<Account> GetAccountByEmail(string email)
        {

            try
            {
                return await this._dbContext.Accounts                                    
                                    .Include(a => a.User).ThenInclude(u => u.Role)
                                    .Include(u => u.Posts)
                                    .Include(a => a.Style)
                                    .SingleOrDefaultAsync(a => a.Email.Equals(email) && a.IsActive == true );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<Account> GetAccountByEmailAndPasswordAsync(string email, string password)
        {
            try
            {
                return await this._dbContext.Accounts.Include(a => a.User).ThenInclude(a => a.Role)
                                                    .FirstOrDefaultAsync(x => x.Username.Equals(email.Trim().ToLower())
                                                                                   && x.PasswordHash.Equals(password)
                                                                                   && x.IsActive == true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Account>> GetUserNormal()
        {
            try
            {
                return await _dbContext.Accounts.Include(u => u.User)
                                             .Where(u => u.IsVip == 0) // Lọc theo RoleId
                                             .ToListAsync(); // Sử dụng ToListAsync thay vì SingleOrDefaultAsync
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Account>> GetUserVIP()
        {
            try
            {
                return await _dbContext.Accounts.Include(u => u.User)
                                             .Where(u => u.IsVip == 1) // Lọc theo RoleId
                                             .ToListAsync(); // Sử dụng ToListAsync thay vì SingleOrDefaultAsync
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Account>> GetUserPremium()
        {
            try
            {
                return await _dbContext.Accounts.Include(u => u.User)
                                             .Where(u => u.IsVip == 2) // Lọc theo RoleId
                                             .ToListAsync(); // Sử dụng ToListAsync thay vì SingleOrDefaultAsync
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<Account> GetAccountByUserAsync(string email)
        {
            try
            {
                return await this._dbContext.Accounts.Include(a => a.User).ThenInclude(a => a.Role)
                                                    .FirstOrDefaultAsync(x => x.Username.Equals(email.Trim().ToLower())                                                                                   
                                                                                   && x.IsActive == true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task UpdateAccountProfile(Account account)
        {
            try
            {
                this._dbContext.Entry<Account>(account).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Account>> GetSuggestedUsersByStyleNameAsync(string currentUserStyleName, int authenticatedUserId)
        {
            try
            {
                // Lấy danh sách tất cả người dùng
                var allUsers = await _dbContext.Accounts
                    .Include(a => a.Style)
                    .Include(y => y.Posts)
                    .Include(a => a.User)
                    .ThenInclude(u => u.Role)
                    .Where(u => u.AccountId != authenticatedUserId && u.IsActive == true && u.AccountId != 9) // Loại bỏ người dùng xác thực
                    .ToListAsync();

                // Lọc danh sách người dùng đề xuất chỉ giữ lại những người dùng có cùng StyleName với người dùng hiện tại
                var suggestedUsersWithSameStyle = allUsers
                    .Where(u => u.Style != null && u.Style.StyleName == currentUserStyleName)
                    .ToList();

                return suggestedUsersWithSameStyle;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task<List<Account>> GetFollowedUsersWithSameStyleAsync(string currentUserStyleName, int authenticatedUserId)
        {
            try
            {
                // Lấy danh sách tất cả người dùng có cùng StyleName với người dùng hiện tại
                var usersWithSameStyle = await _dbContext.Accounts
                    .Include(a => a.Style)
                    .Include(y => y.Posts)
                    .Include(a => a.User)
                    .ThenInclude(u => u.Role)
                    .Where(u => u.AccountId != authenticatedUserId && u.Style != null && u.Style.StyleName == currentUserStyleName && u.IsActive && u.AccountId != 9)
                    .ToListAsync();

                // Lấy danh sách các người dùng mà người dùng xác thực đang theo dõi từ trong danh sách trên
                var followedUsersWithSameStyle = usersWithSameStyle
                    .Where(u => !_dbContext.UserRelations.Any(ur => ur.FollowerId == authenticatedUserId && ur.FollowingId == u.AccountId))
                    .ToList();

                return followedUsersWithSameStyle;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public void ChangePassword(Account account)
        {
            try
            {
                this._dbContext.Entry<Account>(account).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Search By UserName
        public async Task<List<Account>> SearchByUsername(string username)
        {
            try
            {
                List<Account> accounts = await _dbContext.Accounts
                    
                    .Include(u => u.Posts)
                    .Where(a => a.Username.Contains(username))
                    .ToListAsync();

                return accounts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Account>> GetAllAccount()
        {
            try
            {
                List<Account> accounts = await _dbContext.Accounts
                    
                    .Include (u => u.Posts)
                    .Include(u => u.UserRelations)
                    .ToListAsync();
                return accounts;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<Account> GetAccountByReporterId(int reporterId)
        {
            try
            {
                return await _dbContext.Accounts
                    .Include(a => a.Posts).ThenInclude(u => u.Reports)
                                           .SingleOrDefaultAsync(a => a.AccountId == reporterId);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }



    }
}
