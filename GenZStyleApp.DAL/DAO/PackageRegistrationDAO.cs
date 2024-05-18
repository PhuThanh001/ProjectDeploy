using GenZStyleApp.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenZStyleApp.DAL.DAO
{
    public class PackageRegistrationDAO
    {
        private GenZStyleDbContext _dbContext;
        public PackageRegistrationDAO(GenZStyleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }


        public async Task AddNewPackageRegistration(PackageRegistration packageRegistration)
        {
            try
            {
                await _dbContext.packageRegistrations.AddAsync(packageRegistration);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<PackageRegistration>> GetPackageRegisterByIdAsync(int accountId)
        {
            try
            {
                return await _dbContext.packageRegistrations.Where(co => co.AccountId == accountId).ToListAsync();


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task DeletePackageRegisById(PackageRegistration packageRegistration)
        {
            try
            {
                this._dbContext.packageRegistrations.Remove(packageRegistration);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
    }
}
