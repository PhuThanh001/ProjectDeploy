using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.Accounts;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.Posts;
using GenZStyleAPP.BAL.Errors;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.Repository.Interfaces
{
    public interface IPostRepository
    {
        public Task<List<GetPostResponse>> GetPostsAsync(HttpContext httpContext);
        public Task<List<GetPostResponse>> GetActivePosts();
        public List<GetPostResponse> GetActivePostss();
        public Task<GetPostResponse> GetPostDetailByIdAsync(int id);
        public Task<List<GetPostResponse>> GetPostByAccountIdAsync(int id);
        
        public Task<List<GetPostSuggestion>> GetPostByAccountIdFollowAsync(int id);
        public Task<List<GetPostResponse>> GetPostByGenderAsync(bool gender);
        public Task<CommonResponse> CreateNewPostAsync(AddPostRequest addPostRequest, FireBaseImage fireBaseImage, HttpContext httpContext);
        public Task<CommonResponse> UpdatePostProfileByPostIdAsync(int postId,
                                                                                     FireBaseImage fireBaseImage,
                                                                                     UpdatePostRequest updatePostRequest,HttpContext httpContext);
        public Task<List<GetPostSuggestion>> GetPostByUserFollowId(HttpContext httpContext);
        public Task<GetPostResponse> BanPostAsync(int postId, HttpContext httpContext);
        public Task<List<GetPostResponse>> GetListPostStyleName(string? stylename);
        public Task DeletePostById(int id);
    }
}
