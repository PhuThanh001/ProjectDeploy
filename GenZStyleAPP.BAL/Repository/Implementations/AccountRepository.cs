using AutoMapper;
using BMOS.BAL.Helpers;
using Firebase.Auth;
using GenZStyleApp.DAL.DAO;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.Accounts;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleAPP.BAL.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using ProjectParticipantManagement.DAL.Infrastructures;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.Repository.Implementations
{
    public class AccountRepository : IAccountRepository
    {
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;
        private IEmailRepository _emailRepository;

        public AccountRepository(IUnitOfWork unitOfWork, IMapper mapper, IEmailRepository emailRepository)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
            _emailRepository = emailRepository;
        }

        public async Task<List<GetAccountResponse>> GetAccountssAsync()
        {
            try
            {
                List<Account> accounts = await this._unitOfWork.AccountDAO.GetAllAccount();
                return this._mapper.Map<List<GetAccountResponse>>(accounts);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        public async Task ChangPassword(int accountId, ChangePasswordRequest changPasswordRequest)
        {    //phu1234
            //nam1234
            //Admin@123
            try
            {   
                var account = await _unitOfWork.AccountDAO.GetAccountById(accountId);
                if (account == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }
                // Kiểm tra xem mật khẩu cũ có khớp với mật khẩu hiện tại không
                /*bool isPasswordValid = BCrypt.Net.BCrypt.Verify(changPasswordRequest.OldPassword, account.PasswordHash);
                if (!isPasswordValid)
                {
                    throw new BadRequestException("Old password does not match with current password.");
                }*/

                if (changPasswordRequest.NewPassword != changPasswordRequest.ConfirmPassword)
                {
                    throw new BadRequestException("New password and old password do not match each other.");
                }

                // Mã hóa mật khẩu mới
                /*string hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(changPasswordRequest.NewPassword);*/
                // Cập nhật mật khẩu đã được mã hóa vào tài khoản
                account.PasswordHash = changPasswordRequest.NewPassword;
                _unitOfWork.AccountDAO.ChangePassword(account);
                _unitOfWork.Commit();
                /*return _mapper.Map<GetAccountResponse>(account);*/
            }
            catch (BadRequestException ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new BadRequestException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        public async Task<List<GetAccountSuggest>> GetSuggestionUsersStyleNameAsync(HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

                // Lấy stylename của người dùng hiện tại
                var currentUserStyleName = account.Style.StyleName;

                var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người                       
                //var listaccount = await _unitOfWork.AccountDAO.GetSuggestionAccount(followings, account.AccountId);

                // Lấy danh sách người dùng đề xuất có cùng StyleName với người dùng hiện tại
                var suggestedUsersWithSameStyle = await _unitOfWork.AccountDAO.GetSuggestedUsersByStyleNameAsync(currentUserStyleName, account.AccountId);

                var listAccounts = _mapper.Map<List<GetAccountSuggest>>(suggestedUsersWithSameStyle);

                List<GetAccountSuggest> result1 = new List<GetAccountSuggest>();
                List<GetAccountSuggest> result2 = new List<GetAccountSuggest>();
                List<GetAccountSuggest> totalResult = new List<GetAccountSuggest>();

                // Lặp qua danh sách người dùng đề xuất
                foreach (var accountDTO in listAccounts)
                {
                    var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTO.AccountId);// người này có bao nhiêu follower
                    var account1 = await _unitOfWork.AccountDAO.GetAccountById(accountDTO.AccountId);

                    GetAccountSuggest getAccountResponses = new GetAccountSuggest()
                    {
                        AccountId = accountDTO.AccountId,
                        UserId = accountDTO.UserId,
                        Email = accountDTO.Email, // Đảm bảo là null, không phải chuỗi rỗng
                        Firstname = accountDTO.Firstname,
                        Lastname = accountDTO.Lastname,
                        PasswordHash = accountDTO.PasswordHash,
                        AvatarUrl = account1.User.AvatarUrl,
                        isfollow = false,
                        //Gender = accountDTO.Gender,
                        RoleName = account1.User.Role.RoleName,
                        Username = accountDTO.Username,
                        Height = accountDTO.User.Height,
                        follower = follower.Count(),
                        following = follower.Count(),
                        IsVip = accountDTO.IsVip,
                        IsActive = accountDTO.IsActive,
                        User = accountDTO.User,
                        Posts = accountDTO.Posts,
                    };

                    // Nếu người dùng không phải là tài khoản xác thực
                    if (accountDTO.AccountId != account.AccountId)
                    {
                        // Nếu người dùng được tài khoản xác thực follow
                        if (followings.Any(f => f.FollowingId == accountDTO.AccountId))
                        {
                            getAccountResponses.isfollow = true;
                            result2.Add(getAccountResponses); // Thêm vào danh sách người dùng đã follow
                        }
                        else
                        {
                            result1.Add(getAccountResponses); // Thêm vào danh sách người dùng chưa follow
                        }
                    }
                }

                // Sắp xếp kết quả
                result1 = result1.OrderByDescending(a => a.RoleName == "KOL" ? 1 : 0)
                     .ThenByDescending(a => a.follower)
                     .ToList();

                result2 = result2.OrderByDescending(b => b.RoleName == "KOL" ? 1 : 0)
                     .ThenByDescending(a => a.follower)
                     .ToList();

                // Gộp danh sách người dùng đã follow và chưa follow vào danh sách kết quả
                totalResult.AddRange(result2);
                totalResult.AddRange(result1);

                return totalResult;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<GetAccountResponse>> SearchByUserNamee(string username)
        {
            try
            {
                
                // Sử dụng hàm SearchByUsername từ AccountDAO
                List<Account> accounts = await _unitOfWork.AccountDAO.SearchByUsername(username);

                // Chuyển đổi List<Account> thành List<AccountDTO> nếu cần thiết
                List<GetAccountResponse> accountDTOs = _mapper.Map<List<GetAccountResponse>>(accounts);

                return accountDTOs;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<GetAccountSuggest>> SearchByUserName(string username)
        {
            try
            {

                // Sử dụng hàm SearchByUsername từ AccountDAO
                List<Account> accounts = await _unitOfWork.AccountDAO.SearchByUsername(username);

                // Chuyển đổi List<Account> thành List<AccountDTO> nếu cần thiết
                List<GetAccountSuggest> accountDTOs = _mapper.Map<List<GetAccountSuggest>>(accounts);

                return accountDTOs;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Account> FindAccountByEmail(string email)
        {
            try
            {
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(email);
                return account;
            }catch(Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
        /*public async Task<List<GetAccountResponse>> GetSuggestionUsersAsync(HttpContext httpContext)
        {
            JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
            string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
            var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);


            var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người                       
            var listaccount = await _unitOfWork.AccountDAO.GetSuggestionAccount(followings);

            var Listaccounts = _mapper.Map<List<GetAccountResponse>>(listaccount); 
            List<GetAccountResponse> result = new List<GetAccountResponse>();
            foreach (var accountDTO in Listaccounts)
            {
                var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTO.AccountId);// người này có bao nhiêu follower
                GetAccountResponse getAccountResponses = new GetAccountResponse()
                {
                    AccountId = accountDTO.AccountId,
                    Email = "null", // Đảm bảo là null, không phải chuỗi rỗng
                    Firstname = "null",
                    Lastname = "null",
                    PasswordHash = "null",
                    AvatarUrl = "null",
                    Gender = accountDTO.Gender,
                    Username = accountDTO.Username,
                    Height = accountDTO.User.Height,
                    follower = follower.Count(),
                    User = accountDTO.User,
                    Posts = accountDTO.Posts,
                };
                result.Add(getAccountResponses);
            }
            
            
            *//*return _mapper.Map<List<GetAccountResponse>>(listaccount);*//*
            return result;

        }*/

        public async Task<List<GetAccountResponse>> GetSuggestionUsersAsyncc(HttpContext httpContext)
        {
            JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
            string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
            var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

            
            var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người                       
            var listaccount = await _unitOfWork.AccountDAO.GetSuggestionAccount(followings, account.AccountId);

            var Listaccounts = _mapper.Map<List<GetAccountResponse>>(listaccount);
            List<GetAccountResponse> result1 = new List<GetAccountResponse>();//khoi tao 1 cai list
            List<GetAccountResponse> result2 = new List<GetAccountResponse>();//khoi tao 1 cai list
            List<GetAccountResponse> Totalresult = new List<GetAccountResponse>();//khoi tao 1 cai list
            foreach (var accountDTO in Listaccounts)
            {
                var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTO.AccountId);// người này có bao nhiêu follower
                var account1 = await this._unitOfWork.AccountDAO.GetAccountById(accountDTO.AccountId);
                GetAccountResponse getAccountResponses = new GetAccountResponse()
                {
                    AccountId = accountDTO.AccountId,
                    Email = "null", // Đảm bảo là null, không phải chuỗi rỗng
                    Firstname = "null",
                    Lastname = "null",
                    PasswordHash = "null",
                    AvatarUrl = "null",
                    isfollow = false,
                    //Gender = accountDTO.Gender,
                    Username = accountDTO.Username,
                    //RoleName = account1.User.Role.RoleName,
                    Height = accountDTO.User.Height,
                    follower = follower.Count(),
                    User = accountDTO.User,
                    Posts = accountDTO.Posts,
                };
                /*if (getAccountResponses.RoleName == "KOL")
                {
                    result1.Insert(0, getAccountResponses);
                }
                else
                {
                    result1.Add(getAccountResponses);// trả về những người chưa follow 
                }*/
               
                result1.Add(getAccountResponses);// trả về những người chưa follow 
            }
            /*result1 = result1.OrderByDescending(a => a.RoleName == "KOL" ? 1 : 0)
                 .ThenByDescending(a => a.follower)
                 .ToList();*/
            var listrestaccount = await _unitOfWork.AccountDAO.GetListRestAccount(listaccount, account.AccountId);
            var listrestaccounts = _mapper.Map<List<GetAccountResponse>>(listrestaccount);
            foreach (var accountDTOO in listrestaccounts)
            {
                var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTOO.AccountId);// người này có bao nhiêu follower
                var account2 = await this._unitOfWork.AccountDAO.GetAccountById(accountDTOO.AccountId);
                GetAccountResponse getAccountResponsess = new GetAccountResponse()
                {
                    AccountId = accountDTOO.AccountId,
                    Email = "null", // Đảm bảo là null, không phải chuỗi rỗng
                    Firstname = "null",
                    Lastname = "null",
                    PasswordHash = "null",
                    AvatarUrl = "null",
                    isfollow = true,
                    //RoleName = account2.User.Role.RoleName,
                    //Gender = accountDTOO.Gender,
                    Username = accountDTOO.Username,
                    Height = accountDTOO.User.Height,
                    follower = follower.Count(),
                    User = accountDTOO.User,
                    Posts = accountDTOO.Posts,
                };
                /*if (getAccountResponsess.RoleName == "KOL")
                {
                    result2.Insert(0, getAccountResponsess);
                }
                else
                {
                    result2.Add(getAccountResponsess);// trả về những người chưa follow 
                }*/

                
                result2.Add(getAccountResponsess);// trả về những người đã follow 
            }
            /*result2 = result2.OrderByDescending(a => a.RoleName == "KOL" ? 1 : 0)
                 .ThenByDescending(a => a.follower)
                 .ToList();*/
            /*return _mapper.Map<List<GetAccountResponse>>(listaccount);*/
            Totalresult.AddRange(result2);
            Totalresult.AddRange(result1);
            return Totalresult;

        }
        public async Task<List<GetAccountSuggest>> GetSuggestionUsersAsync(HttpContext httpContext)
        {
            try 
            
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);


                var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người                       
                var listaccount = await _unitOfWork.AccountDAO.GetSuggestionAccount(followings, account.AccountId);

                var Listaccounts = _mapper.Map<List<GetAccountSuggest>>(listaccount);
                List<GetAccountSuggest> result1 = new List<GetAccountSuggest>();//khoi tao 1 cai list
                List<GetAccountSuggest> result2 = new List<GetAccountSuggest>();//khoi tao 1 cai list
                List<GetAccountSuggest> Totalresult = new List<GetAccountSuggest>();//khoi tao 1 cai list
                foreach (var accountDTO in Listaccounts)
                {
                    var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTO.AccountId);// người này có bao nhiêu follower
                    var account1 = await this._unitOfWork.AccountDAO.GetAccountById(accountDTO.AccountId);
                    GetAccountSuggest getAccountResponses = new GetAccountSuggest()
                    {
                        AccountId = accountDTO.AccountId,
                        UserId = accountDTO.UserId,
                        Email = accountDTO.Email, // Đảm bảo là null, không phải chuỗi rỗng
                        Firstname = accountDTO.Firstname,
                        Lastname = accountDTO.Lastname,
                        PasswordHash = accountDTO.PasswordHash,
                        AvatarUrl = account1.User.AvatarUrl,
                        isfollow = false,
                        //Gender = accountDTO.Gender,
                        RoleName = account1.User.Role.RoleName,
                        Username = accountDTO.Username,
                        Height = accountDTO.User.Height,
                        follower = follower.Count(),
                        following = follower.Count(),
                        IsVip = accountDTO.IsVip,
                        IsActive = accountDTO.IsActive,
                        User = accountDTO.User,
                        Posts = accountDTO.Posts,

                    };
                    if (getAccountResponses.RoleName == "KOL")
                    {
                        result1.Insert(0, getAccountResponses);
                    }
                    else
                    {
                        result1.Add(getAccountResponses);// trả về những người chưa follow 
                    }
                    //result1.Add(getAccountResponses);// trả về những người chưa follow 
                }
                result1 = result1.OrderByDescending(a => a.RoleName == "KOL" ? 1 : 0)
                     .ThenByDescending(a => a.follower)
                     .ToList();
                var listrestaccount = await _unitOfWork.AccountDAO.GetListRestAccount(listaccount, account.AccountId);
                var listrestaccounts = _mapper.Map<List<GetAccountSuggest>>(listrestaccount);
                foreach (var accountDTOO in listrestaccounts)
                {
                    var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountDTOO.AccountId);// người này có bao nhiêu follower
                    var account2 = await this._unitOfWork.AccountDAO.GetAccountById(accountDTOO.AccountId);
                    GetAccountSuggest getAccountResponsess = new GetAccountSuggest()
                    {
                        AccountId = accountDTOO.AccountId,
                        UserId = accountDTOO.UserId,
                        Email = accountDTOO.Email, // Đảm bảo là null, không phải chuỗi rỗng
                        Firstname = accountDTOO.Firstname,
                        Lastname = accountDTOO.Lastname,
                        PasswordHash = accountDTOO.PasswordHash,
                        AvatarUrl = account2.User.AvatarUrl,
                        isfollow = true,
                        //Gender = accountDTOO.Gender,
                        RoleName = account2.User.Role.RoleName,
                        Username = accountDTOO.Username,
                        Height = accountDTOO.User.Height,
                        follower = follower.Count(),
                        following = follower.Count(),
                        IsActive = accountDTOO.IsActive,
                        IsVip = accountDTOO.IsVip,
                        User = accountDTOO.User,
                        Posts = accountDTOO.Posts,
                    };
                    if (getAccountResponsess.RoleName == "KOL")
                    {
                        result2.Insert(0, getAccountResponsess);
                    }
                    else
                    {
                        result2.Add(getAccountResponsess);// trả về những người chưa follow 
                    }
                    //result2.Add(getAccountResponsess);// trả về những người đã follow 
                }
                result2 = result2.OrderByDescending(b => b.RoleName == "KOL" ? 1 : 0)
                     .ThenByDescending(a => a.follower)
                     .ToList();
                /*return _mapper.Map<List<GetAccountResponse>>(listaccount);*/
                Totalresult.AddRange(result2);
                Totalresult.AddRange(result1);
                return Totalresult;
            }
            
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        #region DeleteAccountAsync
        public async Task DeleteAccountAsync(int id)
        {
            try
            {
                var account = await _unitOfWork.AccountDAO.GetAccountById(id);
                if (account == null)
                {
                    throw new NotFoundException("Account id does not exist in the system.");
                }

                // Xóa tài khoản
                _unitOfWork.AccountDAO.DeleteAccount(account);

                // Xóa người dùng liên kết
                if (account.User != null)
                {
                    _unitOfWork.UserDAO.DeleteUser(account.User);
                }

                await _unitOfWork.CommitAsync();
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("Account Id", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion
        public async Task<GetAccountSuggest> GetSuggestionUsersIdAsync(int accountId, HttpContext httpContext)
        {
            JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
            string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
            var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

            var account1 = await _unitOfWork.AccountDAO.GetAccountById(accountId);

            var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người

            bool isFollowing = followings.Any(following => following.FollowingId == account1.AccountId);
            var accounts = _mapper.Map<GetAccountSuggest>(account1);
            GetAccountSuggest getAccountResponsess;
            if (isFollowing == true)
            {
                var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountId);// người này có bao nhiêu follower
                var following = await this._unitOfWork.userRelationDAO.GetFollowing(accountId);// người này có bao nhiêu following
                 getAccountResponsess = new GetAccountSuggest()
                {
                    AccountId = accounts.AccountId,
                    Email = "null", // Đảm bảo là null, không phải chuỗi rỗng
                    Firstname = "null",
                    Lastname = "null",
                    PasswordHash = "null",
                    AvatarUrl = "null",
                    isfollow = true,
                    RoleName = account1.User.Role.RoleName,
                    //Gender = accounts.Gender,
                    Username = accounts.Username,
                    StyleName = account1.Style.StyleName,
                    Height = accounts.User.Height,
                    follower = follower.Count(),
                    following = following.Count(),
                    User = accounts.User,
                    Posts = accounts.Posts,
                };
            }
            else // isFollowing == false
            {
                var follower = await this._unitOfWork.userRelationDAO.GetFollower(accountId);
                var following = await this._unitOfWork.userRelationDAO.GetFollowing(accountId);
                 getAccountResponsess = new GetAccountSuggest()
                {
                    AccountId = accounts.AccountId,
                    Email = "null",
                    Firstname = "null",
                    Lastname = "null",
                    PasswordHash = "null",
                    AvatarUrl = "null",
                    isfollow = false, // Đổi thành false
                                      //Gender = accounts.Gender,
                    RoleName = account1.User.Role.RoleName,
                    Username = accounts.Username,
                    StyleName = account1.Style.StyleName,
                    Height = accounts.User.Height,
                    follower = follower.Count(),
                    following = following.Count(),
                    User = accounts.User,
                    Posts = accounts.Posts,
                };


            }
            return getAccountResponsess;
        }
        public async Task ResetPassword(string email)
        {
            try
            {
                 var account = await _unitOfWork.AccountDAO.GetAccountByEmail(email);
                if(account == null)
                {
                    throw new NotFoundException("Account does not exist in system.");
                }
                
                var password = Guid.NewGuid().ToString().Substring(0,10);
                //string hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(password);
                account.PasswordHash = password;
                await _unitOfWork.AccountDAO.UpdateAccountProfile(account);
                await _unitOfWork.CommitAsync();
                var message = new GenZStyleAPP.BAL.Models.Message(new string[] { account.Email! }, "Reset old password to newpassword", password!);
                /*var confirmationLink = Url.Action(nameof(ConfirmEmail), "Users", new { token, email = user.Email }, Request.Scheme);
                var message = new GenZStyleAPP.BAL.Models.Message(new string[] { user.Email! }, "Confirmation email link", confirmationLink!);*/
                _emailRepository.SendEmail(message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


    }
}
