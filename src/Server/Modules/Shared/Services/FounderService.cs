using StartupAgent.Data.Repositories;
using StartupAgent.Shared.Contracts;
using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Service interface for founder profile management.
/// </summary>
public interface IFounderService
{
    /// <summary>
    /// Get founder profile by ID.
    /// </summary>
    Task<FounderDto?> GetFounderByIdAsync(string founderId);

    /// <summary>
    /// Get founder profile by email.
    /// </summary>
    Task<FounderDto?> GetFounderByEmailAsync(string email);

    /// <summary>
    /// Create a new founder profile.
    /// </summary>
    Task<FounderDto> CreateFounderAsync(string email, CreateUpdateFounderDto dto);

    /// <summary>
    /// Update founder profile.
    /// </summary>
    Task<FounderDto> UpdateFounderAsync(string founderId, CreateUpdateFounderDto dto);

    /// <summary>
    /// Check if founder exists by email.
    /// </summary>
    Task<bool> FounderExistsByEmailAsync(string email);

    /// <summary>
    /// Get or create founder (upsert pattern).
    /// Used after magic link verification to ensure founder record exists.
    /// </summary>
    Task<FounderDto> GetOrCreateFounderAsync(string email);
}

/// <summary>
/// Implementation of founder profile management service.
/// </summary>
public class FounderService : IFounderService
{
    private readonly IFounderRepository _founderRepository;
    private readonly ILogger<FounderService> _logger;

    public FounderService(IFounderRepository founderRepository, ILogger<FounderService> logger)
    {
        _founderRepository = founderRepository;
        _logger = logger;
    }

    public async Task<FounderDto?> GetFounderByIdAsync(string founderId)
    {
        var founder = await _founderRepository.GetByIdAsync(founderId);
        return founder == null ? null : MapToDto(founder);
    }

    public async Task<FounderDto?> GetFounderByEmailAsync(string email)
    {
        var founder = await _founderRepository.GetByEmailAsync(email);
        return founder == null ? null : MapToDto(founder);
    }

    public async Task<FounderDto> CreateFounderAsync(string email, CreateUpdateFounderDto dto)
    {
        // Check if founder already exists
        var existing = await _founderRepository.GetByEmailAsync(email);
        if (existing != null)
        {
            _logger.LogWarning("Founder with email {Email} already exists", email);
            throw new InvalidOperationException($"Founder with email '{email}' already exists");
        }

        var founder = new Founder
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = dto.DisplayName,
            StartupName = dto.StartupName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _founderRepository.AddAsync(founder);
        await _founderRepository.SaveChangesAsync();

        _logger.LogInformation("Founder created with email: {Email}, FounderId: {FounderId}", email, founder.Id);

        return MapToDto(founder);
    }

    public async Task<FounderDto> UpdateFounderAsync(string founderId, CreateUpdateFounderDto dto)
    {
        var founder = await _founderRepository.GetByIdAsync(founderId);
        if (founder == null)
        {
            _logger.LogWarning("Founder not found: {FounderId}", founderId);
            throw new InvalidOperationException($"Founder with ID '{founderId}' not found");
        }

        founder.DisplayName = dto.DisplayName ?? founder.DisplayName;
        founder.StartupName = dto.StartupName ?? founder.StartupName;
        founder.UpdatedAt = DateTime.UtcNow;

        await _founderRepository.UpdateAsync(founder);
        await _founderRepository.SaveChangesAsync();

        _logger.LogInformation("Founder updated: {FounderId}", founderId);

        return MapToDto(founder);
    }

    public async Task<bool> FounderExistsByEmailAsync(string email)
    {
        return await _founderRepository.EmailExistsAsync(email);
    }

    public async Task<FounderDto> GetOrCreateFounderAsync(string email)
    {
        var existing = await _founderRepository.GetByEmailAsync(email);
        if (existing != null)
        {
            _logger.LogDebug("Founder already exists: {Email}", email);
            return MapToDto(existing);
        }

        _logger.LogInformation("Creating new founder record for email: {Email}", email);

        var founder = new Founder
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _founderRepository.AddAsync(founder);
        await _founderRepository.SaveChangesAsync();

        return MapToDto(founder);
    }

    private static FounderDto MapToDto(Founder founder)
    {
        return new FounderDto
        {
            Id = founder.Id,
            Email = founder.Email,
            DisplayName = founder.DisplayName,
            StartupName = founder.StartupName,
            LastMindset = founder.LastMindset?.ToString(),
            CreatedAt = founder.CreatedAt,
            UpdatedAt = founder.UpdatedAt
        };
    }
}
