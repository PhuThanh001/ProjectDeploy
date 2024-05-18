using AutoMapper;
using GenZStyleAPP.BAL.DTOs.Accounts;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.Users;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleAPP.BAL.Repository.Interfaces;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using ProjectParticipantManagement.DAL.Infrastructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenZStyleAPP.BAL.DTOs.HashTags;
using GenZStyleApp.BAL.Helpers;
using GenZStyleAPP.BAL.DTOs.Posts;
using GenZStyleAPP.BAL.DTOs.PostLike;
using GenZStyleAPP.BAL.DTOs.HashPosts;

namespace GenZStyleAPP.BAL.Repository.Implementations
{
    public class HashTagRepository : IHashTagRepository
    {
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;
        public HashTagRepository(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<GetPostForSearch>> SearchByHashTagName(string hashtag)
        {
            try
            {
                var hashtags = await _unitOfWork.HashTagDAO.SearchByHashTagName(hashtag);
                var result = new List<GetPostForSearch>();

                foreach (var post in hashtags)
                {
                    var hashtagss = post.HashPosts.Select(hp => hp.Hashtag.Name).ToList();

                    var postForSearch = new GetPostForSearch
                    {
                        PostId = post.PostId,
                        AccountId = post.AccountId,
                        CreateTime = post.CreateTime,
                        UpdateTime = post.UpdateTime,
                        Content = post.Content,
                        Image = post.Image,
                        Status = post.Status,
                        Username = post.Account != null ? post.Account.Username : "N/A",
                        //HashPosts = _mapper.Map<ICollection<GetHashPostsResponse>>(post.HashPosts),
                        Likes = post.Likes.Where(like => like.isLike == true).Select(l => new GetPostLikeCollection
                        {
                            PostId = l.PostId,
                            LikeBy = l.LikeBy,
                            isLike = l.isLike,
                            // Map thông tin của người dùng từ đối tượng Account vào thuộc tính Account của GetPostLikeCollection
                            Account = _mapper.Map<GetAccountByLikeResponse>(l.Account)
                        }).ToList(),
                        Hashtags = hashtagss
                    };

                    result.Add(postForSearch);
                }

                return result;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region GetHashTag
        public async Task<List<GetAllHashTag>> GetHashTagsAsync()
        {
            try
            {
                var hashtags = await _unitOfWork.HashTagDAO.GetAllHashTag();
                var result = new List<GetAllHashTag>();

                foreach (var hashtag in hashtags)
                {
                    var getAllHashTag = new GetAllHashTag
                    {
                        id = hashtag.id,
                        Name = hashtag.Name,
                        Image = hashtag.Image,
                        CreationDate = hashtag.CreationDate,
                        HashPosts = new List<GetHashPostByHashtag>()
                    };

                    foreach (var hashPost in hashtag.HashPosts)
                    {
                        var post = hashPost.Post;

                        var hashPostByHashtag = new GetHashPostByHashtag
                        {
                            PostId = post.PostId,
                            HashTageId = hashPost.HashTageId,
                            CreateAt = hashPost.CreateAt,
                            UpdateAt = hashPost.UpdateAt,
                            Post = new GetCollectionPostResponse
                            {
                                PostId = post.PostId,
                                AccountId = post.AccountId,
                                CreateTime = post.CreateTime,
                                UpdateTime = post.UpdateTime,
                                Content = post.Content,
                                Image = post.Image,
                                Status = post.Status,
                                //Comments = post.Comments,
                                Likes = post.Likes.Where(like => like.isLike == true).Select(l => new GetPostLikeCollection
                                {
                                    PostId = l.PostId,
                                    LikeBy = l.LikeBy,
                                    isLike = l.isLike,
                                    // Map thông tin của người dùng từ đối tượng Account vào thuộc tính Account của GetPostLikeCollection
                                    Account = _mapper.Map<GetAccountByLikeResponse>(l.Account)
                                }).ToList(),
                                // Lấy danh sách hashtag trong bài đăng
                                Hashtags = post.HashPosts.Select(hp => hp.Hashtag.Name).ToList()
                            }
                        };

                        getAllHashTag.HashPosts.Add(hashPostByHashtag);
                    }

                    result.Add(getAllHashTag);
                }

                return result;
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
        #endregion

        #region GetHashTag
        public async Task<GetAllHashTag> GetHashTagsByIdAsync(int id)
        {
            try
            {
                var hashtag = await _unitOfWork.HashTagDAO.GetHashtagByIdAsync(id);

                // Kiểm tra xem hashtag có tồn tại không
                if (hashtag != null)
                {
                    // Tạo đối tượng GetAllHashTag từ hashtag cụ thể
                    var getAllHashTag = new GetAllHashTag
                    {
                        id = hashtag.id,
                        Name = hashtag.Name,
                        Image = hashtag.Image,
                        CreationDate = hashtag.CreationDate,
                        HashPosts = new List<GetHashPostByHashtag>()
                    };

                    // Lặp qua các bài đăng liên quan đến hashtag và thêm chúng vào danh sách HashPosts
                    foreach (var hashPost in hashtag.HashPosts)
                    {
                        var post = hashPost.Post;

                        var hashPostByHashtag = new GetHashPostByHashtag
                        {
                            PostId = post.PostId,
                            HashTageId = hashPost.HashTageId,
                            CreateAt = hashPost.CreateAt,
                            UpdateAt = hashPost.UpdateAt,
                            Post = new GetCollectionPostResponse
                            {
                                PostId = post.PostId,
                                AccountId = post.AccountId,
                                CreateTime = post.CreateTime,
                                UpdateTime = post.UpdateTime,
                                Content = post.Content,
                                Image = post.Image,
                                Status = post.Status,
                                Likes = post.Likes.Where(like => like.isLike == true).Select(l => new GetPostLikeCollection
                                {
                                    PostId = l.PostId,
                                    LikeBy = l.LikeBy,
                                    isLike = l.isLike,
                                    Account = _mapper.Map<GetAccountByLikeResponse>(l.Account)
                                }).ToList(),
                                Hashtags = post.HashPosts.Select(hp => hp.Hashtag.Name).ToList()
                            }
                        };

                        getAllHashTag.HashPosts.Add(hashPostByHashtag);
                    }

                    return getAllHashTag;
                }
                else
                {
                    // Nếu không tìm thấy hashtag, trả về null
                    return null;
                }
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




        #endregion         
        /*public async Task<GetHashTagResponse> GetHashTagByName(GetHashTagRequest hashTagRequest)
        {
            try
            {


                List<GetHashTagReponse> hashtagDTOs = _mapper.Map<List<GetHashTagReponse>>(hashtags);
            }catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }*/
        #region Add HashTag
        public async Task<GetHashTagResponse> AddNewHashTag(FireBaseImage fireBaseImage, GetHashTagRequest hashTagRequest)
        {
            try
            {
                var post = await _unitOfWork.PostDAO.GetPosts();
                var HashtagByName = await _unitOfWork.HashTagDAO.GetHashTagByNameAsync(hashTagRequest.Name);
                if (HashtagByName != null) 
                {
                    throw new BadRequestException("HashTag already exist in the system.");
                }
                
                
                Hashtag hashtag = new Hashtag
                {
                    Name = hashTagRequest.Name,
                    CreationDate = hashTagRequest.CreationDate,
                    
                };
                
                
                // Upload image to firebase
                FileHelper.SetCredentials(fireBaseImage);
                FileStream fileStream = FileHelper.ConvertFormFileToStream(hashTagRequest.Image);
                Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "HashTag");
                hashtag.Image = result.Item1;

                
                /*Post MathchingPost = null;                 
                foreach (var postById in post)
                {
                    if (postById.PostId == 1)
                    {
                        MathchingPost = postById;
                        break;
                    }

                    
                }*/

                //Gắn hashtag vào bài post
                /*HashPost hashPost = new HashPost
                {
                    PostId = MathchingPost.PostId,
                    HashTageId = hashtag.id,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    Post = MathchingPost,
                    Hashtag = hashtag


                };
                hashtag.HashPosts.Add(hashPost);*/
                await _unitOfWork.HashTagDAO.CreateHashTagAsync(hashtag);
                await this._unitOfWork.CommitAsync();//lưu data
                return this._mapper.Map<GetHashTagResponse>(hashtag);

            }
            catch (BadRequestException ex)
            {
                string fieldNameError = "";
                if (ex.Message.ToLower().Contains("name"))
                {
                    fieldNameError = "Name";
                }
                string error = ErrorHelper.GetErrorString(fieldNameError, ex.Message);
                throw new BadRequestException(error);
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion
    }
}
