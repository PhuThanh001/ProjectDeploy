﻿using GenZStyleApp.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleApp.DAL.DAO
{
    public class StyleDAO
    {
        private GenZStyleDbContext _dbContext;
        public StyleDAO(GenZStyleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        // Add new Post
        public async Task AddNewStyle(Style style)
        {
            try
            {
                await _dbContext.Styles.AddAsync(style);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Style>> FindStyleByPostId(int id)
        {
            try
            {
                return await _dbContext.Styles
                .Where(s => s.StylePosts.Any(sp => sp.PostId == id))
                .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<Dictionary<string, List<Style>>> GetGroupedStylesWithNullDescription()
        {
            try
            {
                // Lấy danh sách các Style có Description là null
                var stylesWithNullDescription = await _dbContext.Styles
                    .Where(s => s.Description == null)
                    .ToListAsync();

                // Nhóm các Style theo StyleName
                var groupedStyles = stylesWithNullDescription
                    .GroupBy(s => s.StyleName)
                    .ToDictionary(g => g.Key, g => g.ToList());

                return groupedStyles;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #region DeleteStylePost
        public async Task DeleteStyle(Style style)
        {
            try
            {
                this._dbContext.Styles.Remove(style);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}
