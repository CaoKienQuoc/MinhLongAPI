using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Microsoft.AspNetCore.Http;

namespace Services.IService
{
    public interface IContractService
    {
        Task<List<Contract>> UploadContractsAsync(List<IFormFile> files, long agencyId);

        Task<List<Contract>> UploadContractsAsync(List<IFormFile> files);
    }
}
