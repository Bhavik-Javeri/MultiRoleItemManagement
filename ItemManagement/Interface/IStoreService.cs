using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ItemManagement.Interface
{
    public interface IStoreService
    {
        Task<IActionResult> GetStoresAsync(ClaimsPrincipal userClaims);
        Task<IActionResult> AddStoreAsync(StoreDto storeDto);
        Task<IActionResult> GetStoreByIdAsync(Guid storeId, ClaimsPrincipal userClaims);
        Task<IActionResult> UpdateStoreAsync(Guid storeId, StoreDto storeDto, ClaimsPrincipal userClaims);
        Task<IActionResult> DeleteStoreAsync(Guid storeId, ClaimsPrincipal userClaims);

    }
}