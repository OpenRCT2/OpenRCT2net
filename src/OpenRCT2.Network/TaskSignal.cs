using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRCT2.Network
{
    /// <summary>
    /// Wait handle for creating tasks that wait for a signal from another thread.
    /// </summary>
    internal class TaskSignal
    {
        private ManualResetEvent _signal = new ManualResetEvent(false);

        public void Reset()
        {
            _signal.Reset();
        }

        public void Set()
        {
            _signal.Set();
        }

        /// <summary>
        /// Waits forever until the signal is set.
        /// </summary>
        /// <returns></returns>
        public Task Wait()
        {
            return Task.Run(() =>
            {
                _signal.WaitOne();
            });
        }

        /// <summary>
        /// Waits until the signal is set or the given timeout is reached.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task Wait(TimeSpan timeout)
        {
            return Task.Run(() =>
            {
                if (!_signal.WaitOne(timeout))
                {
                    throw new TimeoutException();
                }
            });
        }

        /// <summary>
        /// Waits until the signal is set or the given timeout is reached.
        /// </summary>
        /// <param name="timeout">Time interval in milliseconds.</param>
        /// <returns></returns>
        public Task Wait(int timeout)
        {
            return Wait(TimeSpan.FromMilliseconds(timeout));
        }
    }
}
