using IronXL;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;

namespace SQLiteTest
{
    class Program
    {
        static int selectionLimit = 3;
        static int maxTeamCost = 100000000;
        static int maxTeamMembers = 15;
        private static void Main(string[] args)
        {
            Console.WriteLine("Good day");
            bool createDB = false;
            bool createDummy = false;
            bool createUserData = false;
            while (true)
            {
                Console.WriteLine("Recreate database with dummy data? Yes or No.");
                string input = Console.ReadLine() ?? "";
                if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete("database.db");
                    createDB = true;
                    createDummy = true;
                    break;
                }
                else if (input.ToLower() == "no")
                {
                    while (true)
                    {
                        Console.WriteLine("Recreate database with your data? Yes or No.");
                        input = Console.ReadLine() ?? "";
                        if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete("database.db");
                            createDB = true;
                            createUserData = true;
                            break;
                        }
                        else if (input.ToLower() == "no")
                        {
                            break;
                        }
                    }
                    break;
                }
            }

            using SQLiteConnection connection = createConnection();
            if (createDB || createUserData)
            {
                createTables(connection);
            }
            if (createDummy)
            {
                insertDummyData(connection);
            }
            if (createUserData)
            {
                string input = "";
                Console.WriteLine("Recreate players with your data? Yes or No.");
                input = Console.ReadLine() ?? "";
                if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    readInUserPlayersFile(connection);
                }

                Console.WriteLine("Recreate clubs with your data? Yes or No.");
                input = Console.ReadLine() ?? "";
                if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    readInUserClubsFile(connection);
                }

                Console.WriteLine("Recreate which club players are in with your data? Yes or No.");
                input = Console.ReadLine() ?? "";
                if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    readInUserClubPlayersFile(connection);
                }
            }

            while (true)
            {
                Console.WriteLine("");
                Console.WriteLine("Please select what you want to do.");
                Console.WriteLine("1 : View Current Selected Team");
                Console.WriteLine("2 : View Players");
                Console.WriteLine("3 : View Clubs");
                Console.WriteLine("4 : Add Player To Team From Player List");
                Console.WriteLine("5 : Delete Player From Team");
                Console.WriteLine("Exit : Exit app");

                string input = Console.ReadLine() ?? "";
                switch (input.ToLower())
                {
                    case "1":
                        viewTeam(connection);
                        break;
                    case "2":
                        viewPlayers(connection);
                        break;
                    case "3":
                        viewClubs(connection);
                        break;
                    case "4":
                        addPlayerFromPlayersList(connection);
                        break;
                    case "5":
                        deletePlayerFromTeamList(connection);
                        break;
                    case "exit":
                        return;
                }
            }
        }
        private static SQLiteConnection createConnection()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=database.db;Version=3;New=True;Compress=True;");
            try
            {
                connection.Open();
                Console.WriteLine("Connected");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return connection;
        }
        private static void readInUserPlayersFile(SQLiteConnection connection)
        {
            Console.WriteLine("");
            Console.WriteLine("Please provide the file path to you Players file, accepted file types (.xlsx,.xls,.csv) ");
            string filePath = Console.ReadLine() ?? "";
            if (filePath.Length > 0)
            {
                string fileExt = System.IO.Path.GetExtension(filePath);
                if (fileExt == ".xlsx" || fileExt == ".xls" || fileExt == ".csv")
                {
                    WorkBook workBook = WorkBook.Load(filePath);
                    WorkSheet workSheet = workBook.WorkSheets.First();
                    int playerNameCol = 0;
                    int playerPointsCol = 1;
                    int playerPriceCol = 2;
                    var first = workSheet["A1:Z1"].Rows[0];

                    if (first.Columns.Any(a => a.Value.ToString() == "PlayerName"))
                    {
                        playerNameCol = first.Columns.Where(w => w.Value.ToString() == "PlayerName").Select(c => c.ColumnNumber).FirstOrDefault();
                    }
                    if (first.Columns.Any(a => a.Value.ToString() == "Points"))
                    {
                        playerPointsCol = first.Columns.Where(w => w.Value.ToString() == "Points").Select(c => c.ColumnNumber).FirstOrDefault();
                    }
                    if (first.Columns.Any(a => a.Value.ToString() == "Price"))
                    {
                        playerPriceCol = first.Columns.Where(w => w.Value.ToString() == "Price").Select(c => c.ColumnNumber).FirstOrDefault();
                    }
                    foreach (RangeRow row in workSheet.Rows)
                    {
                        if (row.Columns[playerNameCol].Value.ToString() == "PlayerName" || row.Columns[playerPointsCol].Value.ToString() == "Points" || row.Columns[playerPriceCol].Value.ToString() == "Price")
                        {
                            continue;
                        }
                        Console.WriteLine("PlayerName '{0}' has {1} points and costs: {2}", row.Columns[playerNameCol], row.Columns[playerPointsCol], row.Columns[playerPriceCol]);
                        insertPlayer(connection, row.Columns[playerNameCol].Value.ToString(), Convert.ToDouble(row.Columns[playerPointsCol].Value), Convert.ToInt32(row.Columns[playerPriceCol].Value));
                    }
                }
            }
        }
        private static void readInUserClubsFile(SQLiteConnection connection)
        {
            Console.WriteLine("");
            Console.WriteLine("Please provide the file path to you Clubs file, accepted file types (.xlsx,.xls,.csv) ");
            string filePath = Console.ReadLine() ?? "";
            if (filePath.Length > 0)
            {
                string fileExt = System.IO.Path.GetExtension(filePath);
                if (fileExt == ".xlsx" || fileExt == ".xls" || fileExt == ".csv")
                {
                    WorkBook workBook = WorkBook.Load(filePath);
                    WorkSheet workSheet = workBook.WorkSheets.First();
                    int clubNameCol = 0;
                    var first = workSheet["A1:Z1"].Rows[0];

                    if (first.Columns.Any(a => a.Value.ToString() == "ClubName"))
                    {
                        clubNameCol = first.Columns.Where(w => w.Value.ToString() == "ClubName").Select(c => c.ColumnNumber).FirstOrDefault();
                    }
                    foreach (RangeRow row in workSheet.Rows)
                    {
                        if (row.Columns[clubNameCol].Value.ToString() == "ClubName")
                        {
                            continue;
                        }
                        Console.WriteLine("ClubName '{0}'", row.Columns[clubNameCol]);
                        insertClub(connection, row.Columns[clubNameCol].Value.ToString());
                    }
                }
            }
        }
        private static void readInUserCluybPlayersFile(SQLiteConnection connection)
        {
            Console.WriteLine("");
            Console.WriteLine("Please provide the file path to you Club Players file, accepted file types (.xlsx,.xls,.csv) ");
            string filePath = Console.ReadLine() ?? "";
            if (filePath.Length > 0)
            {
                string fileExt = System.IO.Path.GetExtension(filePath);
                if (fileExt == ".xlsx" || fileExt == ".xls" || fileExt == ".csv")
                {
                    WorkBook workBook = WorkBook.Load(filePath);
                    WorkSheet workSheet = workBook.WorkSheets.First();
                    int playerNameCol = 0;
                    int clubNameCol = 1;
                    var first = workSheet["A1:Z1"].Rows[0];

                    if (first.Columns.Any(a => a.Value.ToString() == "PlayerName"))
                    {
                        playerNameCol = first.Columns.Where(w => w.Value.ToString() == "PlayerName").Select(c => c.ColumnNumber).FirstOrDefault();
                    }
                    if (first.Columns.Any(a => a.Value.ToString() == "ClubName"))
                    {
                        clubNameCol = first.Columns.Where(w => w.Value.ToString() == "ClubName").Select(c => c.ColumnNumber).FirstOrDefault();
                    }

                    foreach (RangeRow row in workSheet.Rows)
                    {
                        if (row.Columns[playerNameCol].Value.ToString() == "PlayerName" || row.Columns[clubNameCol].Value.ToString() == "ClubName")
                        {
                            continue;
                        }
                        string playerName = row.Columns[playerNameCol].Value.ToString() ?? "";
                        string clubName = row.Columns[clubNameCol].Value.ToString() ?? "";
                        int clubId = -1;
                        int playerId = -1;
                        playerId = getPlayerId(connection, playerName);
                        clubId = getPlayerId(connection, clubName);
                        Console.WriteLine("PlayerName '{0}', Id:{1} in Club {1} Club Id:{3}", playerName, playerId, clubName, clubId);
                        insertClubPlayer(connection, playerName, clubName);
                    }
                }
            }
        }

        private static void insertClub(SQLiteConnection connection, string clubName)
        {
            using SQLiteCommand cmdInsertPlayers = connection.CreateCommand();
            cmdInsertPlayers.Parameters.AddWithValue("clubName", clubName);
            cmdInsertPlayers.CommandText = @"INSERT OR IGNORE INTO Clubs(ClubName)VALUES(@clubName);";
            cmdInsertPlayers.ExecuteNonQuery();
        }

        private static void insertPlayer(SQLiteConnection connection, string playerName, double points, int price)
        {
            using SQLiteCommand cmdInsertPlayers = connection.CreateCommand();
            cmdInsertPlayers.Parameters.AddWithValue("playerName", playerName);
            cmdInsertPlayers.Parameters.AddWithValue("points", points);
            cmdInsertPlayers.Parameters.AddWithValue("price", price);
            cmdInsertPlayers.CommandText = @"INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES(@playerName, @points , @price );";
            cmdInsertPlayers.ExecuteNonQuery();
        }

        private static void insertClubPlayer(SQLiteConnection connection, string clubName, string playerName)
        {
            int playerId = 0;
            int clubId = 0;

            using SQLiteCommand cmdViewPlayer = connection.CreateCommand();
            cmdViewPlayer.CommandText = @"SELECT p.PlayerId,p.PlayerName,p.Price,p.Points FROM Players p where p.PlayerName=" + playerName;
            using (SQLiteDataReader reader = cmdViewPlayer.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    playerId = reader.GetInt32(0);
                    int price = reader.GetInt32(2);
                    decimal points = reader.GetDecimal(3);
                    Console.WriteLine("");
                    Console.WriteLine("Found Player");
                    Console.WriteLine($"ID: {playerId}, Name:{playerName}, Price:{price}, Points:{points}");
                }
            }

            using SQLiteCommand cmdViewClub = connection.CreateCommand();
            cmdViewClub.CommandText = @"SELECT c.ClubId,c.ClubName FROM Clubs c where c.ClubName=" + clubName;
            using (SQLiteDataReader reader = cmdViewClub.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    clubId = reader.GetInt32(0);
                    Console.WriteLine("");
                    Console.WriteLine("Found Club");
                    Console.WriteLine($"ID: {clubId}, Name:{clubName}");
                }
            }

            if (playerId == 0 || clubId == 0)
            {
                Console.WriteLine("Player not found");
            }
            else if (clubId == 0)
            {
                Console.WriteLine("Club not found");
            }
            else
            {
                using SQLiteCommand cmdInsertClubPlayer = connection.CreateCommand();
                cmdInsertClubPlayer.Parameters.AddWithValue("playerId", playerId);
                cmdInsertClubPlayer.Parameters.AddWithValue("clubId", clubId);
                cmdInsertClubPlayer.CommandText = @"INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES(@playerId, @clubId);";
                cmdInsertClubPlayer.ExecuteNonQuery();
            }
        }

        private static void createTables(SQLiteConnection connection)
        {
            try
            {
                using SQLiteCommand cmdCreateClubs = connection.CreateCommand();
                cmdCreateClubs.CommandText = @" CREATE TABLE IF NOT EXISTS Clubs (
                                    ClubId INTEGER not null PRIMARY KEY AUTOINCREMENT,
                                    ClubName TEXT not null
            );";
                cmdCreateClubs.ExecuteNonQuery();

                using SQLiteCommand cmdCreatePlayers = connection.CreateCommand();
                cmdCreatePlayers.CommandText = @" CREATE TABLE IF NOT EXISTS Players (
                                    PlayerId INTEGER not null PRIMARY KEY AUTOINCREMENT,
                                    PlayerName TEXT not null,
                                    Price INTEGER not null,
                                    Points REAL not null
            );";
                cmdCreatePlayers.ExecuteNonQuery();

                using SQLiteCommand cmdCreatePlayerClubs = connection.CreateCommand();
                cmdCreatePlayerClubs.CommandText = @" CREATE TABLE IF NOT EXISTS PlayerClubs (
                                    PlayerClubId INTEGER not null PRIMARY KEY AUTOINCREMENT,
                                    PlayerId INTEGER not null,
                                    ClubId INTEGER not null,
                                    FOREIGN KEY(PlayerID) REFERENCES Players(PlayerId),
                                    FOREIGN KEY(ClubId) REFERENCES Clubs(ClubId)
            );";
                cmdCreatePlayerClubs.ExecuteNonQuery();

                using SQLiteCommand cmdCreateUserTeam = connection.CreateCommand();
                cmdCreateUserTeam.CommandText = @" CREATE TABLE IF NOT EXISTS UserTeam (
                                    PlayerId INTEGER not null,
                                    ClubId INTEGER not null,
                                    FOREIGN KEY(PlayerID) REFERENCES Players(PlayerId),
                                    FOREIGN KEY(ClubID) REFERENCES Clubs(ClubId)
            );";
                cmdCreateUserTeam.ExecuteNonQuery();

                Console.WriteLine("Created Tables");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static void insertDummyData(SQLiteConnection connection)
        {
            using SQLiteCommand cmdInsertClubs = connection.CreateCommand();
            cmdInsertClubs.CommandText = @" INSERT OR IGNORE INTO Clubs(ClubName)VALUES('Club 1'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Two'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Happy'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Somethings'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Football'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club BOB'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Furries'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Internet'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Coolers'); 
                                            INSERT OR IGNORE  INTO Clubs(ClubName)VALUES('Club Hot');";
            cmdInsertClubs.ExecuteNonQuery();

            using SQLiteCommand cmdInsertPlayers = connection.CreateCommand();
            cmdInsertPlayers.CommandText = @"INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Justyn Gianfranco', 45 , 10357289 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Sanjeev Aminu', 47 , 9883353 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Anani Hristo', 38 , 11177900 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Yngve Juraj', 78 , 9650196 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Anantha Tvrtko', 83 , 10831042 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dan Gonggong', 16 , 6863137 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Savitr Uberto', 47 , 14495594 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Artemio Roldán', 90 , 12362071 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kiran Callistus', 27 , 14028716 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Noé Luis', 56 , 11351558 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dash Máirtín', 40 , 7385036 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Samu Tharindu', 30 , 8623511 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Hyperion Þórarinn', 60 , 13162305 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ayotunde Marcus', 14 , 10528688 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Séamas Benjamín', 88 , 12867041 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Edison Ryousuke', 15 , 10401508 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Isokrates Sopheap', 98 , 13531707 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gaiseric Sven', 27 , 5499484 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Glebŭ Gian', 9 , 8912680 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Nicolae Onyekachi', 75 , 11455384 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Bohdan Tetsuya', 19 , 9329537 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Johnie Brutus', 12 , 10509326 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Okan Sheard', 30 , 7226006 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gopala Hilmar', 36 , 7672429 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Deepak Abhay', 97 , 7477707 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kieron Bojidar', 36 , 11787815 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Lucian Kleisthenes', 65 , 11359104 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Willoughby Pedro', 89 , 8044526 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Suleimen Nour', 20 , 9710653 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Marcas Amon', 73 , 13742105 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Yuusuf Sardar', 51 , 11297212 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Heidrich Vladislav', 67 , 8406434 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kaneonuskatew Farhan', 1 , 14481466 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Zdeněk Barend', 93 , 13160449 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Boipelo Olle', 93 , 5294268 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Rune Suibne', 33 , 7527107 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Erez Awotwi', 43 , 9690970 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Aindrea Menelaos', 22 , 6588635 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ramesha Janko', 76 , 7444275 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Vasant Bhaskar', 6 , 9178606 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Somchai Pradip', 81 , 5726154 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Merab Æthelric', 49 , 12106417 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dimas Victoriano', 15 , 11241484 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Fotis Ülo', 56 , 14402899 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ermis Burkhard', 87 , 5843147 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Matic Gotam', 81 , 14357997 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gebahard Ezechiel', 68 , 5212466 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Refilwe Zacharias', 63 , 5355005 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ljuban Grega', 74 , 7541317 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Rabindra Kerman', 45 , 10880106 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Justyn Gianfranco Jr', 70 , 6595717 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Sanjeev Aminu Jr', 71 , 5821697 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Anani Hristo Jr', 67 , 14248840 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Yngve Juraj Jr', 1 , 8523117 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Anantha Tvrtko Jr', 29 , 12999516 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dan Gonggong Jr', 93 , 13798104 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Savitr Uberto Jr', 14 , 11376446 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Artemio Roldán Jr', 87 , 11075137 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kiran Callistus Jr', 39 , 9986353 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Noé Luis Jr', 3 , 8350345 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dash Máirtín Jr', 37 , 14594892 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Samu Tharindu Jr', 45 , 13075958 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Hyperion Þórarinn Jr', 45 , 14612290 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ayotunde Marcus Jr', 9 , 6400460 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Séamas Benjamín Jr', 11 , 11052986 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Edison Ryousuke Jr', 30 , 10247177 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Isokrates Sopheap Jr', 67 , 11092233 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gaiseric Sven Jr', 32 , 5040641 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Glebŭ Gian Jr', 14 , 6176879 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Nicolae Onyekachi Jr', 94 , 13966799 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Bohdan Tetsuya Jr', 86 , 14967138 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Johnie Brutus Jr', 32 , 11516959 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Okan Sheard Jr', 47 , 9677620 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gopala Hilmar Jr', 2 , 5781026 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Deepak Abhay Jr', 30 , 6416771 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kieron Bojidar Jr', 21 , 6118485 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Lucian Kleisthenes Jr', 95 , 14442818 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Willoughby Pedro Jr', 18 , 8072949 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Suleimen Nour Jr', 38 , 11691730 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Marcas Amon Jr', 51 , 12865921 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Yuusuf Sardar Jr', 4 , 11099598 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Heidrich Vladislav Jr', 66 , 8697274 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Kaneonuskatew Farhan Jr', 79 , 14030968 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Zdeněk Barend Jr', 6 , 9114530 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Boipelo Olle Jr', 92 , 9209829 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Rune Suibne Jr', 42 , 10642178 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Erez Awotwi Jr', 21 , 11886205 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Aindrea Menelaos Jr', 56 , 7969394 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ramesha Janko Jr', 2 , 13294959 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Vasant Bhaskar Jr', 49 , 12593848 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Somchai Pradip Jr', 51 , 12792130 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Merab Æthelric Jr', 64 , 6756695 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Dimas Victoriano Jr', 58 , 8186612 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Fotis Ülo Jr', 83 , 12656202 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ermis Burkhard Jr', 23 , 5801415 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Matic Gotam Jr', 41 , 10702951 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Gebahard Ezechiel Jr', 39 , 13560438 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Refilwe Zacharias Jr', 94 , 5417753 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Ljuban Grega Jr', 100 , 14046116 );
INSERT OR IGNORE INTO Players(PlayerName,Points,Price)VALUES('Rabindra Kerman Jr', 86 , 13444818 );";
            cmdInsertPlayers.ExecuteNonQuery();

            using SQLiteCommand cmdInsertPlayerClubs = connection.CreateCommand();
            cmdInsertPlayerClubs.CommandText = @"INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Justyn Gianfranco'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Sanjeev Aminu'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Anani Hristo'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Yngve Juraj'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Anantha Tvrtko'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dan Gonggong'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Savitr Uberto'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Artemio Roldán'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kiran Callistus'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Noé Luis'limit 1),(SELECT ClubId from Clubs where ClubName='Club 1' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dash Máirtín'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Samu Tharindu'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Hyperion Þórarinn'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ayotunde Marcus'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Séamas Benjamín'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Edison Ryousuke'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Isokrates Sopheap'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gaiseric Sven'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Glebŭ Gian'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Nicolae Onyekachi'limit 1),(SELECT ClubId from Clubs where ClubName='Club Two' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Bohdan Tetsuya'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Johnie Brutus'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Okan Sheard'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gopala Hilmar'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Deepak Abhay'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kieron Bojidar'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Lucian Kleisthenes'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Willoughby Pedro'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Suleimen Nour'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Marcas Amon'limit 1),(SELECT ClubId from Clubs where ClubName='Club Happy' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Yuusuf Sardar'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Heidrich Vladislav'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kaneonuskatew Farhan'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Zdeněk Barend'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Boipelo Olle'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Rune Suibne'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Erez Awotwi'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Aindrea Menelaos'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ramesha Janko'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Vasant Bhaskar'limit 1),(SELECT ClubId from Clubs where ClubName='Club Somethings' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Somchai Pradip'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Merab Æthelric'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dimas Victoriano'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Fotis Ülo'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ermis Burkhard'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Matic Gotam'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gebahard Ezechiel'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Refilwe Zacharias'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ljuban Grega'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Rabindra Kerman'limit 1),(SELECT ClubId from Clubs where ClubName='Club Football' limit 1));    
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Justyn Gianfranco Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Sanjeev Aminu Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Anani Hristo Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Yngve Juraj Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Anantha Tvrtko Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dan Gonggong Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Savitr Uberto Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Artemio Roldán Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kiran Callistus Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club BOB' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Noé Luis Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dash Máirtín Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Samu Tharindu Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Hyperion Þórarinn Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ayotunde Marcus Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Séamas Benjamín Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Edison Ryousuke Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Isokrates Sopheap Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gaiseric Sven Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Glebŭ Gian Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Furries' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Nicolae Onyekachi Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Bohdan Tetsuya Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Johnie Brutus Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Okan Sheard Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gopala Hilmar Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Deepak Abhay Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kieron Bojidar Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Lucian Kleisthenes Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Willoughby Pedro Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Suleimen Nour Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Internet' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Marcas Amon Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Yuusuf Sardar Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Heidrich Vladislav Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Kaneonuskatew Farhan Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Zdeněk Barend Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Boipelo Olle Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Rune Suibne Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Erez Awotwi Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Aindrea Menelaos Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ramesha Janko Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Coolers' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Vasant Bhaskar Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Somchai Pradip Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Merab Æthelric Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Dimas Victoriano Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Fotis Ülo Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ermis Burkhard Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Matic Gotam Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Gebahard Ezechiel Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Refilwe Zacharias Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Ljuban Grega Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1));  
INSERT OR IGNORE INTO PlayerClubs(PlayerId,ClubId)VALUES((SELECT PlayerId from Players where PlayerName='Rabindra Kerman Jr'limit 1),(SELECT ClubId from Clubs where ClubName='Club Hot' limit 1))";
            cmdInsertPlayerClubs.ExecuteNonQuery();

            Console.WriteLine("Created Dummy Data");
        }
        private static void viewTeam(SQLiteConnection connection)
        {
            using SQLiteCommand cmdViewTeam = connection.CreateCommand();
            cmdViewTeam.CommandText = @"SELECT ut.PlayerId,p.PlayerName,p.Price,p.Points,c.ClubName FROM UserTeam ut 
                                        INNER JOIN Players p on ut.PlayerId = p.PlayerId
                                        INNER JOIN PlayerClubs pc on p.PlayerId = pc.PlayerId
                                        INNER JOIN Clubs c on pc.ClubId = c.ClubId";
            using (SQLiteDataReader reader = cmdViewTeam.ExecuteReader())
            {
                Console.WriteLine("");
                Console.WriteLine("Current Team");
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string playerName = reader.GetString(1);
                    int price = reader.GetInt32(2);
                    decimal points = reader.GetDecimal(3);
                    string clubName = reader.GetString(4);
                    Console.WriteLine($"ID: {id}, Name:{playerName}, Price:{price}, Points:{points}, Club:{clubName}");
                }
            }
        }
        private static void viewPlayers(SQLiteConnection connection)
        {
            using SQLiteCommand cmdViewTeam = connection.CreateCommand();
            cmdViewTeam.CommandText = @"SELECT p.PlayerId,p.PlayerName,p.Price,p.Points FROM Players p ORDER By p.PlayerName";
            using (SQLiteDataReader reader = cmdViewTeam.ExecuteReader())
            {
                Console.WriteLine("");
                Console.WriteLine("Current Players");
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string playerName = reader.GetString(1);
                    int price = reader.GetInt32(2);
                    decimal points = reader.GetDecimal(3);
                    Console.WriteLine($"ID: {id}, Name:{playerName}, Price:{price}, Points:{points}");
                }
            }
        }
        private static void viewClubs(SQLiteConnection connection)
        {
            using SQLiteCommand cmdViewTeam = connection.CreateCommand();
            cmdViewTeam.CommandText = @"SELECT c.ClubId,c.ClubName FROM Clubs c ORDER By c.ClubName";
            using (SQLiteDataReader reader = cmdViewTeam.ExecuteReader())
            {
                Console.WriteLine("");
                Console.WriteLine("Current Clubs");
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string clubName = reader.GetString(1);
                    Console.WriteLine($"ID: {id}, Name:{clubName}");
                }
            }
        }
        private static void addPlayerFromPlayersList(SQLiteConnection connection)
        {
            viewPlayers(connection);
            Console.WriteLine("");
            Console.WriteLine("Please select player to add to your current team from list above, you can add them from ID or by Name.");

            while (true)
            {
                string input = Console.ReadLine() ?? "";

                using SQLiteCommand cmdGetPlayer = connection.CreateCommand();
                cmdGetPlayer.Parameters.AddWithValue("input", input);
                if (int.TryParse(input, out _))
                {
                    cmdGetPlayer.CommandText = @"SELECT p.PlayerId,p.PlayerName FROM Players p where PlayerId = @input";
                }
                else
                {
                    cmdGetPlayer.CommandText = @"SELECT p.PlayerId,p.PlayerName FROM Players p where PlayerName like @input ";
                }
                using (SQLiteDataReader reader = cmdGetPlayer.ExecuteReader())
                {
                    Console.WriteLine("");
                    Console.WriteLine("Found player");
                    while (reader.HasRows && reader.Read())
                    {
                        int playerId = reader.GetInt32(0);
                        string playerName = reader.GetString(1);
                        //Console.WriteLine($"ID: {id}, Name:{playerName}");

                        int clubId = getPlayerClubId(connection, playerId);
                        if (clubId == 0)
                        {
                            Console.WriteLine("Player does not belong to a club.");
                        }

                        if (validateTeamSelect(connection, playerId, clubId))
                        {
                            using SQLiteCommand cmdAddPlayerToTeam = connection.CreateCommand();
                            cmdAddPlayerToTeam.CommandText = @"INSERT OR IGNORE INTO UserTeam(PlayerId,ClubId)VALUES(" + playerId.ToString() + "," + clubId.ToString() + ")";
                            cmdAddPlayerToTeam.ExecuteNonQuery();
                        }
                    }
                }
                break;
            }
        }
        private static int getPlayerClubId(SQLiteConnection connection, int playerId)
        {
            using SQLiteCommand cmdGetPlayerClubId = connection.CreateCommand();
            cmdGetPlayerClubId.CommandText = @"SELECT pc.ClubId FROM PlayerClubs pc WHERE pc.PlayerId = " + playerId;
            using (SQLiteDataReader reader = cmdGetPlayerClubId.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);

                    return id;
                }
            }
            return 0;
        }
        private static int countCurrentTeamPlayersPerClub(SQLiteConnection connection, int clubId)
        {
            using SQLiteCommand cmdGetPlayerCount = connection.CreateCommand();
            cmdGetPlayerCount.CommandText = @"SELECT COUNT(ut.PlayerId) FROM UserTeam ut INNER JOIN PlayerClubs pc on ut.PlayerId = pc.PlayerId WHERE pc.ClubId = " + clubId;
            using (SQLiteDataReader reader = cmdGetPlayerCount.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int count = reader.GetInt32(0);

                    return count;
                }
            }
            return 0;
        }
        private static int sumTeamCost(SQLiteConnection connection)
        {
            using SQLiteCommand cmdGetTeamCost = connection.CreateCommand();
            cmdGetTeamCost.CommandText = @"SELECT IFNULL(Sum(p.Price),0) FROM UserTeam ut INNER JOIN Players p on ut.PlayerId = p.PlayerId";
            using (SQLiteDataReader reader = cmdGetTeamCost.ExecuteReader())
            {
                try
                {
                    while (reader.HasRows && reader.Read())
                    {
                        int cost = reader.GetInt32(0);

                        return cost;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 0;
                }
            }
            return 0;
        }
        private static int countUserPlayers(SQLiteConnection connection)
        {
            using SQLiteCommand cmdGetTeamCost = connection.CreateCommand();
            cmdGetTeamCost.CommandText = @"SELECT Count(ut.PlayerId) FROM UserTeam ut";
            using (SQLiteDataReader reader = cmdGetTeamCost.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int playerCount = reader.GetInt32(0);

                    return playerCount;
                }
            }
            return 0;
        }
        private static int getPlayerPrice(SQLiteConnection connection, int playerId)
        {
            using SQLiteCommand cmdGetPlayerPrice = connection.CreateCommand();
            cmdGetPlayerPrice.CommandText = @"SELECT p.Price FROM Players p WHERE p.PlayerId = " + playerId;
            using (SQLiteDataReader reader = cmdGetPlayerPrice.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int price = reader.GetInt32(0);

                    return price;
                }
            }
            return 0;
        }
        private static bool validateTeamSelect(SQLiteConnection connection, int playerId, int clubId)
        {
            bool result = false;
            int currentCount = countCurrentTeamPlayersPerClub(connection, clubId);
            if (currentCount + 1 > selectionLimit)
            {
                Console.WriteLine("Max:" + selectionLimit.ToString() + " players from the same club");
                result = false;
                return result;
            }
            else
            {
                result = true;
            }

            int projectedCost = getPlayerPrice(connection, playerId) + sumTeamCost(connection);
            if (projectedCost > maxTeamCost)
            {
                Console.WriteLine("Max cost of team can not exceed:" + maxTeamCost.ToString());
                result = false;
                return result;
            }
            else
            {
                result = true;
            }

            int userTeamPlayerCount = countUserPlayers(connection);
            if (userTeamPlayerCount >= maxTeamMembers)
            {
                Console.WriteLine("Max number of players in a team:" + maxTeamMembers.ToString());
                result = false;
                return result;
            }
            else
            {
                result = true;
            }
            return result;
        }
        private static void deletePlayerFromTeamList(SQLiteConnection connection)
        {
            viewTeam(connection);
            Console.WriteLine("");
            Console.WriteLine("Please select player to delete from your current team from list above, you can remove them from ID.");

            while (true)
            {
                string input = Console.ReadLine() ?? "";
                using SQLiteCommand cmdGetPlayer = connection.CreateCommand();
                cmdGetPlayer.Parameters.AddWithValue("input", input);
                if (int.TryParse(input, out _))
                {
                    deletePlayerFromTeam(connection, Convert.ToInt32(input));
                }
                else
                {
                    Console.WriteLine("Input needs to be an ID");
                }
                break;
            }
        }
        private static void deletePlayerFromTeam(SQLiteConnection connection, int playerId)
        {
            using SQLiteCommand cmdDelPlayerFromTeam = connection.CreateCommand();
            cmdDelPlayerFromTeam.CommandText = @"DELETE FROM UserTeam WHERE PlayerId = " + playerId;
            cmdDelPlayerFromTeam.ExecuteNonQuery();
        }

        private static int getPlayerId(SQLiteConnection connection, string playerName)
        {
            using SQLiteCommand cmdGetClubId = connection.CreateCommand();
            cmdGetClubId.Parameters.AddWithValue("playerName", playerName);
            cmdGetClubId.CommandText = @"SELECT p.PlayerId FROM Players p WHERE p.PlayerName = @playerName";
            using (SQLiteDataReader reader = cmdGetClubId.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);

                    return id;
                }
            }
            return -1;
        }

        private static int getClubId(SQLiteConnection connection, string clubName)
        {
            using SQLiteCommand cmdGetClubId = connection.CreateCommand();
            cmdGetClubId.Parameters.AddWithValue("clubName", clubName);
            cmdGetClubId.CommandText = @"SELECT c.ClubId FROM Clubs c WHERE c.ClubName = @clubName";
            using (SQLiteDataReader reader = cmdGetClubId.ExecuteReader())
            {
                while (reader.HasRows && reader.Read())
                {
                    int id = reader.GetInt32(0);

                    return id;
                }
            }
            return -1;
        }
    }
}