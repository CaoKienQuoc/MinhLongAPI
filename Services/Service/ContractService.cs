using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Repo.IRepository;
using Services.IService;
using BusinessObject.Models;

namespace Services.Service
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepo;
        private readonly Cloudinary _cloudinary;

        public ContractService(IContractRepository contractRepo, IConfiguration config)
        {
            _contractRepo = contractRepo;

            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<List<Contract>> UploadContractsAsync(List<IFormFile> files, long agencyId)
        {
            var uploadedContracts = new List<Contract>();

            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Upload thất bại: {uploadResult.Error?.Message}");
                }

                uploadedContracts.Add(new Contract
                {
                    AgencyId = agencyId,
                    FileName = file.FileName,
                    FilePath = uploadResult.SecureUrl.ToString(),
                    FileType = Path.GetExtension(file.FileName),
                    CreatedAt = DateTime.Now
                });
            }

            return await _contractRepo.AddRangeAsync(uploadedContracts);
        }

        public async Task<List<Contract>> UploadContractsAsync(List<IFormFile> files)
        {
            var uploadedContracts = new List<Contract>();

            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"Upload thất bại: {uploadResult.Error?.Message}");

                uploadedContracts.Add(new Contract
                {
                    FileName = file.FileName,
                    FilePath = uploadResult.SecureUrl.ToString(),
                    FileType = Path.GetExtension(file.FileName)
                });
            }

            return uploadedContracts;
        }

    }
}
