using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class FileSystemPrivateKeyProvider : IGitHubPrivateKeyProvider
{
    private readonly string _filePath;
    private readonly ILogger<FileSystemPrivateKeyProvider> _logger;

    public FileSystemPrivateKeyProvider(IConfiguration configuration, ILogger<FileSystemPrivateKeyProvider> logger)
    {
        var configPath = configuration["GitHub:PrivateKeyPath"] 
            ?? throw new InvalidOperationException("GitHub:PrivateKeyPath is not configured");
        
        // Resolve to absolute path
        _filePath = Path.IsPathRooted(configPath) 
            ? configPath 
            : Path.GetFullPath(configPath);
        
        _logger = logger;
        
        _logger.LogInformation("Using file system private key provider. File: {FilePath}", _filePath);
    }

    public async Task<string> GetPrivateKeyAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                // Try to provide helpful error message with current directory
                var currentDir = Directory.GetCurrentDirectory();
                _logger.LogError("File not found. Current directory: {CurrentDir}, Looking for: {FilePath}", currentDir, _filePath);
                throw new FileNotFoundException($"GitHub App private key file not found at: {_filePath} (Current directory: {currentDir})");
            }

            var privateKey = await File.ReadAllTextAsync(_filePath);
            
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                throw new InvalidOperationException($"GitHub App private key file is empty: {_filePath}");
            }

            _logger.LogInformation("Successfully loaded private key from file");
            return privateKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load private key from file: {FilePath}", _filePath);
            throw;
        }
    }

    public string GetProviderType()
    {
        return "FileSystem";
    }
}
