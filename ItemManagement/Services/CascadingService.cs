using ItemManagement.Interface;
using ItemManagement.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ItemManagement.Services
{
    public class CascadingService : ICascadingService
    {
        private readonly ApplicationDbContext _context;

        public CascadingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            return await _context.Countrys.ToListAsync();
        }

        public async Task<List<State>> GetStatesAsync(int countryId)
        {
            return await _context.States
                .Where(s => s.CountryId == countryId)
                .ToListAsync();
        }

        public async Task<List<City>> GetCitiesAsync(int stateId)
        {
            return await _context.Citys
                .Where(c => c.StateId == stateId)
                .ToListAsync();
        }

        public async Task<Country?> GetCountryByIdAsync(int id)
        {
            return await _context.Countrys.FindAsync(id);
        }

        public async Task<State?> GetStateByIdAsync(int id)
        {
            return await _context.States.FindAsync(id);
        }

        public async Task<City?> GetCityByIdAsync(int id)
        {
            return await _context.Citys.FindAsync(id);
        }
    }
}