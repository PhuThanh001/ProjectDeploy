using GenZStyleApp.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleApp.DAL.DAO
{
    public class StylePostDAO
    {
        private GenZStyleDbContext _dbContext;
        public StylePostDAO(GenZStyleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        // Add new Post
        public async Task AddNewStylePost(StylePost stylepost)
        {
            try
            {
                await _dbContext.StylePosts.AddAsync(stylepost);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<StylePost>> GetStylePostByPostIdAsync(int id)
        {
            try
            {
                return await _dbContext.StylePosts.Where(p => p.PostId == id)
                                                 .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<int>> GetPostIdsByStyleIds(List<int> styleIds)
        {
            return await _dbContext.StylePosts
                .Where(sp => styleIds.Contains(sp.StyleId))
                .Select(sp => sp.PostId)
                .Distinct()
                .ToListAsync();
        }
        #region DeleteStylePost
        public async Task DeleteStylePost(StylePost stylePost)
        {
            try
            {
                this._dbContext.StylePosts.Remove(stylePost);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}
