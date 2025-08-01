﻿using ItemManagement.Model; // Ensure this points to your database entity 'Store'
using System;
using System.Collections.Generic; // For List<StoreDto>
using System.Linq; // For LINQ operations

namespace ItemManagement.Model.DTO
{
    public class StoreDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Initialize to avoid null reference warnings
        public string? Address { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public int CityId { get; set; }
        public string? ContactNumber { get; set; } // Added
        public string? Email { get; set; } // Added
        public TimeSpan OpeningHours { get; set; } // Added
        public TimeSpan ClosingHours { get; set; } // Added
        public string? ImageUrl { get; set; } // Changed from 'Image' for consistency
        public bool IsActive { get; set; } // Added
        public DateTime CreateDate { get; set; } // Added
        public DateTime? UpdateDate { get; set; } // Added
        public int UserCount { get; set; } // Added (for display purposes, usually populated later)

        // --- Mapping from Store Entity to StoreDto ---
        public static StoreDto Mapping(Store store)
        {
            if (store == null)
            {
                return null; // Handle null input gracefully
            }

            return new StoreDto
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address, // Mapped
                CountryId = store.CountryId,
                StateId = store.StateId,
                CityId = store.CityId,
                ContactNumber = store.ContactNumber, // Mapped
                Email = store.Email, // Mapped
                OpeningHours = store.OpeningHours, // Mapped
                ClosingHours = store.ClosingHours, // Mapped
                ImageUrl = store.Image, // Assuming 'Image' in Store entity maps to 'ImageUrl' in DTO/ViewModel
                IsActive = store.IsActive, // Mapped
                CreateDate = store.CreateDate, // Mapped
                UpdateDate = store.UpdateDate, // Mapped
                UserCount = store.Users?.Count ?? 0 // Calculate UserCount from navigation property, default to 0
            };
        }

        // --- Mapping from List<Store> to List<StoreDto> ---
        public static List<StoreDto> Mapping(List<Store> stores)
        {
            return stores?.Select(s => Mapping(s)).ToList() ?? new List<StoreDto>(); // Use LINQ for cleaner mapping
        }

        // --- Mapping from StoreDto to Store Entity (for saving/updating) ---
        public static Store Mapping(StoreDto storeDto)
        {
            if (storeDto == null)
            {
                return null; // Handle null input gracefully
            }

            return new Store
            {
                // Id is often generated by DB or set on existing entity, but include for completeness
                Id = storeDto.Id,
                Name = storeDto.Name,
                Address = storeDto.Address,
                CountryId = storeDto.CountryId,
                StateId = storeDto.StateId,
                CityId = storeDto.CityId,
                ContactNumber = storeDto.ContactNumber,
                Email = storeDto.Email,
                OpeningHours = storeDto.OpeningHours,
                ClosingHours = storeDto.ClosingHours,
                Image = storeDto.ImageUrl, // Map back to 'Image' property in Store entity
                IsActive = storeDto.IsActive,
                // CreateDate and UpdateDate are typically managed by the database or service layer on save
                // For a new entity, CreateDate will be set by its default value or during SaveChanges
                // UpdateDate will be set on update operations.
                CreateDate = storeDto.CreateDate != default ? storeDto.CreateDate : DateTime.UtcNow, // Use provided date or set new
                UpdateDate = storeDto.UpdateDate // Allow null if not updated
            };
        }

        // --- Mapping from List<StoreDto> to List<Store> ---
        public static List<Store> Mapping(List<StoreDto> storeDtos)
        {
            return storeDtos?.Select(dto => Mapping(dto)).ToList() ?? new List<Store>(); // Use LINQ for cleaner mapping
        }
    }
}
