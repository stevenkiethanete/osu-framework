// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TripleBufferTest
    {
        [Test]
        public void TestWriteOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                using (tripleBuffer.GetForWrite())
                {
                }
            }
        }

        [Test]
        public void TestReadOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowNoContestation()
        {
            // Test with no contest
            var tripleBuffer = new TripleBuffer<TestObject>();

            using (var buffer = tripleBuffer.GetForWrite())
                buffer.Object = new TestObject(1);

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer?.Object?.ID, Is.EqualTo(1));

            using (var buffer = tripleBuffer.GetForWrite())
            {
                Assert.That(buffer.Object, Is.Null);
                buffer.Object = new TestObject(2);
            }
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowContestation()
        {
            // Test with first write in use during second.
            var tripleBuffer = new TripleBuffer<TestObject>();

            using (var buffer = tripleBuffer.GetForWrite())
                buffer.Object = new TestObject(1);

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(1));

                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object, Is.Null);
                    write.Object = new TestObject(2);
                }
            }

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(2));

                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object, Is.Null);
                    write.Object = new TestObject(3);
                }
            }
        }

        [Test]
        public void TestWriteThenRead()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                var obj = new TestObject(i);

                using (var buffer = tripleBuffer.GetForWrite())
                    buffer.Object = obj;

                using (var buffer = tripleBuffer.GetForRead())
                    Assert.That(buffer?.Object, Is.EqualTo(obj));
            }

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestReadSaturated()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 10; i++)
            {
                var obj = new TestObject(i);
                ManualResetEventSlim resetEventSlim = new ManualResetEventSlim();

                var readTask = Task.Factory.StartNew(() =>
                {
                    resetEventSlim.Set();
                    using (var buffer = tripleBuffer.GetForRead())
                        Assert.That(buffer?.Object, Is.EqualTo(obj));
                }, TaskCreationOptions.LongRunning);

                Task.Factory.StartNew(() =>
                {
                    resetEventSlim.Wait(1000);
                    Thread.Sleep(10);

                    using (var buffer = tripleBuffer.GetForWrite())
                        buffer.Object = obj;
                }, TaskCreationOptions.LongRunning);

                readTask.WaitSafely();
            }
        }

        private class TestObject
        {
            public readonly int ID;

            public TestObject(int id)
            {
                ID = id;
            }

            public override string ToString()
            {
                return $"{base.ToString()} {ID}";
            }
        }
    }
}
