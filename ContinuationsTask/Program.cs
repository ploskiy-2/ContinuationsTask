using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ContinuationsTask
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Создаем токен, который будет сообщать об исключениях
            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            Task showSplashTask = new Task(() =>
            {
                Console.WriteLine("ShowSplash process has started...");
                Thread.Sleep(2000);
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
                Console.WriteLine("ShowSplash process has finished.");
            }, token);

            try
            {
                showSplashTask.Start();
              
                if (new Random().Next(10) < 2)
                {
                    cancelTokenSource.Cancel();
                }

                showSplashTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                        Console.WriteLine("This crashed. All next processes canceled");
                }
            }
            finally
            {
                cancelTokenSource.Dispose();
            }
        }
    }
}