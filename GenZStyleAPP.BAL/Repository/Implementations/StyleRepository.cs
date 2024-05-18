using AutoMapper;
using GenZStyleApp.BAL.Helpers;
using GenZStyleApp.DAL;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.Collections;
using GenZStyleAPP.BAL.DTOs.Comments;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.Styles;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleAPP.BAL.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
    public class StyleRepository : IStyleRepository
    {
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;

        public StyleRepository(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
        }


        #region GetCollectionByCollectionId
        public async Task CreateStyleAsync(int accountId, GetStyleRequest styleRequest)
        {            
            try
            {
                var account = await _unitOfWork.AccountDAO.GetAccountById(accountId);
                // Tách chuỗi description thành các phần, sử dụng dấu phẩy làm dấu phân cách
                string[] parts = styleRequest.Description.Split(',');

                // Lấy phần tử đầu tiên
                string firstPart = parts.FirstOrDefault()?.Trim();
                
                Style style = new Style
                {   
                    CategoryId = 1,
                    StyleName = firstPart, // Gán phần tử đầu tiên vào StyleName
                    Description = styleRequest.Description,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                };

                     
                await _unitOfWork.StyleDAO.AddNewStyle(style);
                account.Style = style;
                account.StyleId = style.StyleId;
                await _unitOfWork.AccountDAO.UpdateAccountProfile(account);
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
        #endregion

        #region GetCollectionByCollectionId
        public async Task CreateStylePhoidoAsync(HttpContext httpContext, FireBaseImage fireBaseImage, GetStyleRequest styleRequest)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var account = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                // Tách chuỗi description thành các phần, sử dụng dấu phẩy làm dấu phân cách
                string[] parts = styleRequest.Description.Split(',');

                // Lấy phần tử đầu tiên
                string firstPart = parts.FirstOrDefault()?.Trim();
                #region Upload video to firebase

                //foreach (var imageFile in addPostRequest.Image)
                //{
                FileHelper.SetCredentials(fireBaseImage);
                FileStream fileStream = FileHelper.ConvertFormFileToStream(styleRequest.Video);
                Tuple<string, string> result = await FileHelper.UploadImage(fileStream, "Post");

                // Assuming you want to store multiple image URLs
                // Consider creating a separate entity for images if necessary
                // and establish a one-to-many relationship with the Post entity
                // For now, appending URLs to the Image property
                // Separate URLs by a delimiter
                                           //}
                                           // Kiểm tra hình ảnh có chứa nội dung khiêu dâm không
                                           //Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           //ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                                           //GoogleCredential credential = GoogleCredential.FromFile("D:\\aa\\starlit-casing-420509-1e405b20afea.json");
                                           // Tạo đối tượng ImageAnnotatorClient với thông tin xác thực

                //Style style = new Style();
                #endregion
                Style style = new Style
                {
                    CategoryId = 1,
                    StyleName = styleRequest.SyleName, // Gán phần tử đầu tiên vào StyleName
                    //Description = styleRequest.Description,
                    //videoPath = result.Item1,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                   
                };
                style.Accounts.Add(account);
                await _unitOfWork.StyleDAO.AddNewStyle(style);
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
        #endregion
    }
}
