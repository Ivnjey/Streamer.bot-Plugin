using StreamerbotPlugin.Models;
using Newtonsoft.Json;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Variables;

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;


namespace StreamerbotPlugin.GUI
{
    public partial class PluginConfig : DialogForm
    {
        private List<Tuple<string, string>> selectedVariables = new List<Tuple<string, string>>();
        private List<CheckboxState> checkboxStates = new List<CheckboxState>();
        WebSocketClient webSocketClient = WebSocketClient.Instance;
        Configuration config = Configuration.Instance;

        public PluginConfig()
        {

            InitializeComponent();
            checkboxColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            textBox_Address.Text = config.Address;
            textBox_Port.Text = config.Port.ToString();
            textBox_Endpoint.Text = config.Endpoint;

            // Set up tooltip for PictureBox
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(btn_Connect, "Connect to Streamer.bot's Websocked Server.");
            toolTip.SetToolTip(buttonPrimary1, "Copy Streamer.bot action import code");
            IsConnected(this, EventArgs.Empty);
            WebSocketClient.WebSocketConnected += IsConnected;
            WebSocketClient.WebSocketDisconnected += IsConnected;


            // Subscribe to the UpdateVariableList event
            Main.UpdateVariableList += HandleUpdateVariableList;

            PopulateDataGridView();
            LoadCheckboxState();
        }

        // static string GetFileContent(string url)
        // {
        //     using (var client = new HttpClient())
        //     {
        //         HttpResponseMessage response = client.GetAsync(url).Result; // Using .Result to synchronously wait for the response
        //         response.EnsureSuccessStatusCode();

        //         string fileContent = response.Content.ReadAsStringAsync().Result; // Using .Result to synchronously wait for the content

        //         return fileContent;
        //     }
        // }

        private void HandleUpdateVariableList(object sender, EventArgs e)
        {
            PopulateDataGridView();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            SaveData();
            Close();
        }
        private async void btn_Connect_Click(object sender, EventArgs e)
        {
            SaveData();
            await (WebSocketClient.IsConnected ? webSocketClient.CloseAsync(true) : webSocketClient.ConnectAsync());
        }

        private void SaveData()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBox_Port.Text))
                {
                    new ErrorMessage("please enter port.").ShowDialog();
                    return;
                }
                else if (string.IsNullOrWhiteSpace(textBox_Address.Text))
                {
                    new ErrorMessage("please enter address.").ShowDialog();
                    return;
                }
                config.Port = int.Parse(textBox_Port.Text);
                if (config.Port <= 0)
                {
                    new ErrorMessage("Incorrect value for port.").ShowDialog();
                    config.Port = 8080;
                    return;
                }
                config.Address = textBox_Address.Text;
                config.Endpoint = textBox_Endpoint.Text;
                config.uri = null;
                MacroDeckLogger.Info(PluginInstance.Main, $"Currently Address: {config.Address}, Endpoint: {config.Endpoint}, Port: {config.Port}...");

            }
            catch (Exception ex)
            {

                new ErrorMessage($"Invalid {ex.Message}.").ShowDialog();

            }
        }
        private void IsConnected(object sender, EventArgs e)
        {
            btn_Connect.Text = WebSocketClient.IsConnected ? "Disconnect" : "Connect";
            Invalidate();
            Update();
        }



        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["checkBoxColumn"].Index && e.RowIndex >= 0)
            {
                DataGridViewCheckBoxCell checkBoxCell = dataGridView1.Rows[e.RowIndex].Cells["checkBoxColumn"] as DataGridViewCheckBoxCell;

                // Get the state before the click
                bool prevState = Convert.ToBoolean(checkBoxCell.Value);

                // Toggle the checkbox state
                checkBoxCell.Value = !prevState;

                // Get the current state after the click
                bool currentState = Convert.ToBoolean(checkBoxCell.Value);

                // Get the variable name and value
                string variableName = dataGridView1.Rows[e.RowIndex].Cells["VariableName"].Value.ToString();
                string variableValue = dataGridView1.Rows[e.RowIndex].Cells["VariableValue"].Value.ToString();

                if (currentState)
                {
                    // Checkbox is checked
                    selectedVariables.Add(new Tuple<string, string>(variableName, variableValue));
                    VariableType type = VariableTypeHelper.GetVariableType(variableValue);

                    VariableManager.SetValue(variableName, variableValue, type, PluginInstance.Main, ["Gobal Variables"]);
                }
                else
                {
                    // Checkbox is unchecked
                    selectedVariables.RemoveAll(v => v.Item1 == variableName && v.Item2 == variableValue);
                    VariableManager.DeleteVariable(variableName.ToLower());
                }

                // Save checkbox state
                SaveCheckboxState();
            }
        }


        private void SaveCheckboxState()
        {
            // If checkboxStates is null, initialize it
            if (checkboxStates == null)
            {
                checkboxStates = new List<CheckboxState>();
            }

            // Clear existing checkbox states
            checkboxStates.Clear();

            // Add current checkbox states
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                bool isChecked = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                string variableName = row.Cells["VariableName"].Value.ToString();
                string variableValue = row.Cells["VariableValue"].Value.ToString();

                checkboxStates.Add(new CheckboxState
                {
                    IsChecked = isChecked,
                    VariableName = variableName,
                    VariableValue = variableValue
                });
            }

            // Serialize and save checkbox states
            string json = JsonConvert.SerializeObject(checkboxStates, Formatting.Indented);
            PluginConfiguration.SetValue(PluginInstance.Main, "CheckboxState", json);
        }

        private void LoadCheckboxState()
        {
            string json = PluginConfiguration.GetValue(PluginInstance.Main, "CheckboxState");

            if (!string.IsNullOrEmpty(json))
            {
                checkboxStates = JsonConvert.DeserializeObject<List<CheckboxState>>(json);

                if (checkboxStates.Count > 0)
                {
                    // Set checkbox states based on loaded data
                    foreach (var state in checkboxStates)
                    {
                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (row.Cells["VariableName"].Value.ToString() == state.VariableName)
                            {
                                row.Cells["checkBoxColumn"].Value = state.IsChecked;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void PopulateDataGridView()
        {
            string globalVariables = PluginConfiguration.GetValue(PluginInstance.Main, "sb_globals");

            if (!string.IsNullOrEmpty(globalVariables))
            {
                // If global variables exist, deserialize them from JSON
                var existingGlobals = JsonConvert.DeserializeObject<Dictionary<string, string>>(globalVariables);

                if (existingGlobals != null)
                {
                    // Clear existing rows in the DataGridView
                    dataGridView1.Rows.Clear();

                    foreach (var kvp in existingGlobals)
                    {
                        // Add a new row to the DataGridView
                        dataGridView1.Rows.Add();

                        // Set the key and value in the appropriate columns
                        int rowIndex = dataGridView1.Rows.Count - 1; // Index of the newly added row
                        var formattedKey = kvp.Key;

                        // Set the key in the second column (index 1) and value in the third column (index 2)
                        dataGridView1.Rows[rowIndex].Cells[1].Value = formattedKey.ToLower();
                        dataGridView1.Rows[rowIndex].Cells[2].Value = kvp.Value.ToLower();
                    }
                    dataGridView1.Invalidate();
                    dataGridView1.Update();
                    LoadCheckboxState();
                }
            }
        }

        private void buttonPrimary1_Click(object sender, EventArgs e)
        {
            // Copy text from the textbox to the clipboard
            //Clipboard.SetText(roundedTextBox3.Text);
            buttonPrimary1.Text = "Copied";

            // Start a timer to change the button text back to "Copy" after a delay
            Timer timer = new Timer();
            timer.Interval = 3000; // 3000 milliseconds = 3 seconds
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            Timer timer = (Timer)sender;
            timer.Stop();

            // Change the button text back to "Copy"
            buttonPrimary1.Text = "Copy Code";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open the link in the default web browser
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "https://github.com/Ivnjey/Streamer.bot-Plugin");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error opening the link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
