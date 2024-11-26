namespace EerieLeap.Utilities;

/// <summary>
/// Provides an async-compatible mutual exclusion lock.
/// </summary>
public sealed class AsyncLock : IDisposable {
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Acquires the lock asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the lock acquisition.</param>
    /// <returns>A disposable handle to release the lock.</returns>
    public async ValueTask<IDisposable> LockAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(this);
    }

    public void Dispose() {
        if (_disposed)
            return;

        _semaphore.Dispose();
        _disposed = true;
    }

    private sealed class Releaser : IDisposable {
        private readonly AsyncLock _toRelease;
        private bool _disposed;

        internal Releaser(AsyncLock toRelease) =>
            _toRelease = toRelease;

        public void Dispose() {
            if (_disposed)
                return;

            _toRelease._semaphore.Release();
            _disposed = true;
        }
    }
}
