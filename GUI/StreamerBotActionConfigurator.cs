using StreamerbotPlugin.Models;
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
            _macroDeckAction = macroDeckAction;
            InitializeComponent();
            WebSocketClient.WebSocketOnMessageRecieved_actions += WebSocketClient_WebSocketOnMessageRecieved;
            GetActionList();
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
            comboBox_ActionList.Items.Clear();
            MacroDeckLogger.Info(PluginInstance.Main, $"Current Response: {message}");

            try
            {
                var jsonObject = JObject.Parse(message);

                if (jsonObject.TryGetValue("actions", out var actionsToken) && actionsToken is JArray actionsArray)
                {
                    foreach (var actionToken in actionsArray)
                    {
                        var action = new Models.Action(
                            actionToken["id"]?.ToString() ?? string.Empty,
                            actionToken["name"]?.ToString() ?? "Unnamed",
                            actionToken["group"]?.ToString() ?? "Ungrouped",
                            actionToken["enabled"]?.ToObject<bool>() ?? false,
                            actionToken["subaction_count"]?.ToObject<int>() ?? 0
                        );
                        comboBox_ActionList.Items.Add(action);
                    }
                }
                else
                {
                    MacroDeckLogger.Info(PluginInstance.Main, "No actions found in the message.");
                }

                if (_macroDeckAction?.Configuration != null)
                {
                    try
                    {
                        var configurationObject = JObject.Parse(_macroDeckAction.Configuration);

                        textBox_Arguments.Text = configurationObject["actionArgument"]?.ToString() ?? string.Empty;
                        label_actionId.Text = configurationObject["actionId"]?.ToString() ?? string.Empty;
                        label_actionName.Text = configurationObject["actionName"]?.ToString() ?? string.Empty;
                        label_actionGroup.Text = configurationObject["actionGroup"]?.ToString() ?? string.Empty;
                        label_actionEnabled.Text = configurationObject["actionEnabled"]?.ToString() ?? string.Empty;
                        label_subactionCount.Text = configurationObject["actionSubactionCount"]?.ToString() ?? string.Empty;

                        var selected = configurationObject["actionName"]?.ToString();
                        if (!string.IsNullOrEmpty(selected) && comboBox_ActionList.Items.Contains(selected))
                        {
                            comboBox_ActionList.Text = selected;
                        }
                        else if (comboBox_ActionList.Items.Count > 0)
                        {
                            comboBox_ActionList.SelectedIndex = 0;
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        MacroDeckLogger.Warning(PluginInstance.Main, $"Error parsing configuration: {ex.Message}");
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                MacroDeckLogger.Warning(PluginInstance.Main, $"Error parsing JSON message: {ex.Message}");
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Warning(PluginInstance.Main, $"Unexpected error: {ex.Message}");
            }
        }

        public override bool OnActionSave()
        {
            if (comboBox_ActionList.SelectedItem == null)
            {
                return false;
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

                if (string.IsNullOrEmpty(configuration["actionArgument"].ToString()))
                {
                    summary = $"Name - '{configuration["actionName"]}'";
                }
                else
                {
                    summary = $"Name - '{configuration["actionName"]}', Value - '{configuration["actionArgument"]}'";
                }

                _macroDeckAction.ConfigurationSummary = summary; // Set a summary of the configuration that gets displayed in the ButtonConfigurator item
                _macroDeckAction.Configuration = configuration.ToString();
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
            try
            {
                // Проверяем, что есть выбранный элемент
                // if (comboBox_ActionList.SelectedItem == null)
                // {
                //     ClearLabels(); // Метод для очистки меток
                //     return;
                // }

                // Пробуем привести выбранный элемент к типу Models.Action
                Models.Action selectedAction = comboBox_ActionList.SelectedItem as Models.Action;
                // if (selectedAction == null)
                // {
                //     ClearLabels();
                //     return;
                // }

                // Проверка каждого свойства selectedAction на null
                label_actionId.Text = selectedAction.id?.ToString() ?? "N/A";
                label_actionName.Text = selectedAction.name ?? "N/A";
                label_actionGroup.Text = selectedAction.group ?? "N/A";
                label_actionEnabled.Text = selectedAction.enabled.ToString() ?? "N/A";
                label_subactionCount.Text = selectedAction.subaction_count.ToString() ?? "0";

                // Сохранение данных, если всё корректно
                SaveDate();
            }
            catch (Exception ex)
            {
                // Логируем ошибку для диагностики
                MacroDeckLogger.Warning(PluginInstance.Main,$"Error in comboBox_ActionList_SelectedIndexChanged: {ex.Message}");
            }

        }
        private void ClearLabels()
        {
            label_actionId.Text = "N/A";
            label_actionName.Text = "N/A";
            label_actionGroup.Text = "N/A";
            label_actionEnabled.Text = "N/A";
            label_subactionCount.Text = "0";
        }
    }
}