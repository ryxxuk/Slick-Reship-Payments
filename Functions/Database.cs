using System;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace SlickReship_Payments.Functions
{
    public static class Database
    {
        public static JObject _config;

        internal static string GetStripeId(ulong id)
        {
            var connectionStr = _config["db_connection"].Value<string>(); ;

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM connected_accounts WHERE discord_id=@discord_id", conn);
                checkCmd.Parameters.AddWithValue("@discord_id", id);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return "";

                while (reader.Read()) return reader["stripe_id"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return "";
        }

        public static bool AddStripeAccount(IUser user, string stripeId)
        {
            var connectionStr = _config["db_connection"].Value<string>();

            using var conn = new MySqlConnection(connectionStr);

            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand(
                        "INSERT connected_accounts(stripe_id, discord_id) VALUES (@stripe_id, @discord_id)",
                        conn);
                checkCmd.Parameters.AddWithValue("@stripe_id", stripeId);
                checkCmd.Parameters.AddWithValue("@discord_id", user.Id);
                checkCmd.Prepare();

                checkCmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}