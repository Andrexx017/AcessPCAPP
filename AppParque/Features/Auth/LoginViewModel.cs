using AppParque.Services;

namespace AppParque.Features.Auth
{
    public class LoginViewModel
    {
        private FireBaseService _firebaseService = new FireBaseService();

        public async Task<bool> LoginAsync(string username, string password)
        {
            var users = await _firebaseService.GetUserAsync();
            if (users != null && users.ContainsKey(username))
            {
                var user = users[username];
                return user.password == password;
            }

            return false;
        }
    }
}
