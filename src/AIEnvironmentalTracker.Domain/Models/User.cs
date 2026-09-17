using System;

namespace AIEnvironmentalTracker.Domain.Models
{
    /// <summary>
    /// Role a User account can hold. Drives which concrete subtype the account maps to.
    /// </summary>
    public enum UserRole
    {
        RegisteredUser,
        Admin
    }

    /// <summary>
    /// Abstract base for any account able to authenticate with the system.
    /// Concrete registration is realized on the subclasses (RegisteredUser.Register /
    /// Admin.Register) since an abstract User can never be instantiated directly - this
    /// satisfies the diagram's registerAccount() contract while keeping the type safe.
    /// </summary>
    public abstract class User
    {
        // Primary Key for EF Core
        public int UserId { get; protected set; }

        public string Username { get; protected set; } = string.Empty;
        public string Email { get; protected set; } = string.Empty;

        // In a real system this must be a salted hash, never plain text.
        public string Password { get; protected set; } = string.Empty;

        public UserRole Role { get; protected set; }
        public bool IsSuspended { get; protected set; }

        // Parameterless constructor for EF Core
        protected User() { }

        protected User(string username, string email, string password, UserRole role)
        {
            ValidateAccount(username, email, password);

            Username = username;
            Email = email;
            Password = password;
            Role = role;
        }

        /// <summary>
        /// Maps to Activity Diagram 1: "Login match a registered account?".
        /// </summary>
        public virtual bool Login(string email, string password)
        {
            if (IsSuspended)
                return false;

            return string.Equals(Email, email, StringComparison.OrdinalIgnoreCase)
                && Password == password;
        }

        /// <summary>
        /// Maps to Activity Diagram 3: "Admin submits changes" for an existing account.
        /// </summary>
        public void UpdateProfile(string? newUsername, string? newEmail)
        {
            if (string.IsNullOrWhiteSpace(newUsername) && string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("At least one field must be provided to update.");

            if (!string.IsNullOrWhiteSpace(newUsername))
                Username = newUsername;

            if (!string.IsNullOrWhiteSpace(newEmail))
            {
                if (!newEmail.Contains('@'))
                    throw new ArgumentException("A valid email address is required.", nameof(newEmail));

                Email = newEmail;
            }
        }

        public virtual void Suspend() => IsSuspended = true;
        public virtual void Reactivate() => IsSuspended = false;

        /// <summary>
        /// Shared validation used by every concrete Register(...) factory method.
        /// Maps to Activity Diagram 1: "Fields valid & email not already registered?"
        /// (uniqueness against existing accounts is enforced at the repository/persistence
        /// layer, not here, since this class has no knowledge of other accounts).
        /// </summary>
        protected static void ValidateAccount(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new ArgumentException("A valid email address is required.", nameof(email));

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.", nameof(password));
        }
    }
}
