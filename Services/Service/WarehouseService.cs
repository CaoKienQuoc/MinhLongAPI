using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly ILocationRepository _locationRepository;

        public WarehouseService(IWarehouseRepository warehouseRepo, ILocationRepository locationRepository)
        {
            _warehouseRepo = warehouseRepo;
            _locationRepository = locationRepository;
        }

        public IEnumerable<WarehouseInfoDto> GetAllWarehouseInfo()
        {
            var warehouses = _warehouseRepo.GetAllWarehouses();

            return warehouses.Select(w => new WarehouseInfoDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                FullAddress = $"{w.Address.Street}, {w.Address.Ward.WardName}, {w.Address.District.DistrictName}, {w.Address.Province.ProvinceName}"
            }).ToList();
        }

        public Warehouse GetWarehouseByUserId(Guid userId) => _warehouseRepo.GetWarehouseByUserId(userId);

        public void CreateWarehouse(Guid userId, string warehousName, string street, string province, string district, string ward, string note)
        {
            // Kiểm tra xem User đã có Warehouse chưa
            if (_warehouseRepo.GetWarehouseByUserId(userId) != null)
                throw new Exception("Mỗi nhân viên chỉ được phép tạo một kho hàng!");

            // Lấy ID của Province
            var provinceId = _locationRepository.GetProvinceIdByName(province);
            if (provinceId == null) throw new Exception("Tỉnh không hợp lệ!");

            // Lấy ID của District
            var districtId = _locationRepository.GetDistrictIdByName(district, provinceId.Value);
            if (districtId == null) throw new Exception("Quận/Huyện không hợp lệ!");

            // Lấy ID của Ward
            var wardId = _locationRepository.GetWardIdByName(ward, districtId.Value);
            if (wardId == null) throw new Exception("Phường/Xã không hợp lệ!");

            // Tạo Address
            var address = new Address
            {
                Street = street,
                ProvinceId = provinceId.Value,
                DistrictId = districtId.Value,
                WardId = wardId.Value
            };

            _locationRepository.AddAddress(address);

            // Tạo Warehouse
            var warehouse = new Warehouse
            {
                WarehouseName = warehousName,
                UserId = userId,
                AddressId = address.AddressId,
                Note = note
            };

            _warehouseRepo.AddWarehouse(warehouse);

        }

        public void UpdateWarehouse(Guid userId, int warehouseId, string warehousName, string street, string province, string district, string ward, string note)
        {
            // Tìm Warehouse của User
            var warehouse = _warehouseRepo.GetWarehouseById(warehouseId);
            if (warehouse == null)
                throw new Exception("Kho không tồn tại!");

            if (warehouse.UserId != userId)
                throw new Exception("Bạn không có quyền cập nhập kho này!");

            // Tìm Address liên kết với Warehouse
            var address = _locationRepository.GetAddressById(warehouse.AddressId);  // ✅ Đảm bảo gọi từ đúng Interface
            if (address == null)
                throw new Exception("Địa chỉ của kho không tồn tại");

            // Cập nhật Address
            var provinceId = _locationRepository.GetProvinceIdByName(province);
            if (provinceId == null) throw new Exception("Tỉnh không hợp lệ!");

            var districtId = _locationRepository.GetDistrictIdByName(district, provinceId.Value);
            if (districtId == null) throw new Exception("Quận/Huyện không hợp lệ!");

            var wardId = _locationRepository.GetWardIdByName(ward, districtId.Value);
            if (wardId == null) throw new Exception("Phường/Xã không hợp lệ!");

            address.Street = street;
            address.ProvinceId = provinceId.Value;
            address.DistrictId = districtId.Value;
            address.WardId = wardId.Value;

            _locationRepository.UpdateAddress(address);

            // Cập nhật Warehouse
            warehouse.WarehouseName = warehousName;
            warehouse.Note = note;
            _warehouseRepo.UpdateWarehouse(warehouse);
        }


        public async Task<bool> DeleteWarehouseAsync(int warehouseId)
        {
            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(warehouseId);
            if (warehouse == null)
            {
                return false; // Không tìm thấy Warehouse để xóa
            }

            await _warehouseRepo.DeleteWarehouseAsync(warehouse);
            return true;
        }

        public async Task<IEnumerable<WarehouseProductDto>> GetProductsByWarehouseIdAsync(long warehouseId, string sortBy = null)
        {
            return await _warehouseRepo.GetProductsByWarehouseIdAsync(warehouseId, sortBy);
        }

        public async Task<WarehouseProductDto> GetProductByIdAsync(long warehouseProductId)
        {
            return await _warehouseRepo.GetProductByIdAsync(warehouseProductId);
        }

        public async Task<List<WarehouseProductSummaryDto>> GetProductSummariesByWarehouseIdAsync(long warehouseId)
        {
            return await _warehouseRepo.GetProductSummariesByWarehouseIdAsync(warehouseId);
        }

        public async Task<List<ProductWarehouseSummaryDto>> GetWarehousesByProductIdAsync(long productId)
        {
            return await _warehouseRepo.GetWarehousesByProductIdAsync(productId);
        }

    }
}
