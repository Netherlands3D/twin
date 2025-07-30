using System;
using System.Runtime.CompilerServices;
using RSG;

public static class PromiseAwaiterExtensions
{
    public static PromiseAwaiter<T> GetAwaiter<T>(this IPromise<T> promise)
        => new PromiseAwaiter<T>(promise);

    public static PromiseAwaiter GetAwaiter(this IPromise promise)
        => new PromiseAwaiter(promise);
}

public class PromiseAwaiter<T> : INotifyCompletion
{
    readonly IPromise<T> _promise;
    T _result;
    Exception _exception;

    public PromiseAwaiter(IPromise<T> promise)
    {
        _promise   = promise;
        _result    = default;
        _exception = null;
    }

    public bool IsCompleted => false;  // Always async

    public T GetResult()
    {
        if (_exception != null) throw _exception;
        return _result;
    }

    public void OnCompleted(Action continuation)
    {
        _promise
            .Then(result =>
            {
                _result = result;
                continuation();
            })
            .Catch(ex =>
            {
                _exception = ex;
                continuation();
            });
    }
}

public class PromiseAwaiter : INotifyCompletion
{
    readonly IPromise _promise;
    Exception _exception;

    public PromiseAwaiter(IPromise promise)
    {
        _promise   = promise;
        _exception = null;
    }

    public bool IsCompleted => false;

    public void GetResult()
    {
        if (_exception != null) throw _exception;
    }

    public void OnCompleted(Action continuation)
    {
        _promise
            .Then(() => continuation())
            .Catch(ex =>
            {
                _exception = ex;
                continuation();
            });
    }
}