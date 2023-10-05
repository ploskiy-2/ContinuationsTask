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

            ///created dependent tokens 
            ///(if showSplashToken is canceled, then the child tokens will also
            ///be canceled immediately, without calling the task)
            CancellationTokenSource licenseTokenSource = CancellationTokenSource.CreateLinkedTokenSource(showSplashToken);
            CancellationToken licenseToken = licenseTokenSource.Token;

            CancellationTokenSource updateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(showSplashToken);
            CancellationToken checkForUpdateToken = updateTokenSource.Token;

            
            
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
            Task licenseTask = showSplashTask.ContinueWith(async(showSplashResult) =>
            {             
                if (licenseToken.IsCancellationRequested)
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

            /// Create third process which works async with licenseTask
            Task checkForUpdateTask = showSplashTask.ContinueWith(async(showSplashResult) =>
            {
                if (checkForUpdateToken.IsCancellationRequested)
                {
                    return;
                }

                Console.WriteLine("Checking updates...");
                Thread.Sleep(1000); // Simulate checking updates process
                if (checkForUpdateToken.IsCancellationRequested)
                {
                    Console.WriteLine("Check for update canceled.");
                    Console.WriteLine("Download updates");
                }
                else
                {
                    Console.WriteLine("All updates downloaded");
                }
            }, checkForUpdateToken);


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


                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    updateTokenSource.Cancel();
                }

                await Task.WhenAll(licenseTask, checkForUpdateTask);

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
                updateTokenSource.Dispose();

            }
        }
    }
}