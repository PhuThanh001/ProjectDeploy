using AutoMapper;
using GenZStyleApp.BAL.Helpers;
using GenZStyleApp.DAL.Models;
using GenZStyleAPP.BAL.DTOs.FireBase;
using GenZStyleAPP.BAL.DTOs.Posts;
using GenZStyleAPP.BAL.DTOs.Reports;
using GenZStyleAPP.BAL.Helpers;
using GenZStyleAPP.BAL.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;
using ProjectParticipantManagement.DAL.Infrastructures;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleAPP.BAL.Repository.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private UnitOfWork _unitOfWork;
        private IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public ReportRepository(IUnitOfWork unitOfWork, IMapper mapper, IServiceProvider serviceProvider)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        public List<GetReportResponse> GetPostReports()
        {
            try
            {
                List<Report> reports =  _unitOfWork.ReportDAO.GetAllRepostsByPost();

                // Lọc danh sách báo cáo theo bài đăng sử dụng LINQ


                // Sử dụng AutoMapper để ánh xạ danh sách báo cáo đã lọc sang các GetReportResponse tương ứng
                return this._mapper.Map<List<GetReportResponse>>(reports);

            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }

        /*public async Task<List<GetReportResponse>> GetUserReports()
        {
            try
            {
                List<Report> reports = await this._unitOfWork.ReportDAO.GetAllRepostsByUser();
                List<GetReportResponse> mappedUserReports = reports.Select(r => new GetReportResponse
                {
                    Id = r.Id,
                    AccuseeId = r.AccuseeId,
                    ReporterId = r.ReporterId,
                    PostId = r.PostId,
                    ReportName = r.ReportName,
                    Status = r.Status,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    IsStatusReport = r.IsStatusReport,
                    // Lấy thông tin tên người dùng và email từ tài khoản
                    UserName = r.Account.Username,
                    Email = r.Account.Email
                }).ToList();

                return mappedUserReports;


            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }*/
        public async Task<List<GetReportResponse>> GetUserReports()
        {
            try
            {
                List<Report> reports = await this._unitOfWork.ReportDAO.GetAllRepostsByUser();
                List<GetReportResponse> mappedUserReports = new List<GetReportResponse>();

                foreach (var report in reports)
                {
                    var accussee = await _unitOfWork.AccountDAO.GetAccountById(report.AccuseeId.Value);
                    var mappedReport = new GetReportResponse
                    {
                        Id = report.Id,
                        AccuseeId = report.AccuseeId,
                        ReporterId = report.ReporterId,
                        PostId = report.PostId,
                        ReportName = report.ReportName,
                        Status = report.Status,
                        Description = report.Description,
                        CreatedAt = report.CreatedAt,
                        IsStatusReport = report.IsStatusReport,
                        UserName = accussee.Username, // Lấy username của accusee
                        Email = accussee.Email
                    };
                    mappedUserReports.Add(mappedReport);
                }

                return mappedUserReports;
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        public async Task<List<GetReportResponse>> GetPostReportsByReportId(int postid)
        {
            try
            {


                // Lấy tất cả các báo cáo liên quan đến cùng một bài đăng
                List<Report> reports = await this._unitOfWork.ReportDAO.GetReportByPostIdAsync(postid);

                // Sử dụng AutoMapper để ánh xạ danh sách báo cáo sang các GetReportResponse tương ứng
                var mappedReports = this._mapper.Map<List<GetReportResponse>>(reports);

                return mappedReports;
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

        public async Task<List<GetReportResponse>> GetUserReportsByReportId(int AccusseId)
        {
            try
            {


                // Lấy tất cả các báo cáo liên quan đến cùng một người bị báo cáo
                List<Report> reports = await this._unitOfWork.ReportDAO.GetReportsByAccusseId(AccusseId);

                // Sử dụng AutoMapper để ánh xạ danh sách báo cáo sang các GetReportResponse tương ứng
                var mappedReports = this._mapper.Map<List<GetReportResponse>>(reports);

                return mappedReports;
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



        public async Task<GetReportResponse> GetActiveReportName(string reportname)
        {
            try
            {
                Report report = await this._unitOfWork.ReportDAO.GetReportByName(reportname);
                if (report == null)
                {
                    throw new NotFoundException("User does not exist in the system.");
                }
                return this._mapper.Map<GetReportResponse>(report);
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

        #region Tạo đơn tố cáo bài đăng 
        public async Task<GetReportResponse> CreateNewReportByPostIdAsync(AddReportRequest addReportRequest, HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);
                //var post = await _unitOfWork.PostDAO.GetPostByIdAsync(addReportRequest.PostId);
                //Report existingReport = await _unitOfWork.ReportDAO.GetReportByName(accountStaff.AccountId , addReportRequest);

                Report report;

                /*if (existingReport != null)
                {
                    report = existingReport;
                }*/
                
                    // Create a new report
                    report = new Report
                    {
                        AccuseeId = null, //chỉnh chỗ này 
                        PostId = addReportRequest.PostId,
                        ReporterId = accountStaff.AccountId,
                        ReportName = addReportRequest.ReportName,
                        Description = addReportRequest.Description,
                        //IsReport = false,
                        Account = accountStaff,
                        //Post = post,
                        CreatedAt = DateTime.UtcNow,
                        Status = "Watting",
                        IsStatusReport = 0,

                    };

                    await _unitOfWork.ReportDAO.AddNewReport(report);
                

                await _unitOfWork.CommitAsync();
                return this._mapper.Map<GetReportResponse>(report);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion

        #region Tạo đơn tố cáo người dùng 
        public async Task<GetReportResponse> CreateNewReportByReporterIdAsync(AddReporterRequest addReportRequest, HttpContext httpContext)
        {
            try
            {
                JwtSecurityToken jwtSecurityToken = TokenHelper.ReadToken(httpContext);
                string emailFromClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var accountStaff = await _unitOfWork.AccountDAO.GetAccountByEmail(emailFromClaim);

                //Report existingReport = await _unitOfWork.ReportDAO.GetReportByName(addReportRequest.ReportName);
                var accountreport = await _unitOfWork.AccountDAO.GetAccountByReporterId(addReportRequest.ReporterId);

                Report report;


                // Create a new report
                report = new Report
                {
                    ReporterId = accountStaff.AccountId,
                    PostId = null,
                    AccuseeId = addReportRequest.ReporterId,
                    ReportName = addReportRequest.ReportName,
                    Description = addReportRequest.Description,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Watting",
                    IsStatusReport = 0,
                    Account = accountStaff
                };

                await _unitOfWork.ReportDAO.AddNewReport(report);


                await _unitOfWork.CommitAsync();
                return this._mapper.Map<GetReportResponse>(report);
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion





        #region Xóa bài đăng sau khi duyệt 2 đơn tố cáo  
        public async Task<List<GetReportResponse>> BanReportAsync(int reportId, string status)
        {
            try
            {
                // Get the report by its Id
                Report report = await _unitOfWork.ReportDAO.GetReportByReportIdAsync(reportId);

                if (report == null)
                {
                    throw new NotFoundException("ReportId does not exist in the system.");
                }

                // Check if the report has IsStatusReport set to 0 (not accepted)
                if (report.IsStatusReport == 0)
                {
                    // Set IsStatusReport to 1 for the report (accepted)
                    report.IsStatusReport = 1;

                    // Update the status
                    if (status.ToLower() == "inactive")
                    {
                        report.Status = "InActive";
                        Notification notification = new Notification
                        {
                            //AccountId = report.ReporterId,
                            AccountId = report.Post.AccountId,
                            Message = "Bài đăng của bạn bị vi phạm bản quyền,hãy xem xét và chỉnh sửa nội dung phù hợp nếu có thêm 1 bản tố cáo về bài đăng của bạn thì bài đăng của bạn sẽ bị xóa khỏi hệ thống",
                            CreateAt = DateTime.UtcNow
                        };

                        // Lưu thông báo vào cơ sở dữ liệu
                        await _unitOfWork.NotificationDAO.AddNotiAsync(notification);
                        await _unitOfWork.CommitAsync();
                    }
                    else if (status.ToLower() == "reject")
                    {
                        report.Status = "Reject";
                        report.IsStatusReport = 2;
                    }

                    _unitOfWork.ReportDAO.AcceptReport(report);
                    await this._unitOfWork.CommitAsync();


                    // Check and delete post immediately if needed
                    await CheckAndDeletePost(report.PostId.Value);
                    
                    // Assuming you want to return a response for the report
                    return _mapper.Map<List<GetReportResponse>>(new List<Report> { report });
                }
                else
                {
                    throw new InvalidOperationException("The report is already banned.");
                }
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion

        #region Kiểm tra đơn tố cáo và xóa post
        private async Task CheckAndDeletePost(int? postId)
        {
            

                // Get the post by its Id with related reports
                //var commentDelete = await _unitOfWork.CommentDAO.GetCommentByPostIdAsync(postId.Value);
                var postDelete = await _unitOfWork.PostDAO.GetPostByIdAsync(postId.Value);
                var postReport = await _unitOfWork.ReportDAO.GetReportByPostIdAsync(postId.Value);

                if (postReport != null)
                {
                    // Đếm số lượng báo cáo có IsStatusReport bằng 1
                    var acceptedReportCount = postReport.Count();
                    if (acceptedReportCount >= 2)
                    {
                        //await _unitOfWork.CommentDAO.DeleteComment(commentDelete);
                        //await _unitOfWork.CommitAsync();
                        postDelete.Status = false;
                        await _unitOfWork.PostDAO.UpdatePost(postDelete);
                        // Save changes to the databasecc
                        await _unitOfWork.CommitAsync();
                    }
                }
            
        }
        #endregion
        #region Ban người dùng sau khi duyệt 2 đơn tố cáo trở lên
        public async Task<List<GetReportResponse>> BanUserAsync(int reportId, string status)
        {
            try
            {
                // Get the report by its Id
                Report report = await _unitOfWork.ReportDAO.GetReportByReportIdAsync(reportId);

                if (report == null)
                {
                    throw new NotFoundException("ReportId does not exist in the system.");
                }

                // Check if the report has IsStatusReport set to 0 (not accepted)
                if (report.IsStatusReport == 0)
                {
                    // Set IsStatusReport to 1 for the report (accepted)
                    report.IsStatusReport = 1;

                    // Update the status
                    if (status.ToLower() == "inactive")
                    {
                        report.Status = "InActive";
                        Notification notification = new Notification
                        {
                            AccountId = report.AccuseeId.Value,
                            Message = "Tài Khoản của bạn vi phạm,nếu bạn vi phạm lần nữa,tài khoản của bạn sẽ bị cấm hoạt động trong hệ thống",
                            CreateAt = DateTime.UtcNow
                        };

                        // Lưu thông báo vào cơ sở dữ liệu
                        await _unitOfWork.NotificationDAO.AddNotiAsync(notification);
                        await _unitOfWork.CommitAsync(); 
                    }
                    else if (status.ToLower() == "active")
                    {
                        report.Status = "Active";
                        report.IsStatusReport = 2;
                    }

                    _unitOfWork.ReportDAO.AcceptReport(report);

                    await this._unitOfWork.CommitAsync();
                    await BanAccount(report.AccuseeId);
                    //await this._unitOfWork.CommitAsync();

                    // Assuming you want to return a response for the report
                    return _mapper.Map<List<GetReportResponse>>(new List<Report> { report });


                }
                else
                {
                    throw new InvalidOperationException("The report is already banned.");
                }
            }
            catch (Exception ex)
            {
                string error = ErrorHelper.GetErrorString(ex.Message);
                throw new Exception(error);
            }
        }
        #endregion
        private async Task BanAccount(int? AccuseeId)
        {
            // Get the account to ban with its reports
            var account = await _unitOfWork.AccountDAO.GetAccountById(AccuseeId.Value);
            var reportban = await _unitOfWork.ReportDAO.GetReportsByAccusseId(AccuseeId.Value);

            if (reportban != null)
            {
                // Đếm số lượng báo cáo có IsStatusReport bằng 1
                var acceptedReportCount = reportban.Count();
                if (acceptedReportCount >= 2)
                {
                    // Ban the account
                    account.IsActive = false;
                    await _unitOfWork.AccountDAO.UpdateAccountProfile(account);
                    // Save changes to the database
                    await _unitOfWork.CommitAsync();
                }
            }
        }
    }
}


        




