using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

using DataGather.DataTypes;
using DataGather.ErrorHandling;

namespace DataGather.DataBase
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
                ErrorMessages.AddErrorMessage("DB.ExecuteNonQuery: Caught exception with message: " + ex.Message);
                ErrorMessages.AddErrorMessage("               sql: " + sql);
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
                    dp.symbol,
                    dp.date.ToString("yyyy-MM-dd HH:mm:ss"),
                    dp.open,
                    dp.high,
                    dp.low,
                    dp.close,
                    dp.volume,
                    dp.average,
                    dp.notional,
                    dp.numberOfTrades
                );
            }

            sql = sql.TrimEnd(',') + ";";

            ExecuteNonQuery(ref sql);
        }




        public static List<Stock> GetStocks()
        {
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
                        reader[0].ToString(),
                        reader[1].ToString(),
                        DateTime.Parse(reader[2].ToString()),
                        reader[3].ToString(),
                        reader[4].ToString(),
                        reader[5].ToString(),
                        reader[6].ToString(),
                        reader[7].ToString() == "True",
                        reader[8].ToString(),
                        reader[9].ToString()
                    ));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ErrorMessages.AddErrorMessage("DB.GetStocks: Caught exception with message: " + ex.Message);
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();
            }

            return stocks;
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
                        reader[0].ToString(),
                        reader[1].ToString(),
                        DateTime.Parse(reader[2].ToString()),
                        reader[3].ToString(),
                        reader[4].ToString(),
                        reader[5].ToString(),
                        reader[6].ToString(),
                        reader[7].ToString() == "True",
                        reader[8].ToString(),
                        reader[9].ToString()
                    ));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ErrorMessages.AddErrorMessage("DB.GetStocks: Caught exception with message: " + ex.Message);
            }
            finally
            {
                m_connection.Close();
                mutex.ReleaseMutex();
            }

            return stocks;
        }


    }
}
