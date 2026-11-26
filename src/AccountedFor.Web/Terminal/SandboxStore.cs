using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fidos.Core;
using Fidos.Core.Training;
using Microsoft.Extensions.Options;

namespace AccountedFor.Web.Terminal;

public sealed class SandboxOptions
{
    /// <summary>Folder where each sandbox is saved as JSON. Relative paths are resolved from the content root.</summary>
    public string DataPath { get; set; } = "App_Data/sandboxes";

    /// <summary>Sandboxes idle for longer than this are dropped from memory (they stay on disk).</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Keeps each trainee's sandbox company in memory and persists it to a JSON file after every change.
/// Sandbox ids are random GUIDs held in a browser cookie, so trainees cannot see each other's books.
/// </summary>
public sealed class SandboxStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ConcurrentDictionary<string, Entry> _cache = new();
    private readonly string _directory;
    private readonly TimeSpan _idleTimeout;
    private readonly ILogger<SandboxStore> _logger;

    public SandboxStore(IOptions<SandboxOptions> options, IWebHostEnvironment environment, ILogger<SandboxStore> logger)
    {
        _directory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.DataPath));
        _idleTimeout = options.Value.IdleTimeout;
        _logger = logger;
        Directory.CreateDirectory(_directory);
    }

    public static bool IsValidId(string? id) => Guid.TryParseExact(id, "N", out _);

    public string Create(string? operatorId)
    {
        var id = Guid.NewGuid().ToString("N");
        var company = Company.CreateNew(operatorId);
        _cache[id] = new Entry(company);
        Save(id, company);
        return id;
    }

    public Company? Find(string? id)
    {
        if (!IsValidId(id))
        {
            return null;
        }

        var key = id!.ToLowerInvariant();
        if (_cache.TryGetValue(key, out var entry))
        {
            entry.Touch();
            return entry.Company;
        }

        var company = Load(key);
        if (company is null)
        {
            return null;
        }

        Trim();
        return _cache.GetOrAdd(key, _ => new Entry(company)).Company;
    }

    /// <summary>Applies a change under a per-sandbox lock, checks training progress and saves.</summary>
    public T Update<T>(string id, Company company, Func<Company, T> action, out Level? completedLevel)
    {
        lock (company)
        {
            var result = action(company);
            completedLevel = company.CheckLevelProgress();
            Save(id.ToLowerInvariant(), company);
            return result;
        }
    }

    /// <summary>Wipes a sandbox back to a brand new company, keeping the operator id.</summary>
    public void Reset(string id, string operatorId)
    {
        var key = id.ToLowerInvariant();
        var company = Company.CreateNew(operatorId);
        _cache[key] = new Entry(company);
        Save(key, company);
    }

    private string PathFor(string id) => Path.Combine(_directory, $"{id}.json");

    private Company? Load(string id)
    {
        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<Company>(stream, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogWarning(ex, "Could not load sandbox {SandboxId}", id);
            return null;
        }
    }

    private void Save(string id, Company company)
    {
        var path = PathFor(id);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(company, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }

    private void Trim()
    {
        var cutoff = DateTime.UtcNow - _idleTimeout;
        foreach (var (key, entry) in _cache)
        {
            if (entry.LastUsedUtc < cutoff)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private sealed class Entry(Company company)
    {
        public Company Company { get; } = company;
        public DateTime LastUsedUtc { get; private set; } = DateTime.UtcNow;
        public void Touch() => LastUsedUtc = DateTime.UtcNow;
    }
}
