using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Repomat.Runtime
{
    public class ConcurrentlyLoadedCollection<T> : IEnumerable<T>
    {
        private readonly IEnumerator<T> _loader;
        private bool _isCompletelyLoaded = false;
        private bool _inErrorState = false;

        private Task _loaderTask;
        private readonly List<T> _loadedValues = new List<T>();

        public ConcurrentlyLoadedCollection(IEnumerable<T> loader)
        {
            _loader = loader.GetEnumerator();
            _loaderTask = Task.Factory.StartNew(LoadValues);
            _loaderTask.ContinueWith(i => { });
        }

        private void LoadValues()
        {
            try
            {
                while (_loader.MoveNext())
                {
                    _loadedValues.Add(_loader.Current);
                }
                _isCompletelyLoaded = true;
            }
            catch
            {
                _inErrorState = true;
                throw;
            }
        }

        internal void PropagateAnyErrors()
        {
            // If it's in an error state, then force the task to propagate its error up.
            if (_inErrorState)
            {
                _loaderTask.Wait();
            }
        }

        internal bool IsCompletelyLoaded { get { return _isCompletelyLoaded; } }

        public IReadOnlyList<T> LoadedValues { get { return _loadedValues; } } 

        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentlyLoadedCollectionEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class ConcurrentlyLoadedCollectionEnumerator<T1> : IEnumerator<T1>
        {
            private int _currentIndex = -1;
            private readonly ConcurrentlyLoadedCollection<T1> _coll; 

            public ConcurrentlyLoadedCollectionEnumerator(ConcurrentlyLoadedCollection<T1> coll)
            {
                _coll = coll;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _currentIndex++;

                while (true)
                {
                    if (_currentIndex < _coll.LoadedValues.Count)
                    {
                        // More to go? Just return it.
                        return true;
                    }
                    else if (_coll.IsCompletelyLoaded)
                    {
                        // Successfully got to the end.
                        return false;
                    }
                    else
                    {
                        // Not done reading all the values.
                        // First make sure nothing went wrong, then just wait until we get something else.
                        // TODO - should this have a timeout?
                        _coll.PropagateAnyErrors();
                        Thread.Sleep(5);
                    }
                }
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public T1 Current { get { return _coll.LoadedValues[_currentIndex]; } }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
