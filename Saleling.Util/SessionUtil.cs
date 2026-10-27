using Saleling.Model;

namespace Saleling.Util
{
    public class SessionUtil
    {
        private static readonly SessionUtil instance = new SessionUtil();

        private readonly object lockObject = new object();

        private UserModel currentUser;

        private SessionUtil() { }

        public static SessionUtil Instance { get { return instance; } }

        public UserModel CurrentUser
        {
            get { lock (lockObject) { return currentUser; } }
        }

        public UserModel RequireLogin()
        {
            UserModel? user = CurrentUser;
            if (user == null)
            {
                throw new UnauthorizedAccessException("You must be logged in to perform this action.");
            }

            return user;
        }

        public UserModel RequireRole(params string[] allowedRoles)
        {
            UserModel user = RequireLogin();
            if (!allowedRoles.Contains(user.Role))
            {
                throw new UnauthorizedAccessException("You do not have permission to perform this action.");
            }

            return user;
        }

        public void SetLoggedInUser(UserModel account)
        {
            lock (lockObject)
            {
                currentUser = account;
            }
        }

        public void Logout()
        {
            lock (lockObject)
            {
                currentUser = null;
            }
        }
    }
}
