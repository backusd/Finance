using BucketStrategy.DataBase;
using BucketStrategy.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BucketStrategy
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateStrategyPage : Page
    {
        private ObservableCollection<Stock> m_stocks;

        public CreateStrategyPage()
        {
            this.InitializeComponent();

            m_stocks = new ObservableCollection<Stock>();
        }

        private void AddStockButton_Click(object sender, RoutedEventArgs e)
        {
            AddStock();
        }

        private void RemoveStockButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)e.OriginalSource;
            Grid parent = (Grid)button.Parent;
            TextBlock textBlock = (TextBlock)parent.Children[0];
            string symbol = textBlock.Text;

            m_stocks.Remove(m_stocks.First(s => s.symbol == symbol));
        }

        private void AddStockTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddStock();
            }
        }

        private void AddStock()
        {
            // Clear any error text that may exist
            ErrorTextBlock.Text = "";

            // Make sure the symbol is all uppercase
            string symbol = AddStockTextBox.Text.ToUpper();

            // Make sure the symbol was not already previously added
            List<string> symbols = m_stocks.Select(s => s.symbol).ToList();
            if (!symbols.Contains(symbol))
            {
                // try to get the stock for the symbol
                try
                {
                    Stock stock = DB.GetStock(symbol);
                    m_stocks.Add(stock);
                }
                catch (Exception ex)
                {
                    ErrorTextBlock.Text = "Error: " + ex.Message;
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear any error text that may exist
            ErrorTextBlock.Text = "";

            // Initialize all input variables
            string name = DistributionNameTextBox.Text;

            TimeUnit timeUnit;
            string tag = ((ComboBoxItem)TimeUnitComboBox.SelectedItem).Tag.ToString();
            switch (tag)
            {
                case "minute":  timeUnit = TimeUnit.MINUTE; break;
                case "day":     timeUnit = TimeUnit.DAY; break;
                default:        timeUnit = TimeUnit.DAY; break;
            }

            OHLCOptions ohlcOptions = new OHLCOptions();
            ohlcOptions.Open = (bool)OpenCheckBox.IsChecked;
            ohlcOptions.High = (bool)HighCheckBox.IsChecked;
            ohlcOptions.Low = (bool)LowCheckBox.IsChecked;
            ohlcOptions.Close = (bool)CloseCheckBox.IsChecked;

            DateTime minDate = new DateTime(), maxDate = new DateTime();
            DateTimeOffset min = MinDateDatePicker.Date;
            DateTimeOffset max = MaxDateDatePicker.Date;
            TimeSpan tmin = MinDateTimePicker.Time;
            TimeSpan tmax = MaxDateTimePicker.Time;
            switch (timeUnit)
            {
                case TimeUnit.DAY:    
                    minDate = new DateTime(min.Year, min.Month, min.Day);
                    maxDate = new DateTime(max.Year, max.Month, max.Day);
                    break;

                case TimeUnit.MINUTE: 
                    minDate = new DateTime(min.Year, min.Month, min.Day, tmin.Hours, tmin.Minutes, tmin.Seconds);
                    maxDate = new DateTime(max.Year, max.Month, max.Day, tmax.Hours, tmax.Minutes, tmax.Seconds);
                    break;
            }

            int windowSize = (int)WindowSizeNumberBox.Value;

            int returnRange = (int)ReturnRangeNumberBox.Value;

            int numberOfBuckets = (int)NumberOfBucketsNumberBox.Value;

            NormalizationMethod normalizationMethod;
            string ntag = ((ComboBoxItem)NormalizationMethodComboBox.SelectedItem).Tag.ToString();
            switch(ntag)
            {
                case "dividebymax": normalizationMethod = NormalizationMethod.DIVIDE_BY_MAX; break;
                case "slopes":      normalizationMethod = NormalizationMethod.SLOPES; break;
                default:            normalizationMethod = NormalizationMethod.DIVIDE_BY_MAX; break;
            }


            // Do basic checking to make sure inputs are at least valid types
            // 
            // Distribution name must be non-empty
            if (name == "")
            {
                ErrorTextBlock.Text = "Distribution name cannot be empty";
                return;
            }

            // Stock list cannot be empty
            if (m_stocks.Count == 0)
            {
                ErrorTextBlock.Text = "Stock list cannot be empty";
                return;
            }

            // At least one OHLC option must be selected
            if (!(ohlcOptions.Open || ohlcOptions.High || ohlcOptions.Low || ohlcOptions.Close))
            {
                ErrorTextBlock.Text = "At least one OHLC option must be selected";
                return;
            }

            // Min date must be more recent than 1950
            if (minDate.Year < 1950)
            {
                ErrorTextBlock.Text = "Min date must be more recent than 1950";
                return;
            }

            // Max date must be more recent than 1950
            if (maxDate.Year < 1950)
            {
                ErrorTextBlock.Text = "Max date must be more recent than 1950";
                return;
            }

            // Min date must be less than max date
            if (minDate >= maxDate)
            {
                ErrorTextBlock.Text = "Min date must be less than max date";
                return;
            }

            // Window size must be greater than 0
            if (windowSize <= 0)
            {
                ErrorTextBlock.Text = "Window size must be greater than 0";
                return;
            }

            // Return range must be greater than 0
            if (returnRange <= 0)
            {
                ErrorTextBlock.Text = "Return range must be greater than 0";
                return;
            }

            // Number of buckets must be greater than 0
            if (numberOfBuckets <= 0)
            {
                ErrorTextBlock.Text = "Number of buckets must be greater than 0";
                return;
            }


            // Basic type checking is done
            //
            // Start loading ring
            MainGrid.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Visible;





            // Initialize a CreateDistribution object so the data can be passed to a worker thread
            Distribution dist = new Distribution(name, m_stocks.ToList(), timeUnit, ohlcOptions, minDate, maxDate,
                                                    windowSize, returnRange, numberOfBuckets, normalizationMethod);

            // Start creating strategy on a worker thread
            ThreadPool.QueueUserWorkItem(CreateDistribution, dist);
        }

        private async void CreateDistribution(object args)
        {
            Distribution dist = (Distribution)args;
            string message = "";

            try
            {
                dist.CreateBuckets();
                message = "Success!";
            }
            catch (Exception ex)
            {
                message = string.Format("Creating Buckets failed with message: {0}", ex.Message);
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                    new DispatchedHandler(() =>
                    {
                        LoadingRing.Visibility = Visibility.Collapsed;
                        LoadingStackPanel.Visibility = Visibility.Visible;

                        LoadingErrorMessageTextBlock.Text = message;

                    }
                )
            );
        }

        private void LoadingBackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }

        private void TimeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MinDateTimePicker != null)
            {
                string tag = ((ComboBoxItem)TimeUnitComboBox.SelectedItem).Tag.ToString();

                switch (tag)
                {
                    case "minute":
                        MinDateTimePicker.Visibility = Visibility.Visible;
                        MaxDateTimePicker.Visibility = Visibility.Visible;
                        break;

                    case "day":
                        MinDateTimePicker.Visibility = Visibility.Collapsed;
                        MaxDateTimePicker.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }
    }
}
