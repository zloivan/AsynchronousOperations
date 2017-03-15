using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tasks
{
    public class TaskTest
    {
        /// <summary>
        /// Здесь рассмастривается работа threading.task, cancellationtoken,
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            
           //Создаем задание 
            Task<int> t = new Task<int> (n=>Sum((int)n),1000000000 );
            
            //задание можно начать в любой момент (начнем сейчас) 
            t.Start();
            
            //по выбору можно явно дождатся выполнения задания
            t.Wait();

           
            //можно получить результат нашей выч операции SUM, свойство result внутри себя вызывает Wait
            Console.WriteLine("The sum is: "+t.Result);
            
            ///При построении выведется ошибка так как сумма будет больше чем вмещает в себя int
            Console.WriteLine("Press any key to go farward in your appliocation.");

            Console.ReadLine();

            #region CancellationTokenSource
            CancellationTokenSource cts = new CancellationTokenSource();
            
            Task<int> taskWithCancel = new Task<int>(()=>SumWithCancel(cts.Token,10000),cts.Token);
            taskWithCancel.Start();
            
            //Позднее отменим CancellationTokenSource, чтобы отменить Task
            cts.Cancel();

            try
            {
                //в случае отмены задания метод Result генерирует исключение AggregateExceptiuon
                Console.WriteLine("The sum is: " + taskWithCancel.Result);//значение int32
            }
            catch(AggregateException x)
            {
                ///Считаем обработанными все объекты OperationCanceledException
                ///все остальные обьекты попадают в новый обьект AggregateException,
                ///состоящий только из необработанных исключений.
                x.Handle(e=>e is OperationCanceledException);
                
                //Строка выполняется если все исключения уже обработаны
                Console.WriteLine("Sum was canceled");
            }
            Console.ReadLine();
            #endregion
            #region ContinueWith
            
            //Создаем обьект Task с отложенным запуском.
            Task<int> continuetask = Task.Run(()=>
                SumWithCancel(CancellationToken.None,10000));
            
            //Метод ContinueWith возвращает обьект Task, но обычно в этом нет необходимости.
            Task cwt = continuetask.ContinueWith(task=>
                Console.WriteLine("The sum is: "+task.Result));
            
            //Этот цикл демонстрирует вывод сообщения из Task, автоматического Task.
            for (int i = 0; i < 50; i++)
            {
                Console.WriteLine(i);
                Thread.Sleep(200);
            }
            #endregion
            Console.ReadLine();

            #region ParentChildTasks
            Task<int[]> parent = new Task<int[]>(() => {
                var result = new int[3];//создаем массив для результатов
                //Создание и запуск 3 дочерних Task
                new Task(()=>result[0]=Sum(10000),
                    TaskCreationOptions.AttachedToParent).Start();
                new Task(() => result[1] = Sum(20000),
                    TaskCreationOptions.AttachedToParent).Start();
                new Task(() => result[2] = Sum(30000),
                    TaskCreationOptions.AttachedToParent).Start();
                ///Возвращается ссылка на массив, элементы
                ///могут быть не инициализированны
                return result;
            });
            //Вывод результатов после завершения родительского и дочерних заданий
            var cwt2 = parent.ContinueWith(
                parentTask=>Array.ForEach(parentTask.Result,Console.WriteLine));

            parent.Start();
            Console.ReadLine();
            #endregion
            Console.WriteLine("Starting the TaskFactory testing");
            #region TaskFactory
            Task Parant1 = new Task(() => 
            {
                var cts2 = new CancellationTokenSource();
                var tf2 = new TaskFactory<int>(cts2.Token,
                    TaskCreationOptions.AttachedToParent,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                var childTasks = new[]
                {
                    tf2.StartNew(()=>SumWithCancel(cts2.Token,10000)),
                    tf2.StartNew(()=>SumWithCancel(cts2.Token,20000)),
                    tf2.StartNew(()=>SumWithCancel(cts2.Token,Int32.MaxValue))//Исключение overflowException
                };
            
                //Если дочернее задание становится источником исключения отменяем все доч задания
                for (int task = 0; task < childTasks.Length; task++)
                
                    childTasks[task].ContinueWith(t2 => cts2.Cancel(), 
                        TaskContinuationOptions.OnlyOnFaulted);
                        ///После завершения дочерних заданий получаем максимальное 
                        ///возвращаемое значение и передаем его другому заданию для вывода

                    tf2.ContinueWhenAll(
                        childTasks,
                        completedTasks => completedTasks.Where(
                            t2 => !t2.IsFaulted && !t2.IsCanceled).Max(t2 => t2.Result),
                            CancellationToken.None).
                            ContinueWith(t2 => Console.WriteLine("The maximum is: " + t2.Result),
                            TaskContinuationOptions.ExecuteSynchronously);
            });
            
            //После завершения дочерних заданий выводим, в том числе, и
            //необработанные исключения.
            Parant1.ContinueWith(p =>
                {
                    ///Текст помещен в StringBuilder и однократно вызван
                    ///метод Console.Writeline просто потому, что это задание
                    ///может выполнятся паралелльно с предыдущим,
                    ///и я не хочу путаницы в выводимом результате.
                    StringBuilder sb = new StringBuilder("The following exception(s) ocuurred: " +
                        Environment.NewLine);
                    foreach (var e in p.Exception.Flatten().InnerExceptions)
                        sb.AppendLine(" " + e.GetType().ToString());
                    Console.WriteLine(sb.ToString());
                }, TaskContinuationOptions.OnlyOnFaulted);
            
            //Запуск родительского задания, которое может заупстить дочерние.
            Parant1.Start();
            #endregion
            Console.ReadLine();
        }


        public static int SumWithCancel(CancellationToken token,int n)
        {

            int sum = 0;
            for (; n > 0; n--)
            {
                ///Следующая строка приводит к исключению OperationCanceledException
                ///при вызове метода Cancel для обьекта CancellationTokenSource,
                ///на который ссылается маркер.
                token.ThrowIfCancellationRequested();
                checked { sum += n; }//при больших n выдается system.overflowexception

            }
            Thread.Sleep(1000);
            return sum;
        }
        public static int Sum(int n)
        {
            int sum = 0;
            for (; n > 0; n--)
            {
                unchecked { sum += n; }//при больших n выдается system.overflowexception
               
            }
            return sum;
        }
    }
    
}
