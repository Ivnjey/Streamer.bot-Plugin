﻿using StreamerbotPlugin.GUI;
using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using System;

namespace StreamerbotPlugin.Actions
{
    public class StreamerBotAction : PluginAction
    {
        private WebSocketClient webSocketClient = WebSocketClient.Instance;
        // The name of the action
        public override string Name => "Run Action";

        // A short description what the action can do
        public override string Description => "Select which Streamer.bot action to run.";

        // Optional; Add if this action can be configured. This will make the ActionConfigurator calling GetActionConfigurator();
        public override bool CanConfigure => true;

        // Optional; Add if you added CanConfigure; Gets called when the action can be configured and the action got selected in the ActionSelector. You need to return a user control with the "ActionConfigControl" class as base class
        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new StreamerBotActionConfigurator(this, actionConfigurator);
        }

        // Gets called when the action is triggered by a button press or an event
        public override void Trigger(string clientId, ActionButton actionButton)
        {
            DoStreamerbotAction(Configuration);
        }

        // Optional; Gets called when the action button gets deleted
        public override void OnActionButtonDelete()
        {

        }

        // Optional; Gets called when the action button is loaded
        public override void OnActionButtonLoaded()
        {

        }

        private async void DoStreamerbotAction(string configuration)
        {
            try
            {
                // Parse the configuration string into a JObject
                var configObject = JsonConvert.DeserializeObject<dynamic>(configuration);

                // Define the request object
                string json = "{\n" +
                    "  \"request\": \"DoAction\",\n" +
                    "  \"action\": {\n" +
                    $"    \"id\": \"{(string)configObject.actionId}\",\n" +
                    $"    \"name\": \"{(string)configObject.actionName}\"\n" +
                    "  },\n" +
                    "  \"args\": {\n" +
                    $"    \"key\": \"{(string)configObject.actionArgument}\"\n" +
                    "  },\n" +
                    $"  \"id\": \"{Guid.NewGuid()}\"\n" +
                    "}"; 

                await webSocketClient.SendMessageAsync(json);
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Trace(PluginInstance.Main, $"Failed to execute action. Message: {ex.Message}");
            }
        }
    }
}
