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
            // Create token which will report about abolition
            CancellationTokenSource showSplashcancelTokenSource = new CancellationTokenSource();
            CancellationToken showSplashToken = showSplashcancelTokenSource.Token;

            CancellationTokenSource licenseTokenSource = new CancellationTokenSource();
            CancellationToken licenseToken = licenseTokenSource.Token;

            /// Create first process. 
            Task showSplashTask = new Task(() =>
            {
                Console.WriteLine("ShowSplash process has started...");
                Thread.Sleep(2000);
                if (showSplashToken.IsCancellationRequested)
                {
                    showSplashToken.ThrowIfCancellationRequested();
                    Console.WriteLine("This crashed. All next processes canceled");
                }
                Console.WriteLine("ShowSplash process has finished.");
            }, showSplashToken);

            /// Create second process. it is continuation of ShowSplash 
            Task licenseTask = showSplashTask.ContinueWith((showSplashResult) =>
            {             
                if (showSplashTask.IsCanceled || licenseToken.IsCancellationRequested)
                {
                    return;
                }
                
                Console.WriteLine("Requesting license...");
                Thread.Sleep(1000); // Simulate license verification process
                if (licenseToken.IsCancellationRequested)
                {
                    //licenseToken.ThrowIfCancellationRequested();
                    Console.WriteLine("License verification canceled.");
                    Console.WriteLine("No license.");
                }
                else
                {
                    Console.WriteLine("License verified.");
                }
            }, licenseToken);

            try
            {
                /// Start showsplash and it will be interrupted  with a 20% chance 
                showSplashTask.Start();
                
                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    showSplashcancelTokenSource.Cancel();
                }
                showSplashTask.Wait();

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    licenseTokenSource.Cancel();
                }

                Task.WaitAll(licenseTask);

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
                showSplashcancelTokenSource.Dispose();
                licenseTokenSource.Dispose();

            }
        }
    }
}