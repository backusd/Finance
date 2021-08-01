using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
//using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using DataGather.DataBase;
using DataGather.IEXData;
using DataGather.DataTypes;
using DataGather.ErrorHandling;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DataGather
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static int m_sleepTime = 10;
        private static int m_sleepTimeThreadId = -1;
        private static Mutex m_sleepTimeMutex = new Mutex();

        public MainPage()
        {
            this.InitializeComponent();

            ErrorMessages.Initialize();
            DB.Initialize();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.Content = "Working...";

            ThreadPool.QueueUserWorkItem(this.UpdateStocks);

            /*
            IAsyncAction action = ThreadPool.RunAsync(async (workItem) =>
                {
                    List<Stock> stocksInDB = DB.GetStocks();
                    List<Stock> stocksInIEX = await IEX.GetStocks();

                    // If stock is in DB, but not in IEX -> disable it
                    List<string> stockSymbolsInIEX = stocksInIEX.Select(s => s.symbol).ToList();
                    foreach (Stock stockInDB in stocksInDB)
                    {
                        if (!stockSymbolsInIEX.Contains(stockInDB.symbol))
                        {
                            stockInDB.isEnabled = false;
                            stocksInIEX.Add(stockInDB);
                        }
                    }

                    DB.AddUpdateStocks(stocksInIEX);

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                        new DispatchedHandler(() =>
                            {
                                TestButton.Content = "Done";
                            }
                        )
                    );
                }
            );
            */
            
        }

        private async void UpdateStocks(Object args)
        {
            List<Stock> stocksInDB = DB.GetStocks();
            List<Stock> stocksInIEX = await IEX.GetStocks();

            // If stock is in DB, but not in IEX -> disable it
            List<string> stockSymbolsInIEX = stocksInIEX.Select(s => s.symbol).ToList();
            foreach (Stock stockInDB in stocksInDB)
            {
                if (!stockSymbolsInIEX.Contains(stockInDB.symbol))
                {
                    stockInDB.isEnabled = false;
                    stocksInIEX.Add(stockInDB);
                }
            }

            DB.AddUpdateStocks(stocksInIEX);

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                new DispatchedHandler(() =>
                    {
                        TestButton.Content = "Done";
                    }
                )
            );
        }

        private void TestButton2_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Entered");
            /*
            TestButton2.Content = "Working...";

            // Get list of all active common stocks
            List<Stock> stocks = DB.GetActiveCommonStocks();

            // Gather intraday data for each stock
            for (int iii = 0; iii < stocks.Count(); ++iii)
            {
                List<IntradayDataPoint> data = await IEX.GetIntradayData(stocks[iii].symbol);

                // Store data in database using INSERT IGNORE
                if (data != null)
                    DB.AddIgnoreIntradayData(data);


                TestButton2.Content = string.Format("Completed {0} / {1}", iii, stocks.Count());
            }
            */
            ThreadPool.QueueUserWorkItem(this.GatherIntradayData);

        }

        private async void GatherIntradayData(object args)
        {
            // Get list of all active common stocks
            List<Stock> stocks = DB.GetActiveCommonStocks();

            // Gather intraday data for each stock
            for (int iii = 0; iii < stocks.Count(); ++iii)
            {
                ThreadPool.QueueUserWorkItem(this.GatherIntradayDataWorker, stocks[iii]);

                // Only allowed to do 100 per second, so sleep for 10 milliseconds between calls
                Thread.Sleep(m_sleepTime);

                if (iii % 20 == 0)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {
                                TestButton2.Content = string.Format("Completed {0} / {1}", iii, stocks.Count());
                            }
                        )
                    );
                }
            }
        }

        private void DoubleSleepTime()
        {
            m_sleepTimeMutex.WaitOne();

            if (m_sleepTimeThreadId == -1)
            {
                m_sleepTimeThreadId = Thread.CurrentThread.ManagedThreadId;
                m_sleepTime *= 2;
            }
            else if (m_sleepTimeThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                m_sleepTime *= 2;
            }

            m_sleepTimeMutex.ReleaseMutex();
        }

        private void ResetSleepTime()
        {
            if (m_sleepTimeThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                m_sleepTime = 10;
                m_sleepTimeThreadId = -1;
            }
        }

        private async void GatherIntradayDataWorker(object args)
        {
            Stock stock = (Stock)args;

            List<IntradayDataPoint> data = null;

            while(true)
            {
                try
                {
                    data = await IEX.GetIntradayData(stock.symbol);
                    break;
                }
                catch (IEXException ex)
                {
                    if (ex.ToManyCalls())
                    {
                        // double the sleep time until we can successfully get the data
                        DoubleSleepTime();

                        // Sleep and then make request a second time
                        Thread.Sleep(m_sleepTime);
                    }
                    else
                    {
                        // Not sure why an exception was thrown, so just break and let data be null
                        break;
                    }
                }
            }

            // Set the sleep time back to the original once we got the data
            ResetSleepTime();

            // Store data in database using INSERT IGNORE
            if (data != null)
                DB.AddIgnoreIntradayData(data);
        }
    }
}
