using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Board.Client.Services.Auth
{
    public interface ICustomAuthenticationStateProvider
    {
        Task<AuthenticationState> GetAuthenticationStateAsync();
    }
}
