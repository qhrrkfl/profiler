using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Profiler;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int iProcessId = int.Parse(Console.ReadLine());
            var p = Profiler.ProcessMonitoring.NetworkPerformanceReporter.Create(iProcessId);
            while (true)
            {
                Thread.Sleep(5000);
                long re = p.GetNetworkPerformanceData().BytesReceived;
                Console.WriteLine(re);




            }

        }
    }
}
