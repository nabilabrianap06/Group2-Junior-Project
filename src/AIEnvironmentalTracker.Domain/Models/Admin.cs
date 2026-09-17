using System;
using System.Collections.Generic;

namespace AIEnvironmentalTracker.Domain.Models
{
    public enum AdminLevel
    {
        Support,
        SuperAdmin
    }

    /// <summary>
    /// An administrator account able to manage the pool of registered users.
    /// Maps to Activity Diagram 3: Admin Manage User Accounts.
    /// </summary>
    public class Admin : User
    {
        public AdminLevel AdminLevel { get; private set; }

        private readonly List<RegisteredUser> _managedUsers = new();
        public IReadOnlyList<RegisteredUser> ManagedUsers => _managedUsers.AsReadOnly();

        private Admin() { } // EF Core

        private Admin(string username, string email, string password, AdminLevel adminLevel)
            : base(username, email, password, UserRole.Admin)
        {
            AdminLevel = adminLevel;
        }

        public static Admin Register(string username, string email, string password, AdminLevel adminLevel = AdminLevel.Support)
        {
            return new Admin(username, email, password, adminLevel);
        }

        /// <summary>
        /// Maps to Activity Diagram 3: "Open Admin Panel: Manage User Accounts".
        /// Returns the current roster so a caller can decide to Add, Edit, or Suspend.
        /// </summary>
        public IReadOnlyList<RegisteredUser> ManageUserAccounts() => ManagedUsers;

        /// <summary>
        /// Maps to Activity Diagram 3: "New account" branch -> "Save changes to database".
        /// </summary>
        public RegisteredUser AddUser(string username, string email, string password)
        {
            var user = RegisteredUser.Register(username, email, password);
            _managedUsers.Add(user);
            return user;
        }

        /// <summary>
        /// Maps to Activity Diagram 3: "Edit/Suspend" -> "Load selected user's existing
        /// record" -> "Admin submits changes" -> "Save changes".
        /// </summary>
        public void EditUser(RegisteredUser user, string? newUsername = null, string? newEmail = null)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (!_managedUsers.Contains(user))
                throw new InvalidOperationException("This user is not managed by this admin instance.");

            user.UpdateProfile(newUsername, newEmail);
        }

        /// <summary>
        /// Suspends (rather than deletes) an account, so a suspended user's Login() fails
        /// but their historical usage logs and report are preserved.
        /// </summary>
        public bool SuspendUser(RegisteredUser user)
        {
            if (user is null || !_managedUsers.Contains(user))
                return false;

            user.Suspend();
            return true;
        }
    }
}
