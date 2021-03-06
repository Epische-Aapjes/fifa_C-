﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;

namespace ProjectFifaV2
{
    public partial class frmAdmin : Form
    {
        private DatabaseHandler dbh;
        private OpenFileDialog opfd;

        DataTable table;
        public frmAdmin()
        {
            dbh = new DatabaseHandler();
            table = new DataTable();
            this.ControlBox = false;
            InitializeComponent();
        }

        private void btnAdminLogOut_Click(object sender, EventArgs e)
        {
            txtQuery.Text = null;
            txtPath = null;
            dgvAdminData.DataSource = null;
            Hide();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            if (txtQuery.TextLength > 0)
            {
                ExecuteSQL(txtQuery.Text);
            }
        }

        private void ExecuteSQL(string selectCommandText)
        {
            dbh.TestConnection();
            SqlDataAdapter dataAdapter = new SqlDataAdapter(selectCommandText, dbh.GetCon());
            dataAdapter.Fill(table);
            dgvAdminData.DataSource = table;
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            txtPath.Text = null;

            string path = GetFilePath();

            if (CheckExtension(path, "csv"))
            {
                txtPath.Text = path;
            }
            else
            {
                MessageHandler.ShowMessage("The wrong filetype is selected.");
            }
        }

        private void btnLoadData_Click(object sender, EventArgs e)
        {
            if (txtPath.Text != "")
            {
                string[] pathSplit = txtPath.Text.Split('\\');
                int latestIndex = pathSplit.Length - 1;
                StreamReader sr;
                bool success = true;

                string fileName = pathSplit[latestIndex];

                dbh.OpenConnectionToDB();

                try
                {
                    sr = new StreamReader(txtPath.Text);
                }
                catch (System.IO.DirectoryNotFoundException exep)
                {
                    MessageHandler.ShowMessage(string.Format("Couldn't find the directory..", exep));
                    success = false;
                }
                catch (System.IO.FileNotFoundException exep)
                {
                    MessageHandler.ShowMessage(string.Format("Couldn't find the file..", exep));
                    success = false;
                }

                if (success)
                {
                    sr = new StreamReader(txtPath.Text);

                    string line = sr.ReadLine();
                    string[] value = line.Split(',');

                    DataTable dt = new DataTable();

                    foreach (string dc in value)
                    {
                        dt.Columns.Add(new DataColumn(dc));
                    }

                    while (!sr.EndOfStream)
                    {
                        value = sr.ReadLine().Split(',');
                        if (value.Length == dt.Columns.Count)
                        {
                            DataRow row = dt.NewRow();
                            row.ItemArray = value;
                            dt.Rows.Add(row);
                        }
                        else
                        {
                            MessageHandler.ShowMessage("Amount of columns not consistent");
                            return;
                        }
                    }

                    SqlBulkCopy bc = new SqlBulkCopy(dbh.GetCon().ConnectionString, SqlBulkCopyOptions.TableLock);

                    if (fileName.Contains("teams"))
                    {
                        bc.DestinationTableName = "TblTeams";

                        MessageHandler.ShowMessage("Teams toegevoegd");
                    }
                    else if (fileName.Contains("matches"))
                    {
                        bc.DestinationTableName = "TblGames";

                        MessageHandler.ShowMessage("Matches toegevoegd");
                    }
                    else if (!fileName.Contains("matches"))
                    {
                        MessageHandler.ShowMessage("Verkeerd bestand!");
                    }
                    else if (!fileName.Contains("teams"))
                    {
                        MessageHandler.ShowMessage("Verkeerd bestand!");
                    }

                    bc.BatchSize = dt.Rows.Count;
                    bc.WriteToServer(dt);
                    bc.Close();
                }
            }
            dbh.CloseConnectionToDB();
        }

        private string GetFilePath()
        {
            string filePath = "";
            opfd = new OpenFileDialog();

            opfd.Multiselect = false;

            if (opfd.ShowDialog() == DialogResult.OK)
            {
                filePath = opfd.FileName;
            }

            return filePath;
        }

        private bool CheckExtension(string fileString, string extension)
        {
            int extensionLength = extension.Length;
            int strLength = fileString.Length;

            string ext = fileString.Substring(strLength - extensionLength, extensionLength);

            if (ext == extension)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
