﻿using StreamerbotPlugin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using System;

namespace StreamerbotPlugin.GUI
{
    public partial class StreamerBotActionConfigurator : ActionConfigControl
    {
        // Add a variable for the instance of your action to get access to the Configuration etc.
        private PluginAction _macroDeckAction;
        private WebSocketClient webSocketClient = WebSocketClient.Instance;

        public StreamerBotActionConfigurator(PluginAction macroDeckAction, ActionConfigurator actionConfigurator)
        {
            this._macroDeckAction = macroDeckAction;
            InitializeComponent();
            WebSocketClient.WebSocketOnMessageRecieved_actions += WebSocketClient_WebSocketOnMessageRecieved;
            //GetActionList(); нахер
        }

        private void WebSocketClient_WebSocketOnMessageRecieved(object sender, string message)
        {
            // Assuming e.Data is a JSON string representing your API response
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(message);
            MacroDeckLogger.Info(PluginInstance.Main, apiResponse.ToString());
            if (apiResponse != null && apiResponse.actions != null)
            {
                UpdateFormAcionList(message);
                return;
            }
        }

        public async void GetActionList()
        {
            // Generate a new Guid
            // Format the JSON string with the generated Guid
            string jsonRequest = @"
            {
                ""request"": ""GetActions"",
                ""id"": """ + Guid.NewGuid().ToString() + @"""
            }";
            
            await webSocketClient.SendMessageAsync(jsonRequest);
        }

        private void UpdateFormAcionList(string message)
        {
            //configuration["actionArgument"]
            comboBox_ActionList.Items.Clear();

            // Assuming e.Data is a JSON string representing your API response
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(message);
            MacroDeckLogger.Info(PluginInstance.Main, $"Current Response: {message}");

            if (apiResponse != null && apiResponse.actions != null)
            {
                foreach (var action in apiResponse.actions)
                {
                    // Process each action
                    // MacroDeckLogger.Error(PluginInstance.Main, $"Action Name: {action.name}, Group: {action.group}, Enabled: {action.enabled}, Subaction Count: {action.subaction_count}");
                    var item = new Models.Action(action.id, action.name, action.group, action.enabled, action.subaction_count);
                    comboBox_ActionList.Items.Add(item);
                }

                if (_macroDeckAction.Configuration != null)
                {
                    // Assuming _macroDeckAction.Configuration is a valid JSON string
                    JObject configurationObject = JObject.Parse(_macroDeckAction.Configuration);

                    // Further processing with configurationObject...
                    var selected = configurationObject["actionName"]?.ToString();
                    textBox_Arguments.Text = configurationObject["actionArgument"]?.ToString();

                    label_actionId.Text = configurationObject["actionId"]?.ToString();
                    label_actionName.Text = configurationObject["actionName"]?.ToString();
                    label_actionGroup.Text = configurationObject["actionGroup"]?.ToString();
                    label_actionEnabled.Text = configurationObject["actionEnabled"]?.ToString();
                    label_subactionCount.Text = configurationObject["actionSubactionCount"]?.ToString();

                    if (selected == string.Empty)
                    {
                        // Handle the case where selected is null or empty
                        if (comboBox_ActionList.Items.Count > 0)
                        {
                            comboBox_ActionList.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        comboBox_ActionList.Text = selected;
                    }
                }
                else
                {
                    // Handle the case where _macroDeckAction.Configuration is null
                    // For example, you could provide default values or log a message.
                    MacroDeckLogger.Info(PluginInstance.Main, "_macroDeckAction.Configuration is null.");
                }
            }
        }

        public override bool OnActionSave()
        {
            if (comboBox_ActionList.SelectedItem == null)
            {
                return false; // Return false if no action is selected
            }

            try
            {
                string summary = "";
                // Retrieve the selected Action object
                Models.Action selectedAction = (Models.Action)comboBox_ActionList.SelectedItem;

                JObject configuration = new JObject();
                configuration["actionId"] = selectedAction.id; // Save the ID of the selected action
                configuration["actionName"] = selectedAction.name; // Save the name of the selected action
                configuration["actionArgument"] = textBox_Arguments.Text;
                configuration["actionGroup"] = selectedAction.group;
                configuration["actionEnabled"] = selectedAction.enabled;
                configuration["actionSubactionCount"] = selectedAction.subaction_count;

                if (configuration["actionArgument"].ToString() == string.Empty)
                {
                    summary = $"Name - '{configuration["actionName"]}'";
                }
                else
                {
                    summary = $"Name - '{configuration["actionName"]}', Value - '{configuration["actionArgument"]}'";
                }

                this._macroDeckAction.ConfigurationSummary = summary; // Set a summary of the configuration that gets displayed in the ButtonConfigurator item
                this._macroDeckAction.Configuration = configuration.ToString();
            }
            catch { }
            return true; // Return true if the action was configured successfully; This closes the ActionConfigurator
        }

        private void btn_Refresh_Click(object sender, EventArgs e)
        {
            try
            {
                MacroDeckLogger.Trace(PluginInstance.Main, "TryButtonRefresh");
                GetActionList();
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Trace(PluginInstance.Main, "Failed to refresh actions: " + ex.Message);
            }
        }

        private void SaveDate()
        {
            Models.Action selectedAction = (Models.Action)comboBox_ActionList.SelectedItem;

            JObject configuration = new JObject();
            configuration["actionId"] = selectedAction.id; // Save the ID of the selected action
            configuration["actionName"] = selectedAction.name; // Save the name of the selected action
            configuration["actionArgument"] = textBox_Arguments.Text;
            configuration["actionGroup"] = selectedAction.group;
            configuration["actionEnabled"] = selectedAction.enabled;
            configuration["actionSubaction_count"] = selectedAction.subaction_count;
        }

        private void comboBox_ActionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            Models.Action selectedAction = (Models.Action)comboBox_ActionList.SelectedItem;
            label_actionId.Text = selectedAction.id.ToString();
            label_actionName.Text = selectedAction.name.ToString();
            label_actionGroup.Text = selectedAction.group.ToString();
            label_actionEnabled.Text = selectedAction.enabled.ToString();
            label_subactionCount.Text = selectedAction.subaction_count.ToString();
            SaveDate();
        }
    }
}