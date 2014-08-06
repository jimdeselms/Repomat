using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Repomat.Runtime;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class ConcurrentLoadedCollectionTests
    {
        [Test]
        public void EmptyCollection_ZeroEntriesRead()
        {
            ConcurrentlyLoadedCollection<int> c = new ConcurrentlyLoadedCollection<int>(new int[0]);

            var array = c.ToArray();
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void CollectionWithEntries_GettingInitialRowIsInstantEvenIfWholeCollectionIsSlow()
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            ConcurrentlyLoadedCollection<int> c = new ConcurrentlyLoadedCollection<int>(BuildSlowCollection());
            int first = c.First();

            s.Stop();

            // Getting the first one should have been fast, and since we're not getting the second one,
            // this should be done before the second one would load.
            Assert.IsTrue(s.ElapsedMilliseconds < 100);

            Assert.AreEqual(1, first);
        }

        [Test]
        public void CollectionWithEntries_GettingAllRowsWaitsForAllToLoad()
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            ConcurrentlyLoadedCollection<int> c = new ConcurrentlyLoadedCollection<int>(BuildSlowCollection());
            var all = c.ToArray();

            s.Stop();

            // Since we got all, then we should wait on the lagging rows to load up.
            Assert.IsTrue(s.ElapsedMilliseconds >= 100);

            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(1, all[0]);
            Assert.AreEqual(2, all[1]);
        }

        [Test]
        public void CollectionWithEntries_ExceptionThrownOnLoad()
        {
            ConcurrentlyLoadedCollection<int> c = new ConcurrentlyLoadedCollection<int>(BuildSlowCollectionThatFails());

            // This passes, because the first one is harmless.
            var first = c.First();

            // Let's do it again for fun.
            first = c.First();

            // Now let's try to get the second one and it should blow up.
            Assert.Throws<AggregateException>(() => { var _ = c.ElementAt(1); });
        }

        [Test]
        public void RepeatedlyEnumerating_OnlyEnumerateOnce()
        {
            _currentValue = 10;

            var c = new ConcurrentlyLoadedCollection<int>(LoadValuesToProveThisIsOnlyCalledOnce());

            // Enumerate this multiple times. They'll all have the same result;
            var array1 = c.ToArray();
            var array2 = c.ToArray();
            var array3 = c.ToArray();

            CollectionAssert.AreEquivalent(array1, array2);
            CollectionAssert.AreEquivalent(array2, array3);
        }

        private int _currentValue = 0;

        private IEnumerable<int> LoadValuesToProveThisIsOnlyCalledOnce()
        {
            // This is designed so that if the enumerator were called more than once, then 
            // the returned values would be different.
            yield return _currentValue++;
            yield return _currentValue++;
            yield return _currentValue++;
        }

        private static IEnumerable<int> BuildSlowCollection()
        {
            // First one's fast,
            // Second one's slow.
            yield return 1;

            Thread.Sleep(100);

            yield return 2;
        }

        private static IEnumerable<int> BuildSlowCollectionThatFails()
        {
            // First one's fast,
            yield return 1;

            // After a wait, we fail with an exception.
            Thread.Sleep(100);

            throw new ArgumentOutOfRangeException("whatever");
        }
    }
}
