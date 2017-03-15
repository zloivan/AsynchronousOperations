using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace CancellationDemo
{
    class CancellationDemo
    {
        static void Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            //Передаем операции CancellationToken и число
            ThreadPool.QueueUserWorkItem(o=>Count(cts.Token,50));
            
            //Регестрируем методы которые вызовутся при отмене выполнения метода Count
            cts.Token.Register(() => Console.WriteLine("Canceled 1"));
            cts.Token.Register(() => Console.WriteLine("Canceled 2"));
            Console.WriteLine("Press <Enter> to cancel the operation.");
            Console.ReadLine();
            cts.Cancel();///Если метод Count уже вернул управления,
                         ///cancel не оказывает никакого эффекта.
            //Cancel немедленно возвращает управление, метод продолжает работу...
            Console.ReadLine();

           

            #region Linked from other cancellation objects object.
            //Создаем ConcellationtokenSource
            var cts1 = new CancellationTokenSource();
            cts1.Token.Register(()=>Console.WriteLine("cts1 canceled"));

            //Создаем еще один ConcellationTokenSource
            var cts2 = new CancellationTokenSource();
            cts2.Token.Register(()=>Console.WriteLine("cts2 canceled"));

            //Создаем связанный с обоими предидущими рбьектами CTS, который отменяется когда отменяется любой из них.
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token,cts2.Token);
            linkedCts.Token.Register(()=>Console.WriteLine("linkedCTS canceled"));
            
            //Отменяем один из CTS обьектов. в нашем случае cts2.
            cts2.Cancel();
            Console.WriteLine("cts1 cancled={0}, cts2 cancled={1}, linkedCTS canceled={2}",
                cts1.IsCancellationRequested,cts2.IsCancellationRequested,linkedCts.IsCancellationRequested);
            #endregion
            Console.WriteLine("Press any key to start count");
            Console.ReadLine();



            var delay = new TimeSpan(0, 0, 10);
            var ctsTimer = new CancellationTokenSource(delay);
            ctsTimer.Token.Register(() => Console.WriteLine("Operation canced"));
            ThreadPool.QueueUserWorkItem(i => Count(ctsTimer.Token, 100));

            Console.ReadLine();
            //ctsTimer.Cancel();

    
        
        }
        private static void Count(CancellationToken token, int countTo)
        {
            for (int count = 0; count < countTo; count++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Count is cancelled.");
                    break;//Выходим из цыкла что бы прекратить операцию
                }
                Console.WriteLine(count);
                
                Thread.Sleep(200);
            }
            Console.WriteLine("Count is done.");
        }
    }
}
