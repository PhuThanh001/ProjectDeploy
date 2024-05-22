using AutoMapper;
using BMOS.DAL.Enums;
using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using System.IO;
using Firebase.Auth;
using GenZStyleApp.BAL.Helpers;
using GenZStyleApp.DAL.DAO;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.Accounts;
using GenZStyleAPP.BAL.DTOs.Comments;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.HashPosts;
using GenZStyleAPP.BAL.DTOs.HashTags;
using GenZStyleAPP.BAL.DTOs.PostLike;
using GenZStyleAPP.BAL.DTOs.Posts;
using GenZStyleAPP.BAL.DTOs.Postss;
using GenZStyleAPP.BAL.DTOs.Reports;
using GenZStyleAPP.BAL.DTOs.Users;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleAPP.BAL.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using ProjectParticipantManagement.DAL.Infrastructures;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GenZStyleAPP.BAL.Errors;
using Google.Rpc;
using static Google.Rpc.Context.AttributeContext.Types;
using System.Net;

namespace GenZStyleAPP.BAL.Repository.Implementations
{
    public class PostsRepository : IPostRepository
    {
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;
        private readonly IConfiguration _config;

        public PostsRepository(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
            _config = configuration;
        }

        #region GetPosts
        public async Task<List<GetPostResponse>> GetPostsAsync(HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                // Lấy tất cả bài post
                List<Post> allPosts = await _unitOfWork.PostDAO.GetPosts();

                // Lọc bài post để loại bỏ bài post của người dùng đăng nhập
                List<Post> filteredPosts = allPosts.Where(p => p.AccountId != accountStaff.AccountId).ToList();

                var postDTOs = this._mapper.Map<List<GetPostResponse>>(filteredPosts);
                foreach (var postDTO in postDTOs)
                {
                    if (postDTO.Link == null)
                    {
                        postDTO.Link = "NULL";
                    }
                }

                return postDTOs;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion

        public async Task<List<GetPostResponse>> GetActivePosts()
        {
            try
            {
                List<Post> posts = await _unitOfWork.PostDAO.GetActivePosts();                
                List<GetPostResponse> postResponses = new List<GetPostResponse>();

                foreach (var post in posts)
                {
                    // Tạo danh sách HashTags cho mỗi Post
                    List<GetHashPostsResponse> hashPosts = post.HashPosts.Select(hashPost => new GetHashPostsResponse
                    {
                        PostId = hashPost.PostId,
                        HashTageId = hashPost.HashTageId,
                        CreateAt = hashPost.CreateAt,
                        UpdateAt = hashPost.UpdateAt,
                        Hashtag = _mapper.Map<GetHashTagResponse>(hashPost.Hashtag)
                    }).ToList();

                    // Tạo danh sách GetPostLikeResponse từ post.Likes
                    List<GetPostLikeResponse> likes = post.Likes.Where(like => like.isLike == true).Select(like => new GetPostLikeResponse
                    {
                        PostId = like.PostId,
                        LikeBy = like.LikeBy,
                        isLike = like.isLike,
                        // Thực hiện mapping thông tin từ Account sang GetAccountResponse
                        Account = _mapper.Map<GetAccountResponse>(like.Account)
                        
                    }).ToList();

                    // Map thông tin từ Post sang GetPostResponse
                    GetPostResponse postResponse = new GetPostResponse
                    {
                        PostId = post.PostId,
                        AccountId = post.AccountId,
                        CreateTime = post.CreateTime,
                        UpdateTime = post.UpdateTime,
                        Content = post.Content,
                        Image = post.Image,
                        Status = post.Status,
                        Link = post.Link != null ? post.Link : "",
                        Username = post.Account.Username,
                        UserAvatar = post.Account.User.AvatarUrl,
                        /*HashPosts = hashPosts,*/
                        Likes = likes,
                       /* Account = _mapper.Map<GetAccountResponse>(post.Account)*/// Phần này cần điều chỉnh tùy vào cách bạn muốn xử lý Likes
                    };

                    // Tạo danh sách Hashtags từ HashPosts
                    postResponse.Hashtags = hashPosts.Select(h => h.Hashtag.Name).ToList();

                    postResponses.Add(postResponse);
                }

                return postResponses;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }

        public List<GetPostResponse> GetActivePostss()
        {
            try
            {
                List<Post> posts =  _unitOfWork.PostDAO.GetActivePostss();

                List<GetPostResponse> postResponses = new List<GetPostResponse>();

                foreach (var post in posts)
                {
                    // Tạo danh sách HashTags cho mỗi Post
                    List<GetHashPostsResponse> hashPosts = post.HashPosts.Select(hashPost => new GetHashPostsResponse
                    {
                        PostId = hashPost.PostId,
                        HashTageId = hashPost.HashTageId,
                        CreateAt = hashPost.CreateAt,
                        UpdateAt = hashPost.UpdateAt,
                        Hashtag = _mapper.Map<GetHashTagResponse>(hashPost.Hashtag)
                    }).ToList();

                    // Tạo danh sách GetPostLikeResponse từ post.Likes
                    List<GetPostLikeResponse> likes = post.Likes.Select(like => new GetPostLikeResponse
                    {
                        PostId = like.PostId,
                        LikeBy = like.LikeBy,
                        isLike = like.isLike,
                        // Thực hiện mapping thông tin từ Account sang GetAccountResponse
                        Account = _mapper.Map<GetAccountResponse>(like.Account)

                    }).ToList();

                    // Map thông tin từ Post sang GetPostResponse
                    GetPostResponse postResponse = new GetPostResponse
                    {
                        PostId = post.PostId,
                        AccountId = post.AccountId,
                        CreateTime = post.CreateTime,
                        UpdateTime = post.UpdateTime,
                        Content = post.Content,
                        Image = post.Image,
                        Link = post.Link != null ? post.Link : "",
                        /*HashPosts = hashPosts,*/
                        Likes = likes,
                        /* Account = _mapper.Map<GetAccountResponse>(post.Account)*/// Phần này cần điều chỉnh tùy vào cách bạn muốn xử lý Likes
                    };

                    // Tạo danh sách Hashtags từ HashPosts
                    postResponse.Hashtags = hashPosts.Select(h => h.Hashtag.Name).ToList();

                    postResponses.Add(postResponse);
                }
                var Post1 = _mapper.Map<List<GetPostResponse>>(posts);
                return Post1;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }

        #region GetPostDetailByIdAsync
        public async Task<GetPostResponse> GetPostDetailByIdAsync(int id)
        {
            try
            {
                var post = await _unitOfWork.PostDAO.GetPostByIdAsync(id);
                if (post == null)
                {
                    throw new NotFoundException("PostId id does not exist in the system.");
                }

                // Lấy danh sách người like cho post
                var likes = await this._unitOfWork.LikeDAO.GetAllAccountByLikes(id);
                var stylename = await _unitOfWork.PostDAO.GetStyleNameByPostIdAsync(id);

                //var postDTO = _mapper.Map<GetPostResponse>(post);
                GetPostResponse postDTO = new GetPostResponse
                {
                    PostId = post.PostId,
                    AccountId = post.AccountId,
                    CreateTime = post.CreateTime,
                    UpdateTime = post.UpdateTime,
                    Content = post.Content,
                    Image = post.Image,
                    Status = post.Status,
                    Link = post.Link != null ? post.Link : "",
                    Username = post.Account.Username,
                    UserAvatar = post.Account.User.AvatarUrl,
                    StyleName = stylename,
                    /*HashPosts = hashPosts,*/
                    //Likes = likes,
                    /* Account = _mapper.Map<GetAccountResponse>(post.Account)*/// Phần này cần điều chỉnh tùy vào cách bạn muốn xử lý Likes
                };
                // Gán danh sách người like vào postDTO, chỉ lấy thông tin cần thiết của account
                postDTO.Likes = likes.Select(like => new GetPostLikeResponse
                {
                    PostId = like.PostId,
                    LikeBy = like.LikeBy,
                    Account = new GetAccountResponse
                    {
                        AccountId = like.Account.AccountId,
                        UserId = like.Account.UserId,
                        //InboxId = like.Account.InboxId,                        
                        Email = like.Account.Email,
                        Firstname = like.Account.Firstname,
                        Lastname = like.Account.Lastname,
                        Username = like.Account.Username,
                        PasswordHash = like.Account.PasswordHash,
                        IsVip = like.Account.IsVip,
                        IsActive = like.Account.IsActive
                    }
                }).ToList();
                return postDTO;
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("Post Id", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion

        #region GetPostByAccountIdAsync
        public async Task<List<GetPostResponse>> GetPostByAccountIdAsync(int id)
        {
            try
            {
                var post = await _unitOfWork.PostDAO.GetPostByAccountIdAsync(id);
                if (post == null)
                {
                    throw new NotFoundException("Account id does not exist in the system.");
                }

                var postDTOs = _mapper.Map<List<GetPostResponse>>(post);
                //Staff staff = await this._unitOfWork.StaffDAO.GetStaffDetailAsync(product.ModifiedBy);
                //productDTO.ModifiedStaff = staff.FullName;
                foreach (var postDTO in postDTOs)
                {
                    if (postDTO.Link == null)
                    {
                        postDTO.Link = "NULL";
                    }
                    var styleNames = await _unitOfWork.PostDAO.GetStyleNameByPostIdAsync(postDTO.PostId);
                    postDTO.StyleName = styleNames;
                }

                return postDTOs;
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

        #region GetPostByAccountIdFollowAsync
        public async Task<List<GetPostSuggestion>> GetPostByAccountIdFollowAsync(int id)
        {
            try
            {
                var post = await _unitOfWork.PostDAO.GetPostByAccountIdAsync(id);
                if (post == null)
                {
                    throw new NotFoundException("Account id does not exist in the system.");
                }

                var postDTOs = _mapper.Map<List<GetPostSuggestion>>(post);
                //Staff staff = await this._unitOfWork.StaffDAO.GetStaffDetailAsync(product.ModifiedBy);
                //productDTO.ModifiedStaff = staff.FullName;
                foreach (var postDTO in postDTOs)
                {
                    if (postDTO.Link == null)
                    {
                        postDTO.Link = "NULL";
                    }
                }

                return postDTOs;
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

        #region GetPostByGenderAsync
        public async Task<List<GetPostResponse>> GetPostByGenderAsync(bool gender)
        {
            try
            {
                List<Post> post = await _unitOfWork.PostDAO.GetPostByGenderAsync(gender);
                if (post == null)
                {
                    throw new NotFoundException("Gender does not exist in system");
                }

                var postDTOs = _mapper.Map<List<GetPostResponse>>(post);

                foreach (var postDTO in postDTOs)
                {
                    if (postDTO.Link == null)
                    {
                        postDTO.Link = "NULL";
                    }
                }
                //sửa chỗ này
                return postDTOs;
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

        #region CreateNewPostAsync
        public async Task<CommonResponse> CreateNewPostAsync(AddPostRequest addPostRequest, FireBaseImage fireBaseImage, HttpContext httpContext)
        {
            
            string loginSuccessMsg = _config["ResponseMessages:AuthenticationMsg:UnauthenticationMsg"];
            string UploadImageSuccessedMsg = _config["ResponseMessages:CommonMsg:UploadImageSuccessedMsg"];
            string NotCreateSuccessMsg = _config["ResponseMessages:RolePermissionMsg:NotCreateSuccessMsg"];
            CommonResponse commonResponse = new CommonResponse();
            // Code inside the method
            try
            {
                //var images = new List<Post>();
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

                Post post = this._mapper.Map<Post>(addPostRequest);

                post.AccountId = accountStaff.AccountId;

                post.Content = addPostRequest.Content;
                post.CreateTime = DateTime.UtcNow;
                post.UpdateTime = DateTime.UtcNow;
                post.Account = accountStaff;
                post.Status = true;

                if (addPostRequest.Link == null)
                {
                    post.Link = "";
                }
                else
                {
                    post.Link = addPostRequest.Link;
                    if (accountStaff.User.RoleId != (int)RoleEnum.Role.KOL)
                    {
                        throw new Exception(loginSuccessMsg);
                    }
                }
                /*#region Upload video to firebase

                //foreach (var imageFile in addPostRequest.Image)
                //{
                FileHelper.SetCredentials(fireBaseImage);
                FileStream fileStream = FileHelper.ConvertFormFileToStream(addPostRequest.Video);
                Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "Post");

                // Assuming you want to store multiple image URLs
                // Consider creating a separate entity for images if necessary
                // and establish a one-to-many relationship with the Post entity
                // For now, appending URLs to the Image property
                post.Image = result.Item1; // Separate URLs by a delimiter
                                           //}
                                           // Kiểm tra hình ảnh có chứa nội dung khiêu dâm không
                                           //Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           //ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                                           //GoogleCredential credential = GoogleCredential.FromFile("D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           // Tạo đối tượng ImageAnnotatorClient với thông tin xác thực

                //Style style = new Style();
                #endregion*/

                #region Upload images to firebase

                //foreach (var imageFile in addPostRequest.Image)
                //{
                FileHelper.SetCredentials(fireBaseImage);
                FileStream fileStream = FileHelper.ConvertFormFileToStream(addPostRequest.Image);
                Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "Post");

                // Assuming you want to store multiple image URLs
                // Consider creating a separate entity for images if necessary
                // and establish a one-to-many relationship with the Post entity
                // For now, appending URLs to the Image property
                post.Image = result.Item1; // Separate URLs by a delimiter
                                           //}
                                           // Kiểm tra hình ảnh có chứa nội dung khiêu dâm không
                                           //Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           //ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                                           //GoogleCredential credential = GoogleCredential.FromFile("D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           // Tạo đối tượng ImageAnnotatorClient với thông tin xác thực
                var jsonCredentials = @"
{
    ""type"": ""service_account"",
    ""project_id"": ""starlit-casing-420509"",
    ""private_key_id"": ""1e405b20afeae0c199d1a261d4353fde65fdcb17"",
    ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDBGVgBQR5xnWsv\ndGKD8zG9L0H4+kiA5MMVY2H5uoExRuV0H/PKZFWxnHuu4AimugrG93FRv97792CI\nVXRGdU0esVbBqpgUtCTncUw//lMM3leZFhlyWKOsCkovXCy+QSqvfcYrdvz/7uID\nU78jqVfS2rY/phnZzGBYdmTmc2pOzmBBXGcxWo1WhjScLC+njNnHXA9zJ7jXh/eE\nRKyLlAt/5yH1bPz/Msg4x+/uoOFqQIMGUZzseJG0aZHrem5dHqWkg69xhwUaTFgM\n48yhuQAdZDLG7pB6hgFAii4NdF4XC661s8l0Lmv8Kz+1+kp7fUJrnDv0DtcVTPmm\nlvERTg7xAgMBAAECggEAXQnyDkCrA4EeuPGx2fPeflH7zpQBRSF0IaUN+v5y6yRG\nwEn49AaeB4LnUR4e7P5s3OsyjXoOXYcW2vmZma1BKE24CH5C4o7dzSoY3CiVKXkj\n6d3tCtxMmEMo9WLLKCxL/dxzFFQzmLkn6wR57xvT9gNtloNYp0lDDryU5ziq+ya7\nL7wA4khF4Q8O8Wxe6syl28SrNSSb/YpVvVksCovzVOmN9gNGNMJAJAkxOOn8QWQm\nTHy8JRfrifZB5ChIeyJ8uYtJ0IpgBnifdnytaoiVTv9bizvXq0Zv+hHQUAHYGNw+\n1FuFpreZ5Lvj6kN3IoIL5laRKBBwx+wVIHcG5+VSRQKBgQD5RrSncKvv+7YV4IOJ\n8STb+5Hoe/QMZoIKUXWoDdSCzHvJ0O30teYblCkApMQSHaMI1BJ9qXu969fHxUxs\nvCeeaiaHYc/lhj6ul2sZiTqSFfDFPywtej8jcae9MVB1MN4hO6nhaTyk/E5JXrgc\nYVoyH1a7dvLrGyfJgUE59sFrHwKBgQDGTrmcujYXm63Nagyrt/M6BVR+O5IxqEEw\nC2y9Q5AoHPm9VvgzR0LK5YxmTxkcB9KS3Hj0GbrONOpSM0NlYD7mP25GoojZ5gL2\nBaXwONVXBCl+hKSeNO6X8+Mox9KLntcXnB5N0XRYrrp2BPWnP2mJNIFvJoUKYlkI\nct7aYwBT7wKBgGeIqnfxIDiov1QO0BN3COwlbNC2ywJrgQ9wDgIi3GUHMVL9aBf/\nhPdnbjBKPvWXQaPlEVkID9EuJ41dsQRokbwGMsKAKa0XOOmjEmSkzqLmYf5K4rpw\nz9CU4CqAVP6XsWr0MPbiksGj2ZA3uxhuhtvSkF+EZBiqSrgy6zh7+JHZAoGAWALM\ngfBq3WNsaYQth3CmdBO1giiyI3PHKqmHYyA/NG5XsF6O3UM9M4tZGnb0b+pQ3HkY\n/U0GCUqWzFCQEsf6Ynm4WYT9M6fPnJy5Hro6hNoGCG6aGNTpJ2tIX+r/WJPwZjwV\nfvf8qPczLfnZhJayIgC5iTkRRqCLXyKMIWRa2uUCgYAZX8pzlX+RdcihfkgRifQC\n/SQDxOJCFEvtoh79DnGKMikgADTJ9JvxXAqlC7naXUvATKZ7xGi46B0O/wK5jVtO\nd+meop/C8Jb2Q8gi7Zx5Mvk+o+7foAx/02hA+5Ri8FmO5EK6ynCwhbB/vn/mvnQK\n8km+mrR1RgilvK8cfGhxFw==\n-----END PRIVATE KEY-----\n"",
    ""client_email"": ""genzstyleapp@starlit-casing-420509.iam.gserviceaccount.com"",
    ""client_id"": ""102338108674487322065"",
    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
    ""token_uri"": ""https://oauth2.googleapis.com/token"",
    ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
    ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/genzstyleapp%40starlit-casing-420509.iam.gserviceaccount.com"",
    ""universe_domain"": ""googleapis.com""
}";
                var client = new ImageAnnotatorClientBuilder
                {
                    JsonCredentials = jsonCredentials
                }.Build();
                using (HttpClient clientt = new HttpClient())
                {
                    var responsee = await clientt.GetAsync(result.Item1);
                    if (responsee.IsSuccessStatusCode)
                    {
                        using (Stream stream = await responsee.Content.ReadAsStreamAsync())
                        {
                            Image image = Image.FromStream(stream);
                            // Sử dụng hình ảnh ở đây

                            var safeSearch = client.DetectSafeSearch(image);
                            if (safeSearch.Adult == Likelihood.VeryLikely || safeSearch.Racy == Likelihood.VeryLikely)
                            {
                                // Nếu phát hiện hình ảnh có nội dung khiêu dâm, xử lý tương ứng
                                throw new Exception("Hình ảnh chứa nội dung người lớn.");
                            }
                            else if (safeSearch.Violence == Likelihood.VeryLikely)
                            {
                                throw new Exception("Hình ảnh chứa nội dung bạo lực.");
                            }
                        }

                    }
                    else
                    {
                        // Xử lý khi không thể tải hình ảnh từ URL
                        throw new Exception("The URL can not dowload.");
                    }
                }
                //Style style = new Style();
                #endregion
                if (addPostRequest.StyleOfPosts != null && addPostRequest.StyleOfPosts.Any())
                {
                    foreach (string styleName in addPostRequest.StyleOfPosts)
                    {
                        List<Style> existingStyles = new List<Style>();


                       Style style = new Style()
                        {
                            StyleName = styleName,
                            CreateAt = DateTime.UtcNow,
                            UpdateAt = DateTime.UtcNow,
                            CategoryId = 1,
                            //Description = addPostRequest.StylePost
                        };
                        
                        await _unitOfWork.StyleDAO.AddNewStyle(style);
                        await _unitOfWork.CommitAsync();

                        existingStyles = new List<Style> { style };
                    

                    if (post.StylePosts == null)
                    {
                        post.StylePosts = new List<StylePost>();
                    }

                    foreach (Style existingHashtag in existingStyles)
                    {
                        StylePost stylePost1 = new StylePost
                        {
                            Post = post,
                            Style = existingHashtag,
                            CreateAt = DateTime.UtcNow,
                            //UpdateAt = DateTime.Now
                        };

                        post.StylePosts.Add(stylePost1);
                    }
                }
            }
                //Process StyleName
                /*Style style = new Style()
                {
                    StyleName = addPostRequest.StylePost,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                    CategoryId = 1,
                    Description = addPostRequest.StylePost
                };*/
                
                
                // Process hashtags
                if (addPostRequest.Hashtags != null && addPostRequest.Hashtags.Any())
                    {
                        foreach (string hashtagName in addPostRequest.Hashtags)
                        {
                            List<Hashtag> existingHashtags = await _unitOfWork.HashTagDAO.SearchByHashTagNames(hashtagName);

                            if (existingHashtags == null || existingHashtags.Count == 0)
                            {
                                Hashtag newHashtag = new Hashtag
                                {
                                    Name = hashtagName,
                                    // Set other properties as needed
                                };

                                existingHashtags = new List<Hashtag> { newHashtag };
                            }

                            if (post.HashPosts == null)
                            {
                                post.HashPosts = new List<HashPost>();
                            }

                            foreach (Hashtag existingHashtag in existingHashtags)
                            {
                                HashPost hashPost = new HashPost
                                {
                                    Post = post,
                                    Hashtag = existingHashtag,
                                    CreateAt = DateTime.UtcNow,
                                    UpdateAt = DateTime.UtcNow
                                };

                                post.HashPosts.Add(hashPost);
                            }
                        }
                    }
                    #endregion
                    await _unitOfWork.PostDAO.AddNewPost(post);
                    await _unitOfWork.CommitAsync();
                    GetPostResponse response = this._mapper.Map<GetPostResponse>(post);

                    // Lấy danh sách hashtag và thêm vào GetPostResponse
                    if (addPostRequest.Hashtags != null)
                    {
                        List<string> hashtags = post.HashPosts.Select(hp => hp.Hashtag.Name).ToList();
                        response.Hashtags = hashtags;
                    }
                    commonResponse.Data = response;
                    commonResponse.Status = 200;
                    commonResponse.Message = UploadImageSuccessedMsg;
                    return commonResponse;

                
            }
            /*catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }*/
            catch (Exception ex)
            {
                commonResponse.Message = ex.Message;
                commonResponse.Status = 405;
            }
            return commonResponse;

        }


        #region UpdatePostProfileByIdAsync
        public async Task<CommonResponse> UpdatePostProfileByPostIdAsync(int postId,
                                                                                     FireBaseImage fireBaseImage,
                                                                                     UpdatePostRequest updatePostRequest, HttpContext httpContext)
        {
            string UploadImageSuccessedMsg = _config["ResponseMessages:CommonMsg:UploadImageSuccessedMsg"];

            CommonResponse commonResponse = new CommonResponse();

            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                Post post = await _unitOfWork.PostDAO.GetPostByIdAsync(postId);
                var hashpost = await _unitOfWork.HashPostDAO.GetHashPostByPostIdAsync(postId);
                var stylePost = await _unitOfWork.StylePostDAO.GetStylePostByPostIdAsync(postId);
                var Style = await _unitOfWork.StyleDAO.FindStyleByPostId(postId);
                var colletion = await _unitOfWork.CollectionDAO.GetAllCollectionsOnlyByPostId(postId); 
                if (post == null)
                {
                    throw new NotFoundException("PostId does not exist in system");
                }
                if (updatePostRequest.Content != null)
                { 
                    post.Content = updatePostRequest.Content; 
                }
                
                if (updatePostRequest.Link != null)
                {
                    post.Link = updatePostRequest.Link;
                }
                
                //if (updateCustomerRequest.PasswordHash != null)
                //{
                //    customer.Account.PasswordHash = StringHelper.EncryptData(updateCustomerRequest.PasswordHash);
                //}

                #region Upload image to firebase
                if (updatePostRequest.Image != null)
                {
                    FileHelper.SetCredentials(fireBaseImage);
                    //await FileHelper.DeleteImageAsync(user.AvatarUrl, "User");
                    FileStream fileStream = FileHelper.ConvertFormFileToStream(updatePostRequest.Image);
                    Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "Post");
                    post.Image = result.Item1;
                    //customer.AvatarID = result.Item2;

                    var jsonCredentials = @"
{
    ""type"": ""service_account"",
    ""project_id"": ""starlit-casing-420509"",
    ""private_key_id"": ""1e405b20afeae0c199d1a261d4353fde65fdcb17"",
    ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDBGVgBQR5xnWsv\ndGKD8zG9L0H4+kiA5MMVY2H5uoExRuV0H/PKZFWxnHuu4AimugrG93FRv97792CI\nVXRGdU0esVbBqpgUtCTncUw//lMM3leZFhlyWKOsCkovXCy+QSqvfcYrdvz/7uID\nU78jqVfS2rY/phnZzGBYdmTmc2pOzmBBXGcxWo1WhjScLC+njNnHXA9zJ7jXh/eE\nRKyLlAt/5yH1bPz/Msg4x+/uoOFqQIMGUZzseJG0aZHrem5dHqWkg69xhwUaTFgM\n48yhuQAdZDLG7pB6hgFAii4NdF4XC661s8l0Lmv8Kz+1+kp7fUJrnDv0DtcVTPmm\nlvERTg7xAgMBAAECggEAXQnyDkCrA4EeuPGx2fPeflH7zpQBRSF0IaUN+v5y6yRG\nwEn49AaeB4LnUR4e7P5s3OsyjXoOXYcW2vmZma1BKE24CH5C4o7dzSoY3CiVKXkj\n6d3tCtxMmEMo9WLLKCxL/dxzFFQzmLkn6wR57xvT9gNtloNYp0lDDryU5ziq+ya7\nL7wA4khF4Q8O8Wxe6syl28SrNSSb/YpVvVksCovzVOmN9gNGNMJAJAkxOOn8QWQm\nTHy8JRfrifZB5ChIeyJ8uYtJ0IpgBnifdnytaoiVTv9bizvXq0Zv+hHQUAHYGNw+\n1FuFpreZ5Lvj6kN3IoIL5laRKBBwx+wVIHcG5+VSRQKBgQD5RrSncKvv+7YV4IOJ\n8STb+5Hoe/QMZoIKUXWoDdSCzHvJ0O30teYblCkApMQSHaMI1BJ9qXu969fHxUxs\nvCeeaiaHYc/lhj6ul2sZiTqSFfDFPywtej8jcae9MVB1MN4hO6nhaTyk/E5JXrgc\nYVoyH1a7dvLrGyfJgUE59sFrHwKBgQDGTrmcujYXm63Nagyrt/M6BVR+O5IxqEEw\nC2y9Q5AoHPm9VvgzR0LK5YxmTxkcB9KS3Hj0GbrONOpSM0NlYD7mP25GoojZ5gL2\nBaXwONVXBCl+hKSeNO6X8+Mox9KLntcXnB5N0XRYrrp2BPWnP2mJNIFvJoUKYlkI\nct7aYwBT7wKBgGeIqnfxIDiov1QO0BN3COwlbNC2ywJrgQ9wDgIi3GUHMVL9aBf/\nhPdnbjBKPvWXQaPlEVkID9EuJ41dsQRokbwGMsKAKa0XOOmjEmSkzqLmYf5K4rpw\nz9CU4CqAVP6XsWr0MPbiksGj2ZA3uxhuhtvSkF+EZBiqSrgy6zh7+JHZAoGAWALM\ngfBq3WNsaYQth3CmdBO1giiyI3PHKqmHYyA/NG5XsF6O3UM9M4tZGnb0b+pQ3HkY\n/U0GCUqWzFCQEsf6Ynm4WYT9M6fPnJy5Hro6hNoGCG6aGNTpJ2tIX+r/WJPwZjwV\nfvf8qPczLfnZhJayIgC5iTkRRqCLXyKMIWRa2uUCgYAZX8pzlX+RdcihfkgRifQC\n/SQDxOJCFEvtoh79DnGKMikgADTJ9JvxXAqlC7naXUvATKZ7xGi46B0O/wK5jVtO\nd+meop/C8Jb2Q8gi7Zx5Mvk+o+7foAx/02hA+5Ri8FmO5EK6ynCwhbB/vn/mvnQK\n8km+mrR1RgilvK8cfGhxFw==\n-----END PRIVATE KEY-----\n"",
    ""client_email"": ""genzstyleapp@starlit-casing-420509.iam.gserviceaccount.com"",
    ""client_id"": ""102338108674487322065"",
    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
    ""token_uri"": ""https://oauth2.googleapis.com/token"",
    ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
    ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/genzstyleapp%40starlit-casing-420509.iam.gserviceaccount.com"",
    ""universe_domain"": ""googleapis.com""
}";
                    var client = new ImageAnnotatorClientBuilder
                    {
                        JsonCredentials = jsonCredentials
                    }.Build();
                    using (HttpClient clientt = new HttpClient())
                    {
                        var responsee = await clientt.GetAsync(result.Item1);
                        if (responsee.IsSuccessStatusCode)
                        {
                            using (Stream stream = await responsee.Content.ReadAsStreamAsync())
                            {
                                Image image = Image.FromStream(stream);
                                // Sử dụng hình ảnh ở đây

                                var safeSearch = client.DetectSafeSearch(image);
                                if (safeSearch.Adult == Likelihood.VeryLikely || safeSearch.Racy == Likelihood.VeryLikely)
                                {
                                    // Nếu phát hiện hình ảnh có nội dung khiêu dâm, xử lý tương ứng
                                    throw new Exception("Hình ảnh chứa nội dung người lớn.");
                                }
                                else if (safeSearch.Violence == Likelihood.VeryLikely)
                                {
                                    throw new Exception("Hình ảnh chứa nội dung bạo lực.");
                                }
                            }

                        }
                        else
                        {
                            // Xử lý khi không thể tải hình ảnh từ URL
                            throw new Exception("The URL can not dowload.");
                        }
                    }
                }
                if (colletion != null)
                {
                    foreach(Collection collection1 in colletion) 
                    {
                        collection1.Name = post.Content;
                        collection1.Image_url = post.Image;

                        await _unitOfWork.CollectionDAO.UpdateCollection(collection1);

                    }
                }
                    if (stylePost != null)
                {
                    foreach (StylePost stylePost1 in stylePost)
                    {
                        await _unitOfWork.StylePostDAO.DeleteStylePost(stylePost1);
                        await _unitOfWork.CommitAsync();
                    }
                }
                if (Style != null)
                {
                    foreach (Style style1 in Style)
                    {
                        await _unitOfWork.StyleDAO.DeleteStyle(style1);
                        await _unitOfWork.CommitAsync();
                    }
                }
                #endregion
                if (updatePostRequest.StyleOfPosts != null && updatePostRequest.StyleOfPosts.Any())
                {
                    foreach (string styleName in updatePostRequest.StyleOfPosts)
                    {
                        List<Style> existingStyles = new List<Style>();


                        Style style = new Style()
                        {
                            StyleName = styleName,
                            CreateAt = DateTime.UtcNow,
                            UpdateAt = DateTime.UtcNow,
                            CategoryId = 1,
                            //Description = addPostRequest.StylePost
                        };

                        await _unitOfWork.StyleDAO.AddNewStyle(style);
                        await _unitOfWork.CommitAsync();

                        existingStyles = new List<Style> { style };


                        if (post.StylePosts == null)
                        {
                            post.StylePosts = new List<StylePost>();
                        }

                        foreach (Style existingHashtag in existingStyles)
                        {
                            StylePost stylePost1 = new StylePost
                            {
                                Post = post,
                                Style = existingHashtag,
                                CreateAt = DateTime.UtcNow,
                                //UpdateAt = DateTime.Now
                            };

                            post.StylePosts.Add(stylePost1);
                        }
                    }
                }

                if (hashpost != null)
                {
                    foreach (HashPost hashpost1 in hashpost)
                    {
                        await _unitOfWork.HashPostDAO.DeleteHashPost(hashpost1);
                        await _unitOfWork.CommitAsync();
                    }
                }

                // Process hashtags
                if (updatePostRequest.Hashtags != null && updatePostRequest.Hashtags.Any())
                {
                    foreach (string hashtagName in updatePostRequest.Hashtags)
                    {
                        List<Hashtag> existingHashtags = await _unitOfWork.HashTagDAO.SearchByHashTagNames(hashtagName);

                        if (existingHashtags == null || existingHashtags.Count == 0)
                        {
                            Hashtag newHashtag = new Hashtag
                            {
                                Name = hashtagName,
                                // Set other properties as needed
                            };

                            existingHashtags = new List<Hashtag> { newHashtag };
                        }

                        if (post.HashPosts == null)
                        {
                            post.HashPosts = new List<HashPost>();
                        }

                        foreach (Hashtag existingHashtag in existingHashtags)
                        {
                            HashPost hashPost = new HashPost
                            {
                                Post = post,
                                Hashtag = existingHashtag,
                                CreateAt = DateTime.Now,
                                UpdateAt = DateTime.Now
                            };

                            post.HashPosts.Add(hashPost);
                        }
                    }
                }



                _unitOfWork.PostDAO.UpdatePost(post);
                await this._unitOfWork.CommitAsync();
                GetPostResponse getPostResponse = _mapper.Map<GetPostResponse>(post);

                commonResponse.Data = getPostResponse;
                commonResponse.Status = 200;
                commonResponse.Message = "Update Post Successfully";
                return commonResponse;
            }
            catch (NotFoundException ex)
            {
                string error = ErrorHelper.GetErrorString("PostId", ex.Message);
                throw new NotFoundException(error);
            }
            catch (Exception ex)
            {
                commonResponse.Message = ex.Message;
                commonResponse.Status = 405;
            }
            return commonResponse;
            
        }
        #endregion



        

        public async Task<List<GetPostSuggestion>> GetPostByUserFollowId(HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

                List<GetPostSuggestion> getPostResponsess = new List<GetPostSuggestion>();

                HashSet<int> uniqueFollowingIds = new HashSet<int>();
                var listRelation = await _unitOfWork.userRelationDAO.GetFollowing(account.AccountId);
                foreach (var userRelation in listRelation)
                {
                    // Kiểm tra xem thuộc tính FollowingId có giá trị không null
                    if (userRelation.FollowingId != null)
                    {
                        // Thêm giá trị của FollowingId vào mảng số nguyên
                        uniqueFollowingIds.Add(userRelation.FollowingId);
                    }

                }
                foreach (int followingId in uniqueFollowingIds)
                {
                    var posts = await _unitOfWork.PostDAO.GetPostByAccountIdAsync(followingId);
                    foreach (Post post in posts)
                    {
                        GetPostSuggestion getPostResponse = new GetPostSuggestion
                        {
                            PostId = post.PostId,
                            AccountId = post.AccountId,
                            CreateTime = post.CreateTime,
                            UpdateTime = post.UpdateTime,
                            Content = post.Content,
                            Image = post.Image,
                            Link = string.IsNullOrWhiteSpace(post.Link) ? null : post.Link,
                        // Lấy thông tin tên người dùng và email từ tài khoản
                            Username = post.Account.Username,
                            Hashtags = post.HashPosts.Select(h => h.Hashtag.Name).ToList(),
                            /*Likes = _mapper.Map<List<GetPostLikeSuggestion>>(
                                post.Likes),*/
                        };
                        List<Like> Likes = await _unitOfWork.LikeDAO.GetLikeByPostId(post.PostId);
                        getPostResponse.Likes = _mapper.Map<List<GetPostLikeSuggestion>>(Likes);
                        // Thêm GetPostResponse vào danh sách getPostResponsess
                        getPostResponsess.Add(getPostResponse);
                    }
                }
                return this._mapper.Map<List<GetPostSuggestion>>(getPostResponsess);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //ban post
        public async Task<GetPostResponse> BanPostAsync(int postId, HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                Post post = await _unitOfWork.PostDAO.GetPostByIdAsync(postId);
                if (post == null)
                {
                    throw new NotFoundException("PostId does not exist in system.");
                }
                post.Status = false; // Đặt IsActive thành false


                _unitOfWork.PostDAO.UpdatePost(post);
                await this._unitOfWork.CommitAsync();
                return _mapper.Map<GetPostResponse>(post);

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
        #region ListPostByStyleName
        public async Task<List<GetPostResponse>> GetListPostStyleName(string? stylename)
        {
            try
            {
                List<Post> posts = await _unitOfWork.PostDAO.GetListPostByStyleName(stylename);
                List<GetPostResponse> postResponses = new List<GetPostResponse>();

                foreach (var post in posts)
                {
                    // Tạo danh sách HashTags cho mỗi Post
                    List<GetHashPostsResponse> hashPosts = post.HashPosts.Select(hashPost => new GetHashPostsResponse
                    {
                        PostId = hashPost.PostId,
                        HashTageId = hashPost.HashTageId,
                        CreateAt = hashPost.CreateAt,
                        UpdateAt = hashPost.UpdateAt,
                        Hashtag = _mapper.Map<GetHashTagResponse>(hashPost.Hashtag)
                    }).ToList();

                    // Tạo danh sách GetPostLikeResponse từ post.Likes
                    List<GetPostLikeResponse> likes = post.Likes.Where(like => like.isLike == true).Select(like => new GetPostLikeResponse
                    {
                        PostId = like.PostId,
                        LikeBy = like.LikeBy,
                        isLike = like.isLike,// Thực hiện mapping thông tin từ Account sang GetAccountResponse
                        Account = _mapper.Map<GetAccountResponse>(like.Account)


                    }).ToList();
                    // Lấy danh sách StyleName từ Post
                    List<string> styleNames = await _unitOfWork.PostDAO.GetStyleNameByPostIdAsync(post.PostId);

                    // Map thông tin từ Post sang GetPostResponse
                    GetPostResponse postResponse = new GetPostResponse
                    {
                        PostId = post.PostId,
                        AccountId = post.AccountId,
                        CreateTime = post.CreateTime,
                        UpdateTime = post.UpdateTime,
                        Content = post.Content,
                        Image = post.Image,
                        Status = post.Status,
                        Link = post.Link != null ? post.Link : "",
                        Username = post.Account.Username,
                        UserAvatar = post.Account.User.AvatarUrl,
                        StyleName = styleNames,
                        /*HashPosts = hashPosts,*/
                        Likes = likes,
                        /* Account = _mapper.Map<GetAccountResponse>(post.Account)*/// Phần này cần điều chỉnh tùy vào cách bạn muốn xử lý Likes
                    };

                    // Tạo danh sách Hashtags từ HashPosts
                    postResponse.Hashtags = hashPosts.Select(h => h.Hashtag.Name).ToList();

                    postResponses.Add(postResponse);
                }

                return postResponses;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion
        public async Task DeletePostById(int id)
        {
            try
            {
                var Post = await _unitOfWork.PostDAO.GetPostByIdAsync(id);
                await _unitOfWork.PostDAO.DeletePost(Post);
                await _unitOfWork.CommitAsync();
           
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
        #region List các bài post có nhiều lượt like tương tác trong hệ thống
        public async Task<List<GetPostResponse>> GetActivePostsByLike()
        {
            try
            {
                List<Post> posts = await _unitOfWork.PostDAO.GetTopActivePostsOrderedByLikes();
                List<GetPostResponse> postResponses = new List<GetPostResponse>();

                foreach (var post in posts)
                {
                    List<GetHashPostsResponse> hashPosts = post.HashPosts.Select(hashPost => new GetHashPostsResponse
                    {
                        PostId = hashPost.PostId,
                        HashTageId = hashPost.HashTageId,
                        CreateAt = hashPost.CreateAt,
                        UpdateAt = hashPost.UpdateAt,
                        Hashtag = _mapper.Map<GetHashTagResponse>(hashPost.Hashtag)
                    }).ToList();

                    List<GetPostLikeResponse> likes = post.Likes.Where(like => like.isLike == true).Select(like => new GetPostLikeResponse
                    {
                        PostId = like.PostId,
                        LikeBy = like.LikeBy,
                        isLike = like.isLike,
                        Account = _mapper.Map<GetAccountResponse>(like.Account)
                    }).ToList();

                    GetPostResponse postResponse = new GetPostResponse
                    {
                        PostId = post.PostId,
                        AccountId = post.AccountId,
                        CreateTime = post.CreateTime,
                        UpdateTime = post.UpdateTime,
                        Content = post.Content,
                        Image = post.Image,
                        Status = post.Status,
                        Link = post.Link != null ? post.Link : "",
                        Username = post.Account.Username,
                        UserAvatar = post.Account.User.AvatarUrl,
                        Likes = likes
                    };

                    postResponse.Hashtags = hashPosts.Select(h => h.Hashtag.Name).ToList();

                    postResponses.Add(postResponse);
                }

                return postResponses;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }


        #endregion
    }


}

