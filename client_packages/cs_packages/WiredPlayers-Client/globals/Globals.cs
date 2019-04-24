using RAGE;
using RAGE.Elements;
using WiredPlayers_Client.account;
using WiredPlayers_Client.vehicles;
using WiredPlayers_Client.jobs;
using WiredPlayers_Client.model;
using WiredPlayers_Client.factions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System;

namespace WiredPlayers_Client.globals
{
    class Globals : Events.Script
    {
        private DateTime lastTimeChecked;
        private string playerMoney;
        public static bool viewingPlayers;
        public static bool playerLogged;
        private static Dictionary<int, AttachmentModel> playerAttachments;

        public Globals()
        {
            Events.Add("updatePlayerList", UpdatePlayerListEvent);
            Events.Add("hideConnectedPlayers", HideConnectedPlayersEvent);
            Events.Add("changePlayerWalkingStyle", ChangePlayerWalkingStyleEvent);
            Events.Add("resetPlayerWalkingStyle", ResetPlayerWalkingStyleEvent);
            Events.Add("attachItemToPlayer", AttachItemToPlayerEvent);
            Events.Add("dettachItemFromPlayer", DettachItemFromPlayerEvent);
            Events.Add("playerLoggedIn", PlayerLoggedInEvent);

            Events.AddDataHandler("SERVER_TIME", PlayerConnectionStateChanged);

            Events.OnEntityStreamIn += OnEntityStreamInEvent;
            Events.OnEntityStreamOut += OnEntityStreamOutEvent;
            Events.Tick += TickEvent;

            playerAttachments = new Dictionary<int, AttachmentModel>();

            // Remove health regeneration
            RAGE.Game.Player.SetPlayerHealthRechargeMultiplier(0.0f);

            // Remove weapons from the vehicles
            RAGE.Game.Player.DisablePlayerVehicleRewards();

            // Remove the fade out after player's death
            RAGE.Game.Misc.SetFadeOutAfterDeath(false);

            // Freeze the player until he logs in
            Player.LocalPlayer.FreezePosition(true);
        }

        public static string EscapeJsonCharacters(string jsonString)
        {
            // Escape the apostrophe on JSON
            return jsonString.Replace("'", "\\'");
        }

        private void UpdatePlayerListEvent(object[] args)
        {
            if (!playerLogged || !viewingPlayers || Browser.customBrowser == null) return;

            // Update the player list
            Browser.ExecuteFunctionEvent(new object[] { "updatePlayerList", args[0].ToString() });
        }

        private void HideConnectedPlayersEvent(object[] args)
        {
            // Cancel the player list view
            viewingPlayers = false;

            // Destroy the browser
            Browser.DestroyBrowserEvent(null);
        }

        private void ChangePlayerWalkingStyleEvent(object[] args)
        {
            // Get the player
            Player player = (Player)args[0];
            string clipSet = args[1].ToString();

            player.SetMovementClipset(clipSet, 0.1f);
        }

        private void ResetPlayerWalkingStyleEvent(object[] args)
        {
            // Get the player
            Player player = (Player)args[0];

            player.ResetMovementClipset(0.0f);
        }

        private void AttachItemToPlayerEvent(object[] args)
        {
            // Get the remote player
            int playerId = Convert.ToInt32(args[0]);
            Player attachedPlayer = Entities.Players.GetAtRemote((ushort)playerId);

            // Check if the player is in the stream range
            if (Entities.Players.Streamed.Contains(attachedPlayer) || Player.LocalPlayer.Equals(attachedPlayer))
            {
                // Get the attachment
                AttachmentModel attachment = JsonConvert.DeserializeObject<AttachmentModel>(args[1].ToString());

                // Create the object for that player
                int boneIndex = attachedPlayer.GetBoneIndexByName(attachment.bodyPart);
                attachment.attach = new MapObject(Convert.ToUInt32(attachment.hash), attachedPlayer.Position, new Vector3(), 255, attachedPlayer.Dimension);
                RAGE.Game.Entity.AttachEntityToEntity(attachment.attach.Handle, attachedPlayer.Handle, boneIndex, attachment.offset.X, attachment.offset.Y, attachment.offset.Z, attachment.rotation.X, attachment.rotation.Y, attachment.rotation.Z, false, false, false, false, 2, true);

                // Add the attachment to the dictionary
                playerAttachments.Add(playerId, attachment);
            }
        }

        private void DettachItemFromPlayerEvent(object[] args)
        {
            // Get the remote player
            int playerId = Convert.ToInt32(args[0]);

            if (playerAttachments.ContainsKey(playerId))
            {
                // Get the attachment
                MapObject attachment = playerAttachments[playerId].attach;

                // Remove it from the player and world
                attachment.Destroy();
                playerAttachments.Remove(playerId);
            }
        }

        private void PlayerLoggedInEvent(object[] args)
        {
            // Show the player as logged
            playerLogged = true;
        }

        public static void OnEntityStreamInEvent(Entity entity)
        {
            if (entity.Type == RAGE.Elements.Type.Player)
            {
                // Get the identifier of the player
                int playerId = entity.RemoteId;
                Player attachedPlayer = Entities.Players.GetAtRemote((ushort)playerId);

                // Get the attachment on the right hand
                object attachmentJson = attachedPlayer.GetSharedData(Constants.ITEM_ENTITY_RIGHT_HAND);

                if(attachmentJson == null)
                {
                    // Check if the player has a crate
                    attachmentJson = attachedPlayer.GetSharedData(Constants.ITEM_ENTITY_WEAPON_CRATE);
                }

                if (attachmentJson != null)
                {
                    AttachmentModel attachment = JsonConvert.DeserializeObject<AttachmentModel>(attachmentJson.ToString());

                    // If the attached item is a weapon, we don't stream it
                    if (RAGE.Game.Weapon.IsWeaponValid(Convert.ToUInt32(attachment.hash))) return;

                    int boneIndex = attachedPlayer.GetBoneIndexByName(attachment.bodyPart);
                    attachment.attach = new MapObject(Convert.ToUInt32(attachment.hash), attachedPlayer.Position, new Vector3(), 255, attachedPlayer.Dimension);
                    RAGE.Game.Entity.AttachEntityToEntity(attachment.attach.Handle, attachedPlayer.Handle, boneIndex, attachment.offset.X, attachment.offset.Y, attachment.offset.Z, attachment.rotation.X, attachment.rotation.Y, attachment.rotation.Z, false, false, false, true, 0, true);

                    // Add the attachment to the dictionary
                    playerAttachments.Add(playerId, attachment);
                }
            }
        }

        public static void OnEntityStreamOutEvent(Entity entity)
        {
            if (entity.Type == RAGE.Elements.Type.Player)
            {
                // Get the player's identifier
                int playerId = entity.RemoteId;

                if(playerAttachments.ContainsKey(playerId))
                {
                    // Get the attached object
                    MapObject attachment = playerAttachments[playerId].attach;

                    // Destroy the attachment
                    attachment.Destroy();
                    playerAttachments.Remove(playerId);
                }
            }
        }

        private void PlayerConnectionStateChanged(Entity entity, object arg)
        {
            if(entity == Player.LocalPlayer)
            {
                string[] serverTime = Player.LocalPlayer.GetSharedData("SERVER_TIME").ToString().Split(":");

                int hours = int.Parse(serverTime[0]);
                int minutes = int.Parse(serverTime[1]);
                int seconds = int.Parse(serverTime[2]);

                // Set the hour from the server
                RAGE.Game.Clock.SetClockTime(hours, minutes, seconds);

                // Get the current timestamp
                lastTimeChecked = DateTime.UtcNow;

                // Show the login window
                Login.AccountLoginFormEvent(null);
            }
        }

        private void TickEvent(List<Events.TickNametagData> nametags)
        {
            // Get the current time
            DateTime dateTime = DateTime.UtcNow;

            // Check if the player is connected
            if (playerLogged)
            {
                // Disable hitting with the weapon
                RAGE.Game.Pad.DisableControlAction(0, 140, true);
                RAGE.Game.Pad.DisableControlAction(0, 141, true);

                if (Vehicles.lastPosition != null)
                {
                    if (Player.LocalPlayer.Vehicle == null)
                    {
                        // He fell from the vehicle, save the data
                        Vehicles.RemoveSpeedometerEvent(null);
                    }
                    else
                    {
                        // Update the speedometer
                        Vehicles.UpdateSpeedometer();
                    }
                }

                // Update the player's money each 450ms
                if (dateTime.Ticks - lastTimeChecked.Ticks >= 4500000)
                {
                    // Check if the player is loaded
                    object money = Player.LocalPlayer.GetSharedData(Constants.HAND_MONEY);

                    if (money != null)
                    {
                        playerMoney = Convert.ToInt32(money) + "$";
                        lastTimeChecked = dateTime;
                    }
                }

                if (Fishing.fishingState > 0)
                {
                    // Start the fishing minigame
                    Fishing.DrawFishingMinigame();
                }

                // Draw the money
                RAGE.NUI.UIResText.Draw(playerMoney, 1900, 60, RAGE.Game.Font.Pricedown, 0.5f, Color.DarkOliveGreen, RAGE.NUI.UIResText.Alignment.Right, true, true, 0);

                // Check if the player
                if (RAGE.Game.Pad.IsControlJustPressed(0, (int)RAGE.Game.Control.VehicleSubPitchDownOnly) && Player.LocalPlayer.Vehicle != null)
                {
                    // Check if the player is on a forklift
                    Trucker.CheckPlayerStoredCrate();
                }

                // Check if the player is handcuffed
                if (Police.handcuffed)
                {
                    RAGE.Game.Pad.DisableControlAction(0, 12, true);
                    RAGE.Game.Pad.DisableControlAction(0, 13, true);
                    RAGE.Game.Pad.DisableControlAction(0, 14, true);
                    RAGE.Game.Pad.DisableControlAction(0, 15, true);
                    RAGE.Game.Pad.DisableControlAction(0, 16, true);
                    RAGE.Game.Pad.DisableControlAction(0, 17, true);
                    RAGE.Game.Pad.DisableControlAction(0, 22, true);
                    RAGE.Game.Pad.DisableControlAction(0, 24, true);
                    RAGE.Game.Pad.DisableControlAction(0, 25, true);
                }
            }

            // Detect if a key has been pressed
            int key = Keys.DetectPressedKey(dateTime.Ticks);

            if (key >= 0)
            {
                // Fire the event for the pressed key
                Keys.FireKeyPressed(key);
            }
        }
    }
}
