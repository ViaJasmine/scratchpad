﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DisposeTest.Test
{
    [TestClass]
    public class DisposeTests
    {
        [TestMethod]
        [TestCategory(UnitTestCategory.DevelopmentTime)]
        public void DisposeTest()
        {
            var freeAll = new Action(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                GC.Collect(0, GCCollectionMode.Forced);
                GC.Collect(1, GCCollectionMode.Forced);
                GC.Collect(2, GCCollectionMode.Forced);
            });

            freeAll();

            var memBegin = Process.GetCurrentProcess().PrivateMemorySize64;
            Debug.WriteLine("Begin: " + (memBegin / 1000000));

            var p1_1 = new Publisher();
            var p2_1 = new Publisher();

            var s1_1 = new Subscriber();
            var s2_1 = new Subscriber();

            s1_1.Subscribe(p1_1);
            s2_1.Subscribe(p2_1);

            // Everything is disposed

            p1_1 = null;
            p2_1 = null;
            s1_1 = null;
            s2_1 = null;

            freeAll();

            var afterDispose = Process.GetCurrentProcess().PrivateMemorySize64 - memBegin;
            Debug.WriteLine("All freed (should be around 0MB): " + (afterDispose / 1000000));

            memBegin = Process.GetCurrentProcess().PrivateMemorySize64;

            var p1_2 = new Publisher();
            var p2_2 = new Publisher();

            var s1_2 = new Subscriber();
            var s2_2 = new Subscriber();

            s1_2.Subscribe(p1_2);
            s2_2.Subscribe(p2_2);

            // Now we try to dispose subscriber 2
            // It is not freed, because it has reference to publisher 2 that was not disposed.

            p1_2 = null;  // dispose publisher 1
            s1_2 = null;  // dispose subscriber 1

            //p2 = null;  // publisher 2 not disposed
            s2_2 = null;  // dipose subscriber 2

            freeAll();

            afterDispose = Process.GetCurrentProcess().PrivateMemorySize64 - memBegin;
            Debug.WriteLine("Only publisher 1 and subscriber 1 are freed (should be around 200MB): " + (afterDispose / 1000000));

            memBegin = Process.GetCurrentProcess().PrivateMemorySize64;

            var p1_3 = new Publisher();
            var p2_3 = new Publisher();

            var s1_3 = new Subscriber();
            var s2_3 = new Subscriber();

            s1_3.Subscribe(p1_3);
            s2_3.Subscribe(p2_3);

            // Dispose publisher 2, but do not dispose subsriber 2
            // publisher 2 is disposed, eventhough subscriber 2 is attached to it's event

            p1_3 = null;  // dispose publisher 1
            p2_3 = null;  // dispose subscriber 1
            s1_3 = null;  // dispose publisher 2
            //s2 = null;  // subscriber 2 not disposed

            freeAll();

            afterDispose = Process.GetCurrentProcess().PrivateMemorySize64 - memBegin;
            Debug.WriteLine("Only subscriber 2 is not freed (should be around 100MB): " + (afterDispose / 1000000));
        }

        public class Publisher
        {
            private byte[] list = new byte[100000000];

            public event EventHandler<bool> Ready;

            public void SetReady()
            {
                Ready(this, true);
            }
        }

        public class Subscriber
        {
            private byte[] list = new byte[100000000];

            public bool LastState { get; private set; }

            public void Subscribe(Publisher pub)
            {
                pub.Ready += (s, e) => { LastState = e; };
            }
        }
    }
}
