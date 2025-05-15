using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Utils.Tests {

    public class LogUtilTests {

        [Test]
        public void EmitExceptionTest() {
            var exceptions = new List<Exception>();
            ExceptionHandler action = (Exception err) => {
                exceptions.Add(err);
            };
            LogUtils.Emit(new InvalidOperationException());
            Assert.That(exceptions, Has.Count.EqualTo(0));
            LogUtils.OnException += action;
            LogUtils.Emit(new InvalidOperationException());
            Assert.That(exceptions, Has.Count.EqualTo(1));
            LogUtils.OnException -= action;
            LogUtils.Emit(new InvalidOperationException());
            Assert.That(exceptions, Has.Count.EqualTo(1));
        }

        [Test]
        public void EmitLogTest() {
            var logs = new List<string>();
            LogHandler action = (string err) => {
                logs.Add(err);
            };
            LogUtils.Emit(string.Empty);
            Assert.That(logs, Has.Count.EqualTo(0));
            LogUtils.OnLog += action;
            LogUtils.Emit(string.Empty);
            Assert.That(logs, Has.Count.EqualTo(1));
            LogUtils.OnLog -= action;
            LogUtils.Emit(string.Empty);
            Assert.That(logs, Has.Count.EqualTo(1));
        }
    }
}
