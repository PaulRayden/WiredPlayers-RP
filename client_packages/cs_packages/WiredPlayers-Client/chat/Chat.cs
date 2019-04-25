using System;
using RAGE;
using RAGE.Ui;
using WiredPlayers_Client.globals;

namespace WiredPlayers_Client.chat
{
    class Chat : Events.Script
    {
        public static bool Visible { get; private set; }
        public static bool Opened { get; private set; }
        public static bool Locked { get; private set; }
        public static HtmlWindow ChatBrowser { get; private set; }

        public Chat()
        {
            Events.OnPlayerCommand += OnPlayerCommandEvent;

            // TODO More information about how chat works needed
            /*
            // Register the events
            Events.Add("toggleChatLock", ToggleChatLockEvent);
            Events.Add("toggleChatOpen", ToggleChatOpenEvent);

            // Create the custom chat
            ChatBrowser = new HtmlWindow("package://statics/html/chat.html");
            ChatBrowser.MarkAsChat();
            RAGE.Chat.Colors = true;

            // Lock the chat
            Locked = true;*/
        }

        private void OnPlayerCommandEvent(string cmd, Events.CancelEventArgs cancel)
        {
            if(!Globals.playerLogged)
            {
                // Send the message to the player
                Events.CallRemote("playerNotLoggedCommand");

                // Cancel the command
                cancel.Cancel = true;
            }
            else
            {
                // Log the command
                Events.CallRemote("logPlayerCommand", cmd);
            }
        }

        public static void SetVisible(bool visible)
        {
            if (visible)
            {
                // Show the chat
            }
            else
            {
                // Hide the chat
                ChatBrowser.Active = false;
            }

            // Toggle the visible state
            Visible = visible;
        }

        public static void SetOpened(bool opened)
        {
            if (opened)
            {
                // Open the chat
                Cursor.Visible = true;
                RAGE.Chat.Show(true);
                RAGE.Chat.Activate(true);
                ChatBrowser.ExecuteJs("focusChat();");
            }
            else
            {
                // Close the chat
                Cursor.Visible = Browser.customBrowser != null;
                ChatBrowser.ExecuteJs("disableChatInput();");
            }

            // Toggle the open state
            Opened = opened;
        }

        public static void SetLocked(bool locked)
        {
            // Toggle the locked state
            Locked = locked;
        }

        private void ToggleChatLockEvent(object[] args)
        {
            // Get the locked state
            bool locked = Convert.ToBoolean(args[0]);

            // Change the state
            SetLocked(locked);
        }

        private void ToggleChatOpenEvent(object[] args)
        {
            // Get the locked state
            bool opened = Convert.ToBoolean(args[0]);

            // Change the state
            SetOpened(opened);
        }
    }
}
