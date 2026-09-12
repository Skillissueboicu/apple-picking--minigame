namespace FarmerQuest.Net
{
    // Authenticated user profile returned by the auth API
    [System.Serializable]
    public class UserInfo
    {
        public string id;
        public string email;
        public string name;
        public string role;
    }

    // Login/register response containing JWT and user
    [System.Serializable]
    public class AuthResponse
    {
        public string token;
        public UserInfo user;
    }
}
