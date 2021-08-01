using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

using BucketStrategy.DataTypes;

namespace BucketStrategy.DataBase
{
    public static class DB
    {
        private const string m_connectionString = "server=localhost;user=root;database=finance;port=3306;password=only1mom";
        private static MySqlConnection m_connection;
        private static Mutex mutex = new Mutex();

        public static void Initialize()
        {
            m_connection = new MySqlConnection(m_connectionString);
        }

        private static void ExecuteNonQuery(ref string sql)
        {
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExecuteNonQuery Exception: " + ex.Message);
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();
            }
        }
        public static void AddUpdateStocks(List<Stock> stocks)
        {
            string sql = "INSERT INTO stocks (`symbol`, `name`, `date`, `type`, `iexId`, `region`, `currency`, `isEnabled`, `figi`, `cik`) VALUES";
            
            foreach(Stock stock in stocks)
            {
                sql += string.Format(" ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', {7}, '{8}', '{9}'),",
                    stock.symbol,
                    stock.name,
                    stock.date.ToString("yyyy-MM-dd"),
                    stock.type,
                    stock.iexId,
                    stock.region,
                    stock.currency,
                    stock.isEnabled ? "true" : "false",
                    stock.figi,
                    stock.cik
                );
            }

            sql = sql.TrimEnd(',') + " AS inputs ON DUPLICATE KEY UPDATE `name` = inputs.name, `date` = inputs.date, `type` = inputs.type, `iexId` = inputs.iexId, `region` = inputs.region, `currency` = inputs.currency, `isEnabled` = inputs.isEnabled, `figi` = inputs.figi, `cik` = inputs.cik;";

            ExecuteNonQuery(ref sql);
        }
        public static void AddIgnoreIntradayData(List<IntradayDataPoint> data)
        {
            string sql = "INSERT IGNORE INTO daily_data (`symbol`, `datetime`, `open`, `high`, `low`, `close`, `volume`, `average`, `notional`, `numberOfTrades`) VALUES";

            foreach (IntradayDataPoint dp in data)
            {
                sql += string.Format(" ('{0}', '{1}', {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}),",
                    dp.Symbol,
                    dp.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    dp.Open,
                    dp.High,
                    dp.Low,
                    dp.Close,
                    dp.Volume,
                    dp.Average,
                    dp.Notional,
                    dp.NumberOfTrades
                );
            }

            sql = sql.TrimEnd(',') + ";";

            ExecuteNonQuery(ref sql);
        }




        public static List<Stock> GetStocks()
        {
            string message = "";
            List<Stock> stocks = new List<Stock>();
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = "SELECT symbol, name, date, type, iexId, region, currency, isEnabled, figi, cik FROM stocks;";
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    stocks.Add(new Stock(
                        reader.GetString(0),
                        reader.GetString(1),
                        DateTime.Parse(reader.GetString(2)),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        reader.GetString(7) == "True",
                        reader.GetString(8),
                        reader.GetString(9)
                    ));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();

                if (message != "")
                    throw new Exception(message);
            }

            return stocks;
        }
        public static Stock GetStock(string symbol)
        {
            bool throwException = false;
            string message = "";
            Stock stock = null;
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = string.Format("SELECT symbol, name, date, type, iexId, region, currency, isEnabled, figi, cik FROM stocks WHERE `symbol` = '{0}';", symbol);
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    stock = new Stock(
                        reader.GetString(0),
                        reader.GetString(1),
                        DateTime.Parse(reader.GetString(2)),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        reader.GetString(7) == "True",
                        reader.GetString(8),
                        reader.GetString(9)
                    );
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                throwException = true;
                message = ex.Message;
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();

                if (throwException)
                    throw new Exception(message);
            }

            // If no data was found, throw an exception
            if (stock == null)
                throw new Exception(string.Format("DB.GetStock - Obtained no results for symbol '{0}'", symbol));
            
            if (stock.type != "cs")
                throw new Exception(string.Format("DB.GetStock - Stock '{0}' is not of type 'cs'", symbol));

            if (!stock.isEnabled)
                throw new Exception(string.Format("DB.GetStock - Stock '{0}' is not enabled", symbol));

            return stock;
        }
        public static List<Stock> GetActiveCommonStocks()
        {
            List<Stock> stocks = new List<Stock>();

            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = "SELECT symbol, name, date, type, iexId, region, currency, isEnabled, figi, cik FROM stocks WHERE type = 'cs' AND isEnabled = true;";
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    stocks.Add(new Stock(
                        reader.GetString(0),
                        reader.GetString(1),
                        DateTime.Parse(reader.GetString(2)),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        reader.GetString(7) == "True",
                        reader.GetString(8),
                        reader.GetString(9)
                    ));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetActiveCommonStocks Exception: " + ex.Message);
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();
            }

            return stocks;
        }
        public static Tuple<DateTime, DateTime> GetStockMinMaxDates(Stock stock, TimeUnit timeUnit)
        {
            string message = "";
            Tuple<DateTime, DateTime> tup = null;
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = string.Format("SELECT MIN(`datetime`), MAX(`datetime`) FROM daily_data WHERE `symbol` = '{0}';", stock.symbol);
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    tup = new Tuple<DateTime, DateTime>(reader.GetDateTime(0), reader.GetDateTime(1));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();

                if (message != "")
                    throw new Exception(message);
            }

            return tup;
        }
        public static List<IntradayDataPoint> GetIntradayData(Stock stock, DateTime minDate, DateTime maxDate)
        {
            string message = "";
            List<IntradayDataPoint> data = new List<IntradayDataPoint>();
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = string.Format("SELECT `symbol`, `datetime`, `open`, `high`, `low`, `close`, `average`, `volume`, `notional`, `numberOfTrades` FROM daily_data WHERE `symbol` = '{0}' AND `datetime` >= '{1}' AND `datetime` <= '{2}';", stock.symbol, minDate.ToString("yyyy-MM-dd HH:mm:ss"), maxDate.ToString("yyyy-MM-dd HH:mm:ss"));
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new IntradayDataPoint(
                        reader.GetString(0),
                        reader.GetDateTime(1),
                        reader.GetFloat(2),
                        reader.GetFloat(3),
                        reader.GetFloat(4),
                        reader.GetFloat(5),
                        reader.GetFloat(6),
                        reader.GetInt32(7),
                        reader.GetFloat(8),
                        reader.GetInt32(9)
                    ));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();

                if (message != "")
                    throw new Exception(message);
            }

            return data;
        }

        public static bool DistributionExists(string name)
        {
            string message = "";
            bool found = false;
            try
            {
                mutex.WaitOne();
                m_connection.Open();
                string sql = string.Format("SELECT name FROM distribution WHERE `name` = '{0}';", name);
                MySqlCommand command = new MySqlCommand(sql, m_connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    found = true;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();

                if (message != "")
                    throw new Exception(message);
            }

            return found;
        }


    }
}
