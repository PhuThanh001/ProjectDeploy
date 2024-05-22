using GenZStyleApp.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleApp.DAL.DAO
{
    public class PostDAO
    {
        private GenZStyleDbContext _dbContext;
        public PostDAO(GenZStyleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        //get posts
        public async Task<List<Post>> GetPosts()
        {
            try
            {
                
                List<Post> posts = await _dbContext.Posts
                    .AsNoTracking()
                    .OrderByDescending( n => n.CreateTime)
                    .Include(x => x.HashPosts).ThenInclude(u => u.Hashtag)
                    .ToListAsync();
                return posts;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region Get post by id
        public async Task<Post> GetPostByIdAsync(int id)
        {
            try
            {
                return await _dbContext.Posts.Include(a => a.Account)
                                             .ThenInclude(b => b.User)
                                             .Include(c => c.StylePosts).ThenInclude(i => i.Style)
                    .SingleOrDefaultAsync(p => p.PostId == id);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        public async Task<List<string>> GetStyleNameByPostIdAsync(int postId)
        {
            try
            {
                // Thực hiện truy vấn để lấy StyleName từ cơ sở dữ liệu dựa trên postId
                var styleName = await _dbContext.StylePosts
                    .Include(sp => sp.Style)
                    .Where(sp => sp.PostId == postId)
                    .Select(sp => sp.Style.StyleName)
                    .ToListAsync();

                // Trả về StyleName
                return styleName;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #region Get post by accountid
        public async Task<List<Post>> GetPostByAccountIdAsync(int id)
        {
            try
            {
                return await _dbContext.Posts.Include(p => p.Account)
                                             .Include(p => p.HashPosts).ThenInclude(h => h.Hashtag)
                                             .Include(p => p.Likes)
                                             .Include(c => c.StylePosts).ThenInclude(i => i.Style)
                                             .OrderByDescending(n => n.CreateTime)
                                             .Where(p => p.AccountId == id && p.Status == true)
                                             .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region Get post by accountid
        public async Task<List<Post>> GetPostByReportAsync(int id)
        {
            try
            {
                // Lấy các bài đăng mà các báo cáo liên quan có IsStatusReport được đặt thành true và có ít nhất 2 báo cáo
                return await _dbContext.Posts
                    .Where(p => p.PostId == id && p.Reports.Count(r => r.IsStatusReport == 1) >= 2)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region Get Post by Gender
        public async Task<List<Post>> GetPostByGenderAsync(bool gender)
        {
            try
            {
                return await _dbContext.Posts
                                             
                                             .Include(c => c.Account.User)  // Thêm dòng này để include User
                                             .Where(c => c.Account.User.Gender == gender)
                                             .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        #region Get List Post by StyleName
        public async Task<List<Post>> GetListPostByStyleName(string? stylename)
        {
            IQueryable<Post> query = _dbContext.Posts
                .AsNoTracking()//thêm chỗ này 
                .Include(e => e.Likes).ThenInclude(l => l.Account).ThenInclude(a => a.User)
                .Include(i => i.Account).ThenInclude(a => a.User)
                .Include(u => u.HashPosts).ThenInclude(x => x.Hashtag)
                .Include(p => p.StylePosts)
                .Where(p => p.Status == true);

            // Nếu stylename không được nhập, trả về tất cả các bài đăng
            if (string.IsNullOrEmpty(stylename))
            {
                return await query.OrderByDescending(n => n.CreateTime).ToListAsync();
            }
            else
            {
                return await query.Where(p => p.StylePosts.Any(sp => sp.Style.StyleName == stylename))
                                 .OrderByDescending(n => n.CreateTime)
                                 .ToListAsync();
            }
        }
        #endregion
        // Add new Post
        public async Task AddNewPost(Post post)
        {
            try
            {
                await _dbContext.Posts.AddAsync(post);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region Update post
        public async Task UpdatePost(Post post)
        {
            try
            {
                 _dbContext.Entry<Post>(post).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        public async Task<List<Post>> GetActivePosts()
        {
            try
            {
                List<Post> posts = await _dbContext.Posts
                    .AsNoTracking()
                    .Include(e => e.Likes).ThenInclude(l => l.Account).ThenInclude(a => a.User)
                    .Include(i => i.Account)
                    .ThenInclude(a => a.User) // Include User within Account
                    .Include(u => u.HashPosts).ThenInclude(x => x.Hashtag)
                    .Where( p => p.Status == true)
                    .OrderByDescending(n => n.CreateTime)
                    .ToListAsync();

                return posts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public List<Post> GetActivePostss()
        {
            try
            {
                List<Post> posts = _dbContext.Posts
                    .AsNoTracking()
                    .Include(e => e.Likes)
                    .Include(i => i.Account)
                    .ThenInclude(a => a.User) // Include User within Account
                    .Include(u => u.HashPosts).ThenInclude(x => x.Hashtag)
                    .OrderByDescending(n => n.CreateTime)
                    .ToList();

                return posts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public void UpdatePostComment(Post post)
        {
            try
            {
                this._dbContext.Entry<Post>(post).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region DeletePost
        public async Task DeletePost(Post post)
        {
            try
            {
                this._dbContext.Posts.Remove(post);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        // List các bài post có nhiều lượt like 
        public async Task<List<Post>> GetTopActivePostsOrderedByLikes()
        {
            try
            {
                return await _dbContext.Posts
                    .AsNoTracking()
                    .Include(e => e.Likes).ThenInclude(l => l.Account).ThenInclude(a => a.User)
                    .Include(i => i.Account).ThenInclude(a => a.User)
                    .Include(u => u.HashPosts).ThenInclude(x => x.Hashtag)
                    .Where(p => p.Status == true)
                    .OrderByDescending(p => p.Likes.Count(l => l.isLike))
                    .Take(5) // Chỉ lấy top 5 bài post
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
