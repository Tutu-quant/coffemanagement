using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.RestaurantTable;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class RestaurantTableService : IRestaurantTableService
    {
        private readonly IRestaurantTableRepository _repository;

        public RestaurantTableService(IRestaurantTableRepository repository)
        {
            _repository = repository;
        }

        public async Task<RestaurantTableDetailViewModel?> GetByIdAsync(int id)
        {
            var table = await _repository.GetByIdAsync(id);
            if (table == null) return null;

            return new RestaurantTableDetailViewModel
            {
                Id = table.TableID,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                TableStatus = table.TableStatus,
                Location = table.Location,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        public async Task<RestaurantTableListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountAsync();
            var skip = (pageNumber - 1) * pageSize;
            var tables = await _repository.SearchWithFilterAsync(string.Empty, null, null, "name_asc", skip, pageSize);
            var locations = await _repository.GetAllLocationsAsync();

            var tableVMs = tables.Select(t => new RestaurantTableViewModel
            {
                Id = t.TableID,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                TableStatus = t.TableStatus,
                Location = t.Location,
                CreatedAt = t.CreatedAt
            }).ToList();

            // Get statistics
            var allTables = await _repository.GetAllAsync();
            var availableTables = await _repository.GetCountByStatusAsync("Available");
            var occupiedTables = await _repository.GetCountByStatusAsync("Occupied");
            var waitingPaymentTables = await _repository.GetCountByStatusAsync("WaitingPayment");
            var maintenanceTables = await _repository.GetCountByStatusAsync("Maintenance");

            return new RestaurantTableListViewModel
            {
                Tables = tableVMs,
                Locations = locations,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalTables = allTables.Count,
                AvailableTables = availableTables,
                OccupiedTables = occupiedTables,
                WaitingPaymentTables = waitingPaymentTables,
                MaintenanceTables = maintenanceTables
            };
        }

        public async Task<RestaurantTableListViewModel> SearchWithFilterAsync(string searchTerm, string? location, string? status, string sortBy, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountByFilterAsync(searchTerm, location, status);
            var skip = (pageNumber - 1) * pageSize;
            var tables = await _repository.SearchWithFilterAsync(searchTerm, location, status, sortBy, skip, pageSize);
            var locations = await _repository.GetAllLocationsAsync();

            var tableVMs = tables.Select(t => new RestaurantTableViewModel
            {
                Id = t.TableID,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                TableStatus = t.TableStatus,
                Location = t.Location,
                CreatedAt = t.CreatedAt
            }).ToList();

            // Get statistics
            var allTables = await _repository.GetAllAsync();
            var availableTables = await _repository.GetCountByStatusAsync("Available");
            var occupiedTables = await _repository.GetCountByStatusAsync("Occupied");
            var waitingPaymentTables = await _repository.GetCountByStatusAsync("WaitingPayment");
            var maintenanceTables = await _repository.GetCountByStatusAsync("Maintenance");

            return new RestaurantTableListViewModel
            {
                Tables = tableVMs,
                Locations = locations,
                SearchTerm = searchTerm,
                SelectedLocation = location,
                SelectedStatus = status,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalTables = allTables.Count,
                AvailableTables = availableTables,
                OccupiedTables = occupiedTables,
                WaitingPaymentTables = waitingPaymentTables,
                MaintenanceTables = maintenanceTables
            };
        }

        public async Task<int> CreateAsync(RestaurantTableCreateViewModel model)
        {
            var table = new RestaurantTable
            {
                TableNumber = model.TableNumber,
                Capacity = model.Capacity,
                Location = model.Location,
                TableStatus = "Available"
            };

            await _repository.AddAsync(table);
            return table.TableID;
        }

        public async Task UpdateAsync(RestaurantTableEditViewModel model)
        {
            var table = await _repository.GetByIdAsync(model.Id);
            if (table != null)
            {
                table.TableNumber = model.TableNumber;
                table.Capacity = model.Capacity;
                table.Location = model.Location;
                table.TableStatus = model.TableStatus;

                await _repository.UpdateAsync(table);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var canDelete = await _repository.CanDeleteAsync(id);
            if (!canDelete)
            {
                throw new InvalidOperationException("Không thể xóa bàn này vì đang có đơn hàng hoặc đặt bàn chưa hoàn thành.");
            }

            await _repository.DeleteAsync(id);
        }

        public async Task<bool> ValidateTableNumberAsync(string tableNumber, int? excludeId = null)
        {
            return !await _repository.ExistsByTableNumberAsync(tableNumber, excludeId);
        }

        public async Task<List<string>> GetAllLocationsAsync()
        {
            return await _repository.GetAllLocationsAsync();
        }

        public async Task<Dictionary<string, int>> GetTableStatisticsAsync()
        {
            var stats = new Dictionary<string, int>
            {
                { "Total", await _repository.GetCountAsync() },
                { "Available", await _repository.GetCountByStatusAsync("Available") },
                { "Occupied", await _repository.GetCountByStatusAsync("Occupied") },
                { "WaitingPayment", await _repository.GetCountByStatusAsync("WaitingPayment") },
                { "Maintenance", await _repository.GetCountByStatusAsync("Maintenance") }
            };

            return stats;
        }
    }
}
