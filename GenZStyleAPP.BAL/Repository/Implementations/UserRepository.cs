using AutoMapper;
using GenZStyleAPP.BAL.DTOs.Accounts;
using GenZStyleApp.DAL.Models;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using ProjectParticipantManagement.DAL.Infrastructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMOS.DAL.Enums;
using GenZStyleAPP.BAL.Repository.Interfaces;
using System.Collections;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using GenZStyleApp.DAL.Enums;
using GenZStyleAPP.BAL.DTOs.FireBase;
using BMOS.BAL.Helpers;
using GenZStyleAPP.BAL.DTOs.UserRelations;
using Microsoft.Extensions.Hosting;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleApp.BAL.Helpers;
using System.Security.Principal;
using GenZStyleAPP.BAL.DTOs.Users;
using Microsoft.AspNetCore.Identity;
using Org.BouncyCastle.Asn1.Ocsp;
using Firebase.Auth;
using BCrypt.Net;
using System.Net.Http;

namespace GenZStyleAPP.BAL.Repository.Implementations
{
    public class UserRepository : IUserRepository
    {
       
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;
        public UserRepository( IUnitOfWork unitOfWork, IMapper mapper)
        {
            
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
        }


        public async Task<GetUserRelationResponse> FollowUser(int UserId,HttpContext httpContext)
        {
            try 
            {
                var accounts = await _unitOfWork.AccountDAO.GetAccountById(UserId);
                if (accounts == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }
                else if(accounts.AccountId == 9){
                    throw new NotFoundException("You cannot follow Admin.");
                }
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                GetUserRelationResponse getUserRelation = new GetUserRelationResponse();
                var follow = await _unitOfWork.userRelationDAO.GetFollowByPostIdAndAccountt(account.AccountId, UserId);
                if (follow != null)
                {
                    follow.FollowerId = account.AccountId;
                    follow.FollowingId = UserId;
                    follow.isFollow = !follow.isFollow;
                    await _unitOfWork.userRelationDAO.ChangeLike(follow);
                    getUserRelation = _mapper.Map<GetUserRelationResponse>(follow);
                }
                else {
                    
                    UserRelation userRelation = new UserRelation
                    {
                        FollowerId = account.AccountId,
                        FollowingId = UserId,
                        isFollow = true,
                        Account = account,                    
                    };

                    if (userRelation.FollowingId == userRelation.FollowerId)
                    {
                        throw new NotFoundException("AccountId has duplicate in system.");
                    }
                    await _unitOfWork.userRelationDAO.AddFollowAsync(userRelation);                   
                    getUserRelation = _mapper.Map<GetUserRelationResponse>(userRelation);
                    
                }
                await _unitOfWork.CommitAsync();
                return getUserRelation;
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("AccountId", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
             {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);

            }

        }
        public async Task<GetUserResponse> Register(FireBaseImage fireBaseImage,RegisterRequest registerRequest)
        {
            try
            {
                var role = await _unitOfWork.RoleDAO.GetRoleAsync((int)RoleEnum.Role.PLAYER);
                var customerByEmail = await _unitOfWork.UserDAO.GetUserByEmailAsync(registerRequest.Email);
                if (customerByEmail != null)
                {
                    throw new BadRequestException("Email already exist in the system.");
                }
                var customerByUserName = await _unitOfWork.UserDAO.GetUserByUserNameAsync(registerRequest.UserName);
                if (customerByUserName != null)
                {
                    throw new BadRequestException("UserName already exist in the system.");
                }
                var customerPhone = await _unitOfWork.UserDAO.GetUserByPhoneAsync(registerRequest.Phone);
                if (customerPhone != null)
                {
                    throw new BadRequestException("Phone already exist in the system.");
                }
                //string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.PasswordHash);
                // assign registerRequest to account
                Account account = new Account
                {   
                    Email = registerRequest.Email,
                    //PasswordHash = registerRequest.PasswordHash,
                    PasswordHash = registerRequest.PasswordHash,
                    Firstname = registerRequest.FirstName,
                    Lastname = registerRequest.LastName,
                    Username = registerRequest.UserName,
                    IsActive = true,
                    IsVip = 0,
                };


                // assign registerRequest to user
                GenZStyleApp.DAL.Models.User user = new GenZStyleApp.DAL.Models.User
                {
                    City = registerRequest.City,
                    RoleId = 3,
                    Address = registerRequest.Address,
                    Dob = registerRequest.Dob,
                    Gender = registerRequest.Gender,
                    Phone = registerRequest.Phone,
                    Height = registerRequest.Height,
                    Role = role,
                    Account = account,
                };

                
                if(registerRequest.Avatar != null) 
                {
                    // Upload image to firebase
                FileHelper.SetCredentials(fireBaseImage);
                FileStream fileStream = FileHelper.ConvertFormFileToStream(registerRequest.Avatar);
                Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "User");
                user.AvatarUrl = result.Item1; 
                }



                
                          
                await _unitOfWork.AccountDAO.AddAccountAsync(account);
                await _unitOfWork.UserDAO.AddNewUser(user);
                

                
                //Save to Database
                await _unitOfWork.CommitAsync();

                

                return new GetUserResponse
                {
                    Phone = user.Phone,
                    Height = user.Height,
                    UserID = user.UserId,
                    AccountId = account.AccountId,
                    Address = user.Address,
                    AvatarUrl = user.AvatarUrl,
                    Dob = user.Dob.Value,
                    City = user.City,
                    Gender = user.Gender.Value,
                    Account = _mapper.Map<GetAccountResponse>(user.Account),
                };
            }
            catch (BadRequestException ex)
            {
                string fieldNameError = "";
                if (ex.Message.ToLower().Contains("email"))
                {
                    fieldNameError = "Email";
                }
                else if (ex.Message.ToLower().Contains("phone"))
                {
                    fieldNameError = "Phone";
                }
                else if (ex.Message.ToLower().Contains("username"))
                {
                    fieldNameError = "UserName";
                }
                string error = ErrorHelper.GetErrorString(fieldNameError, ex.Message);
                throw new BadRequestException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }



        public async Task<List<GetUserResponse>> GetUsersAsync()
        {
            try
            {
                List<GenZStyleApp.DAL.Models.User> users = await this._unitOfWork.UserDAO.GetAllUser();
                List<GetUserResponse> userResponses = users.Select(user => new GetUserResponse
                {
                    UserID = user.UserId,
                    AccountId = user.Account.AccountId,
                    IsActive = user.Account.IsActive,
                    Address = user.Address,
                    Phone = user.Phone,
                    Height = user.Height,
                    AvatarUrl = user.AvatarUrl,
                    Email = user.Account.Email,
                    Username = user.Account.Username,
                    PasswordHash = user.Account.PasswordHash,
                    Dob = user.Dob,
                    /*City = user.City,
                    Role = user.Role.RoleName,*/
                    Gender = user.Gender,                    
                    //Account = this._mapper.Map<GetAccountResponse>(user.Account)
                    // Các thuộc tính khác của GetUserResponse
                }).ToList();
                return this._mapper.Map<List<GetUserResponse>>(userResponses);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }

        public async Task<GetUserResponse> GetActiveUser(int userId)
        {
            try
            {
                GenZStyleApp.DAL.Models.User user = await this._unitOfWork.UserDAO.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException("User does not exist in the system.");
                }
                return this._mapper.Map<GetUserResponse>(user);
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("User Id", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }

        #region UpdateUserProfileByAccountIdAsync
        public async Task<GetUserResponse> UpdateUserProfileByAccountIdAsync(int accountId,
                                                                                     FireBaseImage fireBaseImage,
                                                                                     UpdateUserRequest updateUserRequest)
        {
            try
            {
                GenZStyleApp.DAL.Models.User user = await _unitOfWork.UserDAO.GetUserByAccountIdAsync(accountId);

                if (user == null)
                {
                    throw new NotFoundException("AccountId does not exist in system");
                }

                user.City = updateUserRequest.City;
                user.Address = updateUserRequest.Address;
                user.Phone = updateUserRequest.Phone;
                user.Gender = updateUserRequest.Gender;
                user.Height = updateUserRequest.Height;
                user.Dob = updateUserRequest.Dob;
                //if (updateCustomerRequest.PasswordHash != null)
                //{
                //    customer.Account.PasswordHash = StringHelper.EncryptData(updateCustomerRequest.PasswordHash);
                //}

                #region Upload image to firebase
                if (updateUserRequest.Avatar != null)
                {
                    FileHelper.SetCredentials(fireBaseImage);
                    //await FileHelper.DeleteImageAsync(user.AvatarUrl, "User");
                    FileStream fileStream = FileHelper.ConvertFormFileToStream(updateUserRequest.Avatar);
                    Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "User");
                    user.AvatarUrl = result.Item1;
                    //customer.AvatarID = result.Item2;
                }
                #endregion

                _unitOfWork.UserDAO.UpdateUser(user);
                await this._unitOfWork.CommitAsync();
                
                
                return  this._mapper.Map<GetUserResponse>(user);
            }

            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("AccountId", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }

        }
        #endregion

        #region DeleteUserAsync
        public async Task DeleteUserAsync(int id, HttpContext httpContext)
        {
            try
            {
                //JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                //string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                //var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                var user = await _unitOfWork.UserDAO.GetUserByIdAsync(id);
                if (user == null)
                {
                    throw new NotFoundException("User id does not exist in the system.");
                }
                //product.Status = (int)ProductEnum.Status.INACTIVE;
                //if (product.ProductMeals != null && product.ProductMeals.Count() > 0)
                //{
                //    foreach (var productMeal in product.ProductMeals)
                //    {
                //        productMeal.Meal.Status = (int)MealEnum.Status.INACTIVE;
                //    }
                //}
                    user.Dob = DateTime.Today;
                //product.ModifiedBy = accountStaff.ID;
                _unitOfWork.UserDAO.DeleteUser(user);
                await _unitOfWork.CommitAsync();

            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("User Id", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion


        #region GetUserByAccountIdAsync
        public async Task<GetUserProfile> GetUserByAccountIdAsync(int accountId)
        {
            try
            {
                GenZStyleApp.DAL.Models.User user = await _unitOfWork.UserDAO.GetUserByAccountIdAsync(accountId);
                if (user == null)
                {
                    throw new NotFoundException("AccountId does not exist in system");
                }
                string roleName = user.Role.RoleName;
                GetUserProfile response = _mapper.Map<GetUserProfile>(user);
                response.Role = roleName;
                if(user.Account.Style != null) {
                    response.StyleName = user.Account.Style.Description; 
                }
                
                response.AccountId = accountId;
                return response;
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }

        }

        #endregion

        public async Task<GetUserStatistics> GetUserstatistics()
        {
            try
            {
                var userVIP = await _unitOfWork.AccountDAO.GetUserVIP();
                var userNormal = await _unitOfWork.AccountDAO.GetUserNormal();
                var userPremium = await _unitOfWork.AccountDAO.GetUserPremium();
                if (userVIP == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }
                if (userNormal == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }
                if (userPremium == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }

                // Tính toán phần trăm

                var userStatistics = new GetUserStatistics
                {
                      UserNormal  = userNormal.Count,
                      UserVIP  = userVIP.Count,
                      UserPremium = userPremium.Count
                 };
                return userStatistics;

                

            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        //ban user
        public async Task<GetUserResponse> OpenBanUserAsync(int accountId)
        {
            try
            {
                GenZStyleApp.DAL.Models.User user = await _unitOfWork.UserDAO.GetUserByAccountIdAsync(accountId);
                if (user == null)
                {
                    throw new NotFoundException("AccountId does not exist in system.");
                }
                //user.Accounts. = Convert.ToBoolean((int)AccountEnum.Status.INACTIVE);
                //_unitOfWork.UserDAO.BanUser(user);
                //await this._unitOfWork.CommitAsync();
                //return _mapper.Map<User>(user);

                // Lặp qua tất cả các tài khoản và thiết lập trạng thái             
                    user.Account.IsActive = true; // Đặt IsActive thành false              

                _unitOfWork.UserDAO.BanUser(user);
                await this._unitOfWork.CommitAsync();
                return _mapper.Map<GetUserResponse>(user);

            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }


        public async Task<GetFollowRequest> GetFollowByProfileIdAsync(HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);


                var followers = await this._unitOfWork.userRelationDAO.GetFollower(account.AccountId);// người này được bao nhiêu người follow 
                var UserRelationn = _mapper.Map<List<GetUserRelationResponse>>(followers);
                var followerAccountss = new List<GetAccountFollow>(); // Danh sách các tài khoản của người theo dõi
                foreach (var follower in UserRelationn)
                {
                    Account account2 = await _unitOfWork.AccountDAO.GetAccountById(follower.FollowerId);
                    GetAccountResponse followerAccounts = _mapper.Map<GetAccountResponse>(account2);
                    GetAccountFollow getAccountFollow = new GetAccountFollow
                    {
                        accountId = account2.AccountId,
                        Username = account2.Username,
                        avatar = account2.User.AvatarUrl != null ? account2.User.AvatarUrl : "",
                        Firstname = account2.Firstname,
                        Lastname = account2.Lastname,
                    };
                    /*followerAccounts.Add(follower.Account);
                    followerAccounts.Height ??= 0;
                    followerAccounts.Gender ??= null;
                    followerAccounts.RoleName ??= "";
                    followerAccounts.AvatarUrl ??= "";
                    foreach (var post in followerAccounts.Posts)
                    {
                        List<string>? hashtags = post.Hashtags.Any() ? post.Hashtags : null;

                        // Do something with the hashtags list, like printing them
                        if (hashtags != null)
                        {
                            Console.WriteLine("Hashtags for post:");
                            foreach (var hashtag in hashtags)
                            {
                                Console.WriteLine(hashtag);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No hashtags for this post.");
                        }
                    }*/
                    followerAccountss.Add(getAccountFollow);
                }
                var followings = await this._unitOfWork.userRelationDAO.GetFollowing(account.AccountId);//người này đang follow bao nhiêu người
                var UserRelationnn = _mapper.Map<List<GetUserRelationResponse>>(followings);
                var followingAccountss = new List<GetAccountFollow>(); // Danh sách các tài khoản của người theo dõi
                foreach (var following in UserRelationnn)
                {

                    Account account1 = await _unitOfWork.AccountDAO.GetAccountById(following.FollowingId);
                    //GetAccountResponse followingAccounts = _mapper.Map<GetAccountResponse>(account1);

                    GetAccountFollow getAccountFollowing = new GetAccountFollow
                    {
                        accountId = account1.AccountId,
                        Firstname = account1.Firstname,
                        Lastname = account1.Lastname,
                        Username = account1.Username,
                        avatar = account1.User.AvatarUrl != null ? account1.User.AvatarUrl : "",
                    };
                    /*followingAccounts.Height ??= 0;
                    followingAccounts.Gender ??= null;
                    followingAccounts.RoleName ??= "";
                    followingAccounts.AvatarUrl ??= "";
                    foreach (var post in followingAccounts.Posts)
                    {
                        List<string>? hashtags = post.Hashtags.Any() ? post.Hashtags : null;

                        // Do something with the hashtags list, like printing them
                        if (hashtags != null)
                        {
                            Console.WriteLine("Hashtags for post:");
                            foreach (var hashtag in hashtags)
                            {
                                Console.WriteLine(hashtag);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No hashtags for this post.");
                        }
                    }*/
                    followingAccountss.Add(getAccountFollowing);
                }
                GetFollowRequest followResponse = new GetFollowRequest
                {
                    /*Follower = followers.Count,
                    Followering = followings.Count,*/
                    Followers = followerAccountss,
                    Following = followingAccountss
                };

                return followResponse;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        
        public async Task<GetFollowResponse> GetFollowByAccountIdAsync(int accountId)
        {
            try
            {
                /*JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);*/


                List<UserRelation> followers = await this._unitOfWork.userRelationDAO.GetFollowerabyaccountid(accountId);// người này được bao nhiêu người follow 
                var UserRelationn = _mapper.Map<List<GetUserRelationAccountid>>(followers);
                var followerAccounts = new List<GetAccountSuggest>(); // Danh sách các tài khoản của người theo dõi
                foreach (var follower in UserRelationn)
                {
                    UserRelation userRelation = await _unitOfWork.userRelationDAO.GetFollowByPostIdAndAccount(follower.FollowerId, follower.FollowingId);
                    if (userRelation != null)
                    {
                        follower.Account.isfollow = userRelation.isFollow;
                        followerAccounts.Add(follower.Account);
                    }


                }
                var followings = await this._unitOfWork.userRelationDAO.GetFollowingbyaccountId(accountId);//người này đang follow bao nhiêu người
                var UserRelationnn = _mapper.Map<List<GetUserRelationAccountid>>(followings);
                var followingAccountss = new List<GetAccountSuggest>(); // Danh sách các tài khoản của người theo dõi
                foreach (var following in UserRelationnn)
                {
                    UserRelation userRelation = await _unitOfWork.userRelationDAO.GetFollowByPostIdAndAccount(following.FollowerId, following.FollowingId);
                    if (userRelation != null) {
                        Account account1 = await _unitOfWork.AccountDAO.GetAccountById(following.FollowingId);
                        GetAccountSuggest followingAccounts = _mapper.Map<GetAccountSuggest>(account1);
                        followingAccounts.isfollow = userRelation.isFollow;
                        //followingAccounts.isfollow = true;
                        followingAccountss.Add(followingAccounts); 
                    }
                        
                }
                GetFollowResponse followResponse = new GetFollowResponse
                {
                    /*Follower = followers.Count,
                    Followering = followings.Count,*/
                    Followers = followerAccounts,
                    Following = followingAccountss
                };

                return followResponse;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /*public async Task<GetFollowResponse> GetFollowByAccountIdAsync(int accountId)
        {
            try
            {
                *//*JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);*//*


                var followers = await this._unitOfWork.userRelationDAO.GetFollower(accountId);// người này được bao nhiêu người follow 
                var UserRelationn = _mapper.Map<List<GetUserRelationResponse>>(followers);
                var followerAccounts = new List<GetAccountFollow>(); // Danh sách các tài khoản của người theo dõi
                foreach (var follower in UserRelationn)
                {
                    Account account2 = await _unitOfWork.AccountDAO.GetAccountById(follower.FollowerId);
                    GetAccountFollow getAccountFollow = new GetAccountFollow
                    {
                        accountId = account2.AccountId,
                        Firstname = account2.Firstname,
                        Lastname = account2.Lastname,
                        Username = account2.Username,
                        avatar = account2.User.AvatarUrl != null ? account2.User.AvatarUrl : "",
                    };
                    followerAccounts.Add(getAccountFollow);
                }
                var followings = await this._unitOfWork.userRelationDAO.GetFollowing(accountId);//người này đang follow bao nhiêu người
                var UserRelationnn = _mapper.Map<List<GetUserRelationResponse>>(followings);
                var followingAccountss = new List<GetAccountFollow>(); // Danh sách các tài khoản của người theo dõi
                foreach (var following in UserRelationnn)
                {

                    Account account1 = await _unitOfWork.AccountDAO.GetAccountById(following.FollowingId);
                    //GetAccountResponse followingAccounts = _mapper.Map<GetAccountResponse>(account1);
                    GetAccountFollow getAccountFollow = new GetAccountFollow
                    {
                        accountId = account1.AccountId,
                        Firstname = account1.Firstname,
                        Lastname = account1.Lastname,
                        Username = account1.Username,
                        avatar = account1.User.AvatarUrl != null ? account1.User.AvatarUrl : "",
                        isfollow = true,
                    };

                    followingAccountss.Add(getAccountFollow);
                }
                GetFollowResponse followResponse = new GetFollowResponse
                {
                    *//*Follower = followers.Count,
                    Followering = followings.Count,*//*
                    Followers = followerAccounts,
                    Following = followingAccountss,
                };

                return followResponse;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }*/


    }
}
