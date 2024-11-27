using EerieLeap.Types;

namespace EerieLeap.Services;

public interface ISensorReadingService {
    Task<IEnumerable<ReadingResult>> GetReadingsAsync();
    Task<ReadingResult?> GetReadingAsync(string id);
    Task WaitForInitializationAsync();
}
