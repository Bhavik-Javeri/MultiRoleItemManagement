using ItemManagement.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItemManagement.Interface
{
    public interface ICascadingService
    {
        Task<List<Country>> GetCountriesAsync();
        Task<List<State>> GetStatesAsync(int countryId);
        Task<List<City>> GetCitiesAsync(int stateId);
        Task<Country?> GetCountryByIdAsync(int id);
        Task<State?> GetStateByIdAsync(int id);
        Task<City?> GetCityByIdAsync(int id);
    }
}