using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler
{
    /*
     MIT License _____

    Permission is hereby granted, free of charge, to any person obtaining a copy of _____ (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice (including the next paragraph) shall be included in all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL _____ BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
     */

    namespace ProcessMonitoring
    {
        public sealed class NetworkPerformanceReporter : IDisposable
        {
            private DateTime m_EtwStartTime;
            private TraceEventSession m_EtwSession;
            private int targetProcessId;
            private readonly Counters m_Counters = new Counters();

            private class Counters
            {
                public long Received;
                public long Sent;
            }

            private NetworkPerformanceReporter() { }

            

            public static NetworkPerformanceReporter Create()
            {
                var networkPerformancePresenter = new NetworkPerformanceReporter();
                networkPerformancePresenter.Initialise(Process.GetCurrentProcess().Id);
                return networkPerformancePresenter;
            }

            public static NetworkPerformanceReporter Create(int targetProcessId)
            {
                var networkPerformancePresenter = new NetworkPerformanceReporter();
                networkPerformancePresenter.Initialise(targetProcessId);
                return networkPerformancePresenter;
            }

            private void Initialise(int processId)
            {
                // Note that the ETW class blocks processing messages, so should be run on a different thread if you want the application to remain responsive.
                this.targetProcessId = processId;
                Task.Run(() => StartEtwSession());
            }

            private void StartEtwSession()
            {
                try
                {

                    var processId = targetProcessId;
                    ResetCounters();

                    using (m_EtwSession = new TraceEventSession("MyKernelAndClrEventsSession"))
                    {
                        m_EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                        m_EtwSession.Source.Kernel.TcpIpRecv += data =>
                        {
                            if (data.ProcessID == processId)
                            {
                                lock (m_Counters)
                                {
                                    m_Counters.Received += data.size;
                                }
                            }
                        };

                        m_EtwSession.Source.Kernel.TcpIpSend += data =>
                        {
                            if (data.ProcessID == processId)
                            {
                                lock (m_Counters)
                                {
                                    m_Counters.Sent += data.size;
                                }
                            }
                        };

                        m_EtwSession.Source.Process();
                    }
                }
                catch
                {
                    ResetCounters(); // Stop reporting figures
                                     // Probably should log the exception
                }
            }

            public NetworkPerformanceData GetNetworkPerformanceData()
            {
                var timeDifferenceInSeconds = (DateTime.Now - m_EtwStartTime).TotalSeconds;

                NetworkPerformanceData networkData;

                lock (m_Counters)
                {
                    networkData = new NetworkPerformanceData
                    {
                        BytesReceived = Convert.ToInt64(m_Counters.Received / timeDifferenceInSeconds),
                        BytesSent = Convert.ToInt64(m_Counters.Sent / timeDifferenceInSeconds)
                    };

                }

                // Reset the counters to get a fresh reading for next time this is called.
                ResetCounters();

                return networkData;
            }

            private void ResetCounters()
            {
                lock (m_Counters)
                {
                    m_Counters.Sent = 0;
                    m_Counters.Received = 0;
                }
                m_EtwStartTime = DateTime.Now;
            }

            public void Dispose()
            {
                m_EtwSession?.Dispose();
            }
        }

        public sealed class NetworkPerformanceData
        {
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
        }
    }
}
