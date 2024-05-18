using BMOS.BAL.Authorization;
using Firebase.Auth;
using FluentValidation;
using FluentValidation.Results;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.UserRelations;
using GenZStyleAPP.BAL.DTOs.Users;
using GenZStyleAPP.BAL.Repository.Interfaces;
using GenZStyleAPP.BAL.Validators.Users;
using Microsoft.AspNetCore.Http;
using GenZStyleAPP.BAL.Models;
using GenZStyleAPP.BAL.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Options;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using System.Text.Json;

using GenZStyleApp_API.Models;
using GenZStyleAPP.BAL.DTOs.Accounts;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace GenZStyleApp_API.Controllers
{
    
    public class UsersController : ODataController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private IAccountRepository _AccountRepository;
        private IUserRepository _userRepository;
        private IValidator<RegisterRequest> _registerValidator;
        private IValidator<UpdateUserRequest> _updateUserValidator;
        private IOptions<FireBaseImage> _firebaseImageOptions;
        private readonly IEmailRepository _emailRepository;
        /*private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionResultExecutor<ViewResult> _viewExecutor;*/

        public UsersController(UserManager<IdentityUser> userManager,
            IUserRepository userRepository,
            IAccountRepository accountRepository,
             IEmailRepository emailRepository,
            IValidator<RegisterRequest> registerValidator,
            IValidator<UpdateUserRequest> updateUserValidator,
            IOptions<FireBaseImage> firebaseImageOptions
            /*IHttpContextAccessor httpContextAccessor,
            IActionResultExecutor<ViewResult> viewExecutor*/
            )
        {
            /*this._viewExecutor = viewExecutor;
            this._httpContextAccessor = httpContextAccessor;*/
            this._emailRepository = emailRepository;
            this._userManager = userManager;
            this._userRepository = userRepository;
            this._registerValidator = registerValidator;
            this._updateUserValidator = updateUserValidator;
            this._firebaseImageOptions = firebaseImageOptions; 
            this._AccountRepository = accountRepository;
        }

        #region Register
        [HttpPost("odata/Users/Register")]
        [EnableQuery]
        

        public async Task<IActionResult> Post([FromForm] RegisterRequest registerRequest)
       {    try 
            {
                ValidationResult validationResult = await _registerValidator.ValidateAsync(registerRequest);
                    if (!validationResult.IsValid)
                    {
                        string error = ErrorHelper.GetErrorsString(validationResult);
                        throw new BadRequestException(error);
                    }
                         GetUserResponse customer = await this._userRepository
                        .Register(_firebaseImageOptions.Value, registerRequest);

                IdentityUser user = new()
                {
                    Email = registerRequest.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = registerRequest.UserName,
                    TwoFactorEnabled = true
                };
                //Add Token to Verify the email....
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // Sử dụng giao thức HTTPS với domain trên Azure
                var scheme = "https";
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Users", new { token, email = user.Email }, scheme);
                var message = new GenZStyleAPP.BAL.Models.Message(new string[] { user.Email! }, "Confirmation email link", confirmationLink!);
                /*var confirmationLink = Url.Action(nameof(ConfirmEmail), "Users", new { token, email = user.Email }, Request.Scheme);
                var message = new GenZStyleAPP.BAL.Models.Message(new string[] { user.Email! }, "Confirmation email link", confirmationLink!);*/
                _emailRepository.SendEmail(message);

                /*return StatusCode(StatusCodes.Status200OK,
                    new Response { Status = "Success", Message = $"User created & Email sent to {user.Email} Successfully" });*/
                return Ok(new
                {
                    Status = $"User created & Email sent to {customer.Email} Successfully",
                    Data = customer
                });
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _AccountRepository.FindAccountByEmail(email);
            if (user != null)
            {

                /*return StatusCode(StatusCodes.Status200OK,
                  //new Response { Status = "Success", Message = "Email Verified Successfully" });
                  new Response { Status = "Success", Message = "Congratulations! You have successfully verified your email address. " +
                  "This important step is complete and you can continue to experience the app our GenZStyle perfectly" });*/
                var viewResult = new ViewResult
                {
                    ViewName = "EmailConfirmationSuccess" // Tên view bạn muốn trả về
                };
                return viewResult;


            }
            return StatusCode(StatusCodes.Status500InternalServerError,
                       new Response { Status = "Error", Message = "This User Doesnot exist!" });
        }
        #endregion
        #region Follower
        [HttpPost("odata/Users/Follower")]
        [EnableQuery]
        public async Task<IActionResult> Post(int AccountId)
        {
            try {
                GetUserRelationResponse getUserRelation = await _userRepository.FollowUser(AccountId, HttpContext);
                return Ok(getUserRelation); }
            catch(Exception ex) 
            {
                return BadRequest(ex.Message);
                    }
            
        }
        #endregion

        // GetAll
        [HttpGet("odata/Users/Active/User")]
        [EnableQuery]
        public async Task<IActionResult> ActiveUsers()
        {
            try
            {
                List<GetUserResponse> users = await this._userRepository.GetUsersAsync();
                return Ok(new
                {
                    Status = "Get List Success",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
        // GetAll
        [HttpGet("odata/Users/GetStaticUser/User")]
        [EnableQuery]
        public async Task<IActionResult> GetStaticUser()
        {
            try
            {
                GetUserStatistics users = await this._userRepository.GetUserstatistics();
                return Ok(new
                {
                    Status = "Get List Success",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("odata/Users/Active/User/{userId}")]
        [EnableQuery(MaxExpansionDepth = 3)]
        public async Task<IActionResult> ActiveUser(int userId)
        {
            try
            {
                GetUserResponse user = await this._userRepository.GetActiveUser(userId);

                // Kiểm tra nếu user không tồn tại
                if (user == null)
                {
                    return BadRequest("User not found. Please provide a valid userId.");
                }

                return Ok(new
                {
                    Status = "Get User By Id Success",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }

        #region Update User
        [HttpPut("User/{key}/UpdateUser")]
        [EnableQuery]
        /*[PermissionAuthorize("Admin")]*/
        public async Task<IActionResult> Put([FromRoute] int key, [FromForm] UpdateUserRequest updateUserRequest)
        {
            try
            {
                ValidationResult validationResult = await _updateUserValidator.ValidateAsync(updateUserRequest);
                if (!validationResult.IsValid)
                {
                    string error = ErrorHelper.GetErrorsString(validationResult);
                    throw new BadRequestException(error);
                }
                GetUserResponse user = await this._userRepository.UpdateUserProfileByAccountIdAsync(key,_firebaseImageOptions.Value,
                                                                                                                  updateUserRequest);

                user.City ??= "NULL";
                user.Address ??= "NULL";
                user.Height ??= 0;
                return Ok(new
                {
                    Status = "Update User Success",
                    Data = Updated(user)
                });



            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
        #endregion

        //#region Delete User 

        //[HttpDelete("User/{userId}")]
        //[EnableQuery]
        ////[PermissionAuthorize("Staff")]
        //public async Task<IActionResult> Delete([FromRoute] int userId)
        //{
        //    await this._userRepository.DeleteUserAsync(userId, this.HttpContext);
        //    return NoContent();
        //}
        //#endregion


        //ban user
        [EnableQuery]
        [HttpPut("odata/Users/{key}/OpenBanUserByAccountId")]
        //[PermissionAuthorize("Store Owner")]
        public async Task<IActionResult> OpenBanUser([FromRoute] int key)
        {
            try
            {
                GetUserResponse user = await this._userRepository.OpenBanUserAsync(key);

                if (user != null)
                {
                    return Ok(new
                    {
                        Status = " Open Ban User Successfully",
                        Data = user
                    });
                }
                else
                {
                    return StatusCode(400, new
                    {
                        Status = -1,
                        Message = "Ban User Fail"
                    });

                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        #region View Profile By AccountId
        [HttpGet("odata/Users/{key}/GetUserByAccountId")]
        [EnableQuery]
        //[PermissionAuthorize("Customer", "Store Owner")]
        public async Task<IActionResult> Get([FromRoute] int key)
        {
            try
            {
                GetUserProfile user = await _userRepository.GetUserByAccountIdAsync(key);

                // Kiểm tra xem user có tồn tại hay không
                if (user == null)
                {
                    return NotFound("User not found. Please provide a valid AccountId.");
                }

                // **Sửa đổi để đảm bảo City, Address, Height luôn hiển thị**

                user.City ??= "NULL";
                user.Address ??= "NULL";
                user.Height ??= 0;
                user.AvatarUrl ??= "NULL";

                // Trả về status thành công và dữ liệu user
                return Ok(new
                {
                    Status = "Get User By AccountId Success",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        [EnableQuery]
        [HttpGet("odata/UserProfile/Follow")]
        
        public async Task<IActionResult> ActiveProducts()
        {
            GetFollowRequest followResponses = await this._userRepository.GetFollowByProfileIdAsync(HttpContext);

            return Ok(followResponses);
        }

        [EnableQuery]
        [HttpGet("odata/AccountProfile/Follow")]

        public async Task<IActionResult> ActiveProducts(int AccountId)
        {
            try {
                GetFollowResponse followResponses = await this._userRepository.GetFollowByAccountIdAsync(AccountId);
                return Ok(followResponses); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}

