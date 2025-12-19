using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MedDemo.Application.Common;
using MedDemo.Domain.Authorization;
using System.Security.Cryptography;
using System.Text;

namespace MedDemo.WebAPI.Extensions;

/// <summary>
/// Provides extension methods for configuring JWT authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    private const int MinimumKeyLength = 32; // 256 bits minimum for HS256
    private const int MaxClockSkew = 5; // Maximum clock skew in minutes

    /// <summary>
    /// Configures JWT authentication and authorization for production environments.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        Identity identitySettings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(identitySettings);

        ValidateIdentitySettings(identitySettings);

        ConfigureAuthentication(services, identitySettings, validateLifetime: true);
        ConfigureAuthorization(services, identitySettings);

        return services;
    }

    /// <summary>
    /// Configures JWT authentication and authorization for local development.
    /// Note: Disables token lifetime validation for easier testing.
    /// </summary>
    public static IServiceCollection AddJwtAuthenticationLocal(
        this IServiceCollection services,
        Identity identitySettings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(identitySettings);

        ValidateIdentitySettings(identitySettings);

        ConfigureAuthentication(services, identitySettings, validateLifetime: false);
        ConfigureAuthorization(services, identitySettings);

        return services;
    }

    private static void ConfigureAuthentication(
        IServiceCollection services,
        Identity identitySettings,
        bool validateLifetime)
    {
        var schemeName = GetSchemeName(identitySettings.Issuer);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
        })
        .AddJwtBearer(schemeName, options =>
        {
            options.SaveToken = false; // Security: Don't save tokens in authentication properties
            options.RequireHttpsMetadata = identitySettings.ValidateHttps;
            options.Authority = identitySettings.Issuer;

            options.TokenValidationParameters = CreateTokenValidationParameters(
                identitySettings,
                validateLifetime);

            // Security: Enhanced event handlers
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Security: Log authentication failures (implement your logging)
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Security: Additional claims validation can be added here
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    // Security: Support SignalR or WebSocket token from query string
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
    }

    private static TokenValidationParameters CreateTokenValidationParameters(
        Identity identitySettings,
        bool validateLifetime)
    {
        var signingKey = CreateSigningKey(identitySettings.Key);

        return new TokenValidationParameters
        {
            // Issuer validation
            ValidateIssuer = true,
            ValidIssuer = identitySettings.Issuer,

            // Audience validation
            ValidateAudience = true,
            ValidAudience = identitySettings.Audience,

            // Signing key validation
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            // Lifetime validation
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(MaxClockSkew),

            // Security: Additional validations
            RequireExpirationTime = true,
            RequireSignedTokens = true,

            // Security: Algorithm validation
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha384, SecurityAlgorithms.HmacSha512],

            // Security: Prevent token replay attacks (if using token ID)
            ValidateTokenReplay = false, // Set to true if implementing token replay detection
        };
    }

    private static void ConfigureAuthorization(
        IServiceCollection services,
        Identity identitySettings)
    {
        var schemeName = GetSchemeName(identitySettings.Issuer);

        services.AddAuthorization(options =>
        {
            // Default policy: Require authenticated user
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName)
                .Build();

            // Fallback policy: Applied when no specific policy is specified
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName)
                .Build();

            // Custom scope-based policies
            AddScopePolicy(options, "user_read", identitySettings, "read");
            AddScopePolicy(options, "user_write", identitySettings, "write");
            AddScopePolicy(options, "user_delete", identitySettings, "delete");
            AddScopePolicy(options, "admin", identitySettings, "admin");
        });

        // Register scope requirement handler
        services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
    }

    private static void AddScopePolicy(
        AuthorizationOptions options,
        string policyName,
        Identity identitySettings,
        string scope)
    {
        var fullScope = $"{identitySettings.ScopeBaseDomain}/{scope}";

        options.AddPolicy(policyName, policy =>
        {
            policy.Requirements.Add(new HasScopeRequirement(
                identitySettings.ScopeBaseDomain,
                fullScope,
                identitySettings.Issuer));
        });
    }

    private static SymmetricSecurityKey CreateSigningKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var keyBytes = Encoding.UTF8.GetBytes(key);

        // Security: Validate key length
        if (keyBytes.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"JWT signing key must be at least {MinimumKeyLength} bytes ({MinimumKeyLength * 8} bits) for HS256. " +
                $"Current key length: {keyBytes.Length} bytes.");
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    private static string GetSchemeName(string issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        // Create a safe scheme name from issuer
        var safeName = new string(issuer
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        return $"{JwtBearerDefaults.AuthenticationScheme}_{safeName}";
    }

    private static void ValidateIdentitySettings(Identity identitySettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identitySettings.Issuer, nameof(identitySettings.Issuer));
        ArgumentException.ThrowIfNullOrWhiteSpace(identitySettings.Audience, nameof(identitySettings.Audience));
        ArgumentException.ThrowIfNullOrWhiteSpace(identitySettings.Key, nameof(identitySettings.Key));
        ArgumentException.ThrowIfNullOrWhiteSpace(identitySettings.ScopeBaseDomain, nameof(identitySettings.ScopeBaseDomain));

        // Security: Validate issuer format
        if (!Uri.TryCreate(identitySettings.Issuer, UriKind.Absolute, out var issuerUri))
        {
            throw new ArgumentException("Issuer must be a valid absolute URI", nameof(identitySettings.Issuer));
        }

        // Security: Enforce HTTPS in production
        if (identitySettings.ValidateHttps && issuerUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "HTTPS is required for the issuer when ValidateHttps is true. " +
                "Use HTTP only in local development environments.");
        }

        // Security: Validate key strength
        var keyBytes = Encoding.UTF8.GetBytes(identitySettings.Key);
        if (keyBytes.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"JWT signing key must be at least {MinimumKeyLength} bytes for security. " +
                "Generate a strong key using a cryptographically secure random generator.");
        }
    }
}

/// <summary>
/// Helper class for generating secure JWT signing keys.
/// </summary>
public static class JwtKeyGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random key for JWT signing.
    /// </summary>
    /// <param name="keyLengthBytes">Key length in bytes (minimum 32 for HS256)</param>
    /// <returns>Base64-encoded secure random key</returns>
    public static string GenerateSecureKey(int keyLengthBytes = 64)
    {
        if (keyLengthBytes < 32)
        {
            throw new ArgumentException("Key length must be at least 32 bytes", nameof(keyLengthBytes));
        }

        var randomBytes = new byte[keyLengthBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }
}