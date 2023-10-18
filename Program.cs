using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;

namespace SQLDatabase
{
    class Program
    {
        static void Main()
        {
            string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=PROG260FA23;Integrated Security=True";
            string inputFile = "Produce.txt";
            string outputFile = "ModifiedProduce.txt";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                InsertDataFromFile(connection, inputFile);
                UpdateLocationsAndPrices(connection);
                DeleteItemsPastSellByDate(connection);
                CreateModifiedFile(connection, outputFile);
            }
        }

        static void InsertDataFromFile(SqlConnection connection, string inputFile)
        {
            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(inputFile);

                // Start processing from line 1 (excluding the header)
                for (int lineNumber = 1; lineNumber < lines.Length; lineNumber++)
                {
                    string line = lines[lineNumber];

                    // Split the line into data fields
                    string[] data = line.Split(',');

                    // Check if the line contains the expected number of elements (columns)
                    if (data.Length < 5)
                    {
                        Console.WriteLine($"Error: Invalid data format in line {lineNumber}: {line}");
                        continue;
                    }

                    // Extract and trim data fields
                    string name = data[0].Trim();
                    string location = data[1].Trim();
                    string priceString = data[2].Trim();
                    string uom = data[3].Trim();
                    string dateString = data[4].Trim();

                    // Validate and parse the price
                    decimal price;
                    if (!decimal.TryParse(priceString, NumberStyles.Currency, CultureInfo.InvariantCulture, out price))
                    {
                        Console.WriteLine($"Error: Invalid price format in line {lineNumber}: {line}");
                        continue;
                    }

                    // Validate and parse the date
                    DateTime sellByDate;
                    string[] dateFormats = { "M-dd-yyyy", "MM-dd-yyyy" }; // Handle both formats
                    if (!DateTime.TryParseExact(dateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out sellByDate))
                    {
                        Console.WriteLine($"Error: Invalid date format in line {lineNumber}: {line}");
                        continue;
                    }

                    // Insert data into the 'Produce' table
                    string insertQuery = "INSERT INTO Produce (Name, Location, Price, UoM, SellByDate) VALUES (@Name, @Location, @Price, @UoM, @SellByDate)";
                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Location", location);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@UoM", uom);
                        command.Parameters.AddWithValue("@SellByDate", sellByDate);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Data inserted successfully. Press Enter to exit.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }






        
        static void UpdateLocationsAndPrices(SqlConnection connection)
        {

            // Update Locations with 'F' to 'Z' and Increase Prices by $1
            string updateQuery = "UPDATE Produce SET Location = REPLACE(Location, 'F', 'Z'), Price = Price + 1"; //
            using (SqlCommand command = new SqlCommand(updateQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        static void DeleteItemsPastSellByDate(SqlConnection connection)
        {
            // Delete items that are past their sell-by date
            string deleteQuery = "DELETE FROM Produce WHERE SellByDate < GETDATE()";  //GETDATE() means today and its a function in SQL Server
            using (SqlCommand command = new SqlCommand(deleteQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        static void CreateModifiedFile(SqlConnection connection, string outputFile)
        {
            try
            {
                // Retrieve data from the 'Produce' table and write to the output file
                string selectQuery = "SELECT * FROM Produce";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                using (StreamWriter writer = new StreamWriter(outputFile))
                {
                    writer.WriteLine("Name,Location,Price,UoM,Sell_by_Date"); // Header line

                    while (reader.Read())
                    {
                        string name = reader["Name"].ToString();
                        string location = reader["Location"].ToString();
                        decimal price = decimal.Parse(reader["Price"].ToString());
                        string uom = reader["UoM"].ToString();
                        DateTime sellByDate = DateTime.Parse(reader["SellByDate"].ToString());

                        // Write data to the output file
                        writer.WriteLine($"{name},{location},{price:F2},{uom},{sellByDate:MM-dd-yyyy}");
                    }
                }

                Console.WriteLine($"Modified data written to {outputFile}. Press Enter to exit.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }


    }
}
