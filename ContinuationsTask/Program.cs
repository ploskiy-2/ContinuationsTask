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

            ///this is child token for licenseToken
            CancellationTokenSource setupMenuTokenSource = CancellationTokenSource.CreateLinkedTokenSource(licenseToken);
            CancellationToken setupMenuToken = setupMenuTokenSource.Token;

            ///this is child token for checkforupdateToken
            CancellationTokenSource downloadUpdateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(checkForUpdateToken);
            CancellationToken downloadUpdateToken = downloadUpdateTokenSource.Token;

            ///this is linked token for downloadUpdate and setupMenu
            CancellationTokenSource displayWelcomeScreenTokenSource = CancellationTokenSource.CreateLinkedTokenSource(downloadUpdateToken, setupMenuToken);
            CancellationToken displayWelcomeScreenToken = displayWelcomeScreenTokenSource.Token;


            /// Create start process
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
                await Task.Delay(2000); // Simulate license verification process
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
                await Task.Delay(2000); // Simulate checking updates process
                if (checkForUpdateToken.IsCancellationRequested)
                {
                    Console.WriteLine("Check for update canceled.");
                }
                else
                {
                    Console.WriteLine("Updates were found");
                }
            }, checkForUpdateToken);

            ///create new task which will be child task for licenseTask
            Task setupMenuTask = licenseTask.ContinueWith(async(licenseResult) =>
            {
                if (setupMenuToken.IsCancellationRequested)
                {
                    return;
                }

                Console.WriteLine("Setup menus...");
                await Task.Delay(2000); 
                if (setupMenuToken.IsCancellationRequested)
                {
                    Console.WriteLine("Setup menus canceled.");
                    Console.WriteLine("Server error");
                }
                else
                {
                    Console.WriteLine("Setup menus finished");
                }
            }, setupMenuToken).Unwrap(); 

            ///create new task which will be child task for checkforupdate
            Task downloadUpdateTask = checkForUpdateTask.ContinueWith(async(checkForUpdateResult) =>
            {
                if (downloadUpdateToken.IsCancellationRequested)
                {
                    return;
                }

                Console.WriteLine("Downloading updates....");
                await Task.Delay(2000);
                if (downloadUpdateToken.IsCancellationRequested)
                {
                    Console.WriteLine("Download updates canceled");
                    Console.WriteLine("No rights for this operation");
                }
                else
                {
                    Console.WriteLine("Download updates finished");
                }
            }, downloadUpdateToken).Unwrap();

            ///create second to last linked task 
            Task displayWelcomeScreenTask = new Task(() =>
            {
                if (displayWelcomeScreenToken.IsCancellationRequested)
                {
                    return;
                }
                Thread.Sleep(1000);
                Console.WriteLine("Display welcome screen....");
                Thread.Sleep(1000);
                Console.WriteLine("Welcome screen was displayed");
                
            }, displayWelcomeScreenToken);

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

                
                await Task.WhenAll(checkForUpdateTask, licenseTask);

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    setupMenuTokenSource.Cancel();
                }

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    downloadUpdateTokenSource.Cancel();
                }

                await Task.WhenAll(setupMenuTask, downloadUpdateTask);



                if (!displayWelcomeScreenToken.IsCancellationRequested)
                {
                   
                    displayWelcomeScreenTask.Start();
                    displayWelcomeScreenTask.Wait();
                }



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
                setupMenuTokenSource.Dispose();
                downloadUpdateTokenSource.Dispose();
                displayWelcomeScreenTokenSource.Dispose();
            }
        }
    }
}