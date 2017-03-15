using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronous_Operations
{
    
    class Program
    {
        
        static void Main(string[] args)
        {
            int a = 5;
            Console.WriteLine("Main thread: queuing an asynchronous operation");
            ThreadPool.QueueUserWorkItem(ComputeBoundOp, a);
            Console.WriteLine("Main thread: Doing other work here...");
            Thread.Sleep(10000);
            Console.WriteLine("Hit <Enter> to end this program...");
            //Console.ReadLine();
        }

        private static void ComputeBoundOp(object state)
        {
            int temp = (int)state;
            Console.WriteLine("In ComputeBoundOp: State: {0}...",state);
            Thread.Sleep(1000);
        }
    }
}
