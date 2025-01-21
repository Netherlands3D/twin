using System;
using System.Collections;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// The interface for a DataSet which can be read on-demand and be passed to between objects.
    ///
    /// DataSets can support a blocking and non-blocking -using coroutines- way of reading their data, the naming
    /// of the methods reflects that of C# Stream classes.
    ///
    /// Individual datasets can choose to implement one or the other and may throw a NotImplementedException if
    /// that specific path is not supported. 
    /// </summary>
    /// <typeparam name="T">The type of results from this dataset</typeparam>
    public interface DataSet<T>
    {
        public bool IsValid();

        public T Read();

        public IEnumerator ReadAsync(Action<float> onProgress, Action<T> onComplete);
    }
}