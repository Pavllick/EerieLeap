using System;
using System.Text.Json;

namespace EerieLeap.Repositories;

/// <summary>
/// Represents the result of a configuration operation
/// </summary>
public record ConfigurationResult<T>(
    bool Success,
    T? Data = default,
    string? Error = null
);

/// <summary>
/// Defines operations for managing configuration persistence
/// </summary>
public interface IConfigurationRepository : IDisposable {
    /// <summary>
    /// Loads a configuration of type T from storage
    /// </summary>
    /// <typeparam name="T">The type of configuration to load</typeparam>
    /// <param name="name">The configuration identifier</param>
    /// <returns>A result containing the configuration if successful</returns>
    Task<ConfigurationResult<T>> LoadAsync<T>(string name) where T : class;

    /// <summary>
    /// Saves a configuration to storage
    /// </summary>
    /// <typeparam name="T">The type of configuration to save</typeparam>
    /// <param name="name">The configuration identifier</param>
    /// <param name="config">The configuration data to save</param>
    /// <returns>A result indicating success or failure</returns>
    Task<ConfigurationResult<T>> SaveAsync<T>(string name, T config) where T : class;

    /// <summary>
    /// Checks if a configuration exists in storage
    /// </summary>
    /// <param name="name">The configuration identifier to check</param>
    /// <returns>True if the configuration exists, false otherwise</returns>
    Task<bool> ExistsAsync(string name);

    /// <summary>
    /// Deletes a configuration from storage
    /// </summary>
    /// <param name="name">The configuration identifier to delete</param>
    /// <returns>True if the configuration was deleted, false if it didn't exist</returns>
    Task<bool> DeleteAsync(string name);
}
