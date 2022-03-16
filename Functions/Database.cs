using System;
using Discord;
using Discord.WebSocket;
using MySql.Data.MySqlClient;

namespace SlickReship_Payments.Functions
{
    internal class Database
    {
        internal static string GetStripeId(ulong id)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

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
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

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