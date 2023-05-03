using MySqlConnector;
using System;
using System.Data;
using System.Data.Odbc;
using System.Threading.Tasks;
using System.Windows;
using Z.BulkOperations;
using MySqlDataAdapter = MySqlConnector.MySqlDataAdapter;
using System.IO;
using System.Windows.Media;
using System.Linq;
using System.ComponentModel;
using System.Windows.Threading;

namespace SyncDB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string strTitle = "Sync";
        private const string tableName = ""; //define table

        private string strDatabase = "";
        private string strArgs = "";
        private bool bolExe = true;

        public MainWindow(string _strArgs)
        {
            InitializeComponent();
            strArgs = _strArgs;
            Activated += OnLoaded;

            getToko();
            getStatus();
        }

        private async void OnLoaded(object sender, EventArgs e)
        {
            if (strArgs == "silent")
            {
                if (bolExe)
                {
                    cmbToko.IsEnabled = false;
                    btnProcess.IsEnabled = false;
                    await this.Dispatcher.Invoke(async () =>
                    {
                        await Task.Run((sync));
                    });
                }
            }
        }

        private async void sync()
        {
            bolExe = false;

            await this.Dispatcher.Invoke(async () =>
            {
                if (strDatabase != "")
                {
                    string dbPath = Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).ToString()).ToString() + "\\Database.accde";
                    if (File.Exists(dbPath))
                    {
                        //start
                        await Task.Run(() => updateStatus("Checking"));

                        try
                        {
                            //get mysql connection string
                            MySqlConnectionStringBuilder conString = getConString();

                            //open connection to accde
                            OdbcConnection con = new OdbcConnection("Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=" + dbPath + ";Uid=Admin;Pwd=;");

                            //split table name
                            string[] arrTable = tableName.Split(new char[] { ';' }, StringSplitOptions.None);

                            var progress = new Progress<int>(value => pgProcess.Value = value);
                            int i = 1;
                            pgProcess.Maximum = arrTable.Count();

                            await Task.Run(() =>
                            {
                                foreach (string table in arrTable)
                                {
                                    //create data table
                                    DataTable dt = new DataTable(table);

                                    //fill data table
                                    using (OdbcDataAdapter oda = new OdbcDataAdapter("SELECT * FROM " + table, con))
                                    {
                                        oda.Fill(dt);
                                    }

                                    // open a connection asynchronously
                                    var connection = new MySqlConnection(conString.ConnectionString);
                                    connection.Open();

                                    //delete table
                                    var command = connection.CreateCommand();
                                    command.CommandText = "DELETE FROM " + table;
                                    command.ExecuteScalar();

                                    //open mysql table
                                    MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM " + table, connection);
                                    MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

                                    //bulk insert to mysql table
                                    var bulk = new BulkOperation(connection);
                                    bulk.BulkInsert(dt);

                                    connection.Close();

                                    ((IProgress<int>)progress).Report(i);
                                    i++;
                                }
                            });

                            //update status
                            await Task.Run(() => updateStatus("Success"));

                            //labelTable.Text = "";
                            MessageBox.Show("Sync success!", strTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            //update status
                            await Task.Run(() => updateStatus("Failed"));

                            MessageBox.Show("Sync failed: " + ex.Message, strTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        getStatus();
                    }
                    else
                    {
                        MessageBox.Show("Database not found!", strTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Database server not found!", strTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (strArgs == "silent") mainMenu.Close();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(async () =>
            {
                await Task.Run((sync));
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            saveSetting();

            base.OnClosing(e);
        }

        //server and database
        private MySqlConnectionStringBuilder getConString()
        {
            MySqlConnectionStringBuilder conString = new MySqlConnectionStringBuilder();

            conString.Server = "";
            conString.UserID = "";
            conString.Password = "";
            conString.Database = "" + strDatabase;
            conString.Port = 3306;
            conString.CharacterSet = "utf8mb4";
            conString.ConnectionTimeout = 60;
            conString.SslMode = MySqlSslMode.None;

            return conString;
        }

        void saveSetting()
        {
            Properties.Settings.Default.strToko = cmbToko.Text;
            Properties.Settings.Default.Save();
        }

        private void getToko()
        {
            cmbToko.Items.Clear();
            cmbToko.Items.Add("Toko");
            cmbToko.SelectedItem = Properties.Settings.Default.strToko;  
        }

        private void getStatus()
        {
            this.Dispatcher.Invoke(() =>
            {
                //get mysql connection string
                MySqlConnectionStringBuilder conString = getConString();

                lblStatus.Content = "";

                try
                {
                    string status = "";

                    // open a connection asynchronously
                    var connection = new MySqlConnection(conString.ConnectionString);
                    connection.Open();

                    //get status
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT status FROM tbl_status";
                    status = command.ExecuteScalar().ToString();

                    if (status == "Success") //sucess
                    {
                        lblStatus.Foreground = Brushes.Green;
                    }
                    else //failed
                    {
                        lblStatus.Foreground = Brushes.Red;
                    }

                    //get date
                    command.CommandText = "SELECT dtUpdate FROM tbl_status";
                    status = status + ": " + command.ExecuteScalar().ToString();

                    connection.Close();

                    lblStatus.Content = status;
                }
                catch (Exception ex)
                {
                    lblStatus.Content = "Error!";
                    lblStatus.Foreground = Brushes.Red;
                }
            });
        }

        private void updateStatus(string strStatus)
        {
            this.Dispatcher.Invoke(() =>
            {
                //get mysql connection string
                MySqlConnectionStringBuilder conString = getConString();

                try
                {
                    // open a connection asynchronously
                    var connection = new MySqlConnection(conString.ConnectionString);
                    connection.Open();

                    //update status
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE tbl_status SET status = '" + strStatus + "'";
                    command.ExecuteScalarAsync();

                    connection.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Update failed: " + ex.Message, strTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void cmbToko_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            strDatabase = "";
            getStatus();
        }
    }
 }
