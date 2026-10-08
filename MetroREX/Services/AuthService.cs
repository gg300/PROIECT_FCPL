using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class AuthService
{
    private static readonly (string Hash, string Salt) DummyCredentials =
        PasswordHasher.Hash(Guid.NewGuid().ToString("N"));

    private readonly ShopRepository _repository;
    private readonly Session _session;

    public AuthService(ShopRepository repository, Session session)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public Result<User> Register(string username, string password, UserProfile profile)
    {
        var error = Validation.Username(username) ?? Validation.Password(password) ?? Validation.Profile(profile);
        if (error is not null)
        {
            return Result.Failure<User>(error);
        }

        var name = username.Trim();
        if (FindUser(name) is not null)
        {
            return Result.Failure<User>("Numele de utilizator este deja folosit.");
        }

        var (hash, salt) = PasswordHasher.Hash(password);
        var user = new User
        {
            Username = name,
            PasswordHash = hash,
            PasswordSalt = salt,
            Profile = profile.Normalized(),
        };

        _repository.Users.Add(user);
        _repository.SaveUsers();
        return Result.Success(user);
    }

    public Result<User> Login(string username, string password)
    {
        var user = FindUser(username);
        var (hash, salt) = user is null ? DummyCredentials : (user.PasswordHash, user.PasswordSalt);
        var passwordMatches = PasswordHasher.Verify(password ?? string.Empty, hash, salt);

        if (user is null || !passwordMatches)
        {
            return Result.Failure<User>(ErrorMessages.InvalidCredentials);
        }

        _session.SignIn(user);
        return Result.Success(user);
    }

    public void Logout() => _session.SignOut();

    public Result ChangePassword(string currentPassword, string newPassword)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        if (!PasswordHasher.Verify(currentPassword ?? string.Empty, user.PasswordHash, user.PasswordSalt))
        {
            return Result.Failure("Parola curenta este incorecta.");
        }

        var error = Validation.Password(newPassword);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        if (newPassword == currentPassword)
        {
            return Result.Failure("Parola noua trebuie sa fie diferita de cea curenta.");
        }

        (user.PasswordHash, user.PasswordSalt) = PasswordHasher.Hash(newPassword);
        _repository.SaveUsers();
        return Result.Success();
    }

    public Result UpdateProfile(UserProfile profile)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        var error = Validation.Profile(profile);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        user.Profile = profile.Normalized();
        _repository.SaveUsers();
        return Result.Success();
    }

    public Result DeleteAccount(string password)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        if (!PasswordHasher.Verify(password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
        {
            return Result.Failure("Parola este incorecta.");
        }

        _repository.Users.Remove(user);
        _repository.SaveUsers();
        _session.SignOut();
        return Result.Success();
    }

    private User? FindUser(string? username)
    {
        var name = username?.Trim();
        return string.IsNullOrEmpty(name)
            ? null
            : _repository.Users.FirstOrDefault(user =>
                string.Equals(user.Username, name, StringComparison.OrdinalIgnoreCase));
    }
}
