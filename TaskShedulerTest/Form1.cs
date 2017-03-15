using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tasks;

namespace TaskSchedulerTest
{
    public partial class MyForm : Form
    {
        private readonly TaskScheduler m_syncContextTaskSheduler;
        
        public MyForm()
        {
            m_syncContextTaskSheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Text = "Synchronization Context Task Sheduler Demo";
            Visible = true; Width = 600; Height = 100;
            //InitializeComponent();
        }

        //
        private CancellationTokenSource m_cts;
        public static int SumWithCancel(CancellationToken token, int n)
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
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (m_cts != null)//Операция выполняется, отменяем ее
            {
                m_cts.Cancel();
                m_cts = null;
            }
            else //Операция не выполняется, запускаем ее
            {
                Text = "Oepration running";
                m_cts = new CancellationTokenSource();

                //Это задание использует расписание заданий по умолчанию, и использует пул потоков.
                Task<int> t = Task.Run(()=>SumWithCancel(m_cts.Token,20000),m_cts.Token);

                //Эти Таски используют расписание sync context, и исполняются на потоке GUI.
                t.ContinueWith(task => Text = "Result: " + task.Result,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                    m_syncContextTaskSheduler);

                t.ContinueWith(task=>Text="Operation Canceled",
                    CancellationToken.None,TaskContinuationOptions.OnlyOnCanceled,
                    m_syncContextTaskSheduler);

                t.ContinueWith(task=>Text="Operation Faulted",
                    CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,
                    m_syncContextTaskSheduler);
            }

            base.OnMouseClick(e);
        }
    }
     
}
