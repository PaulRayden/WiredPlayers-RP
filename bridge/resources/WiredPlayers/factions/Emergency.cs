﻿using GTANetworkAPI;
using WiredPlayers.model;
using WiredPlayers.globals;
using WiredPlayers.database;
using WiredPlayers.house;
using WiredPlayers.business;
using WiredPlayers.messages.error;
using WiredPlayers.messages.information;
using WiredPlayers.messages.success;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace WiredPlayers.factions
{
    public class Emergency : Script
    {
        public static List<BloodModel> bloodList;

        private void CreateEmergencyReport(DeathModel death)
        {
            if (death.killer.Value == Constants.ENVIRONMENT_KILL)
            {
                // Check if the player was dead
                int databaseKiller = death.player.GetSharedData(EntityData.PLAYER_KILLED);

                if (databaseKiller == 0)
                {
                    // There's no killer, we set the environment as killer
                    death.player.SetSharedData(EntityData.PLAYER_KILLED, Constants.ENVIRONMENT_KILL);
                }
            }
            else
            {
                int killerId = death.killer.GetData(EntityData.PLAYER_SQL_ID);
                death.player.SetSharedData(EntityData.PLAYER_KILLED, killerId);
            }

            // Warn the player
            death.player.SendChatMessage(Constants.COLOR_INFO + InfoRes.emergency_warn);
        }

        private int GetRemainingBlood()
        {
            int remaining = 0;
            foreach (BloodModel blood in bloodList)
            {
                if (blood.used)
                {
                    remaining--;
                }
                else
                {
                    remaining++;
                }
            }
            return remaining;
        }

        public static void CancelPlayerDeath(Client player)
        {
            NAPI.Player.SpawnPlayer(player, player.Position);
            player.SetSharedData(EntityData.PLAYER_KILLED, 0);
            player.ResetData(EntityData.TIME_HOSPITAL_RESPAWN);

            // Get the death warning
            FactionWarningModel factionWarn = Faction.GetFactionWarnByTarget(player.Value, Constants.FACTION_EMERGENCY);

            if (factionWarn != null)
            {
                if (factionWarn.takenBy >= 0)
                {
                    // Tell the player who attended the report it's been canceled
                    Client doctor = Globals.GetPlayerById(factionWarn.takenBy);
                    doctor.SendChatMessage(Constants.COLOR_INFO + InfoRes.faction_warn_canceled);
                }

                // Remove the report from the list
                Faction.factionWarningList.Remove(factionWarn);
            }

            // Change the death state
            player.TriggerEvent("togglePlayerDead", false);
        }

        private void TeleportPlayerToHospital(Client player)
        {
            player.Dimension = 0;
            player.Position = new Vector3(1841.702f, 3669.135f, 33.67997f);

            player.ResetData(EntityData.TIME_HOSPITAL_RESPAWN);
            player.SetData(EntityData.PLAYER_BUSINESS_ENTERED, 0);
            player.SetData(EntityData.PLAYER_HOUSE_ENTERED, 0);

            // Change the death state
            player.TriggerEvent("togglePlayerDead", false);
        }

        [ServerEvent(Event.PlayerDeath)]
        public void OnPlayerDeath(Client player, Client killer, uint weapon)
        {
            if(player.GetSharedData(EntityData.PLAYER_KILLED) == 0)
            {
                DeathModel death = new DeathModel(player, killer, weapon);

                Vector3 deathPosition = null;
                string deathPlace = string.Empty;
                string deathHour = DateTime.Now.ToString("h:mm:ss tt");

                // Checking if player died into a house or business
                if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) > 0)
                {
                    int houseId = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                    HouseModel house = House.GetHouseById(houseId);
                    deathPosition = house.position;
                    deathPlace = house.name;
                }
                else if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0)
                {
                    int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                    BusinessModel business = Business.GetBusinessById(businessId);
                    deathPosition = business.position;
                    deathPlace = business.name;
                }
                else
                {
                    deathPosition = player.Position;
                }

                if(killer.Value == Constants.ENVIRONMENT_KILL || killer == player)
                {
                    // We add the report to the list
                    FactionWarningModel factionWarning = new FactionWarningModel(Constants.FACTION_EMERGENCY, player.Value, deathPlace, deathPosition, -1, deathHour);
                    Faction.factionWarningList.Add(factionWarning);

                    // Report message
                    string warnMessage = string.Format(InfoRes.emergency_warning, Faction.factionWarningList.Count - 1);

                    // Sending the report to all the emergency department's members
                    foreach (Client target in NAPI.Pools.GetAllPlayers())
                    {
                        if (target.GetData(EntityData.PLAYER_FACTION) == Constants.FACTION_EMERGENCY && target.GetData(EntityData.PLAYER_ON_DUTY) > 0)
                        {
                            target.SendChatMessage(Constants.COLOR_INFO + warnMessage);
                        }
                    }

                    // Create the emergency report
                    CreateEmergencyReport(death);
                }
                else
                {
                    int killerId = killer.GetData(EntityData.PLAYER_SQL_ID);
                    player.SetSharedData(EntityData.PLAYER_KILLED, killerId);
                }

                // Time to let player accept his dead
                player.SetData(EntityData.TIME_HOSPITAL_RESPAWN, Globals.GetTotalSeconds() + 240);

                // Set the player into dead state
                player.TriggerEvent("togglePlayerDead", true);
            }
        }

        [Command(Commands.COM_HEAL, Commands.HLP_HEAL_COMMAND)]
        public void HealCommand(Client player, string targetString)
        {
            Client target = int.TryParse(targetString, out int targetId) ? Globals.GetPlayerById(targetId) : NAPI.Player.GetPlayerFromName(targetString);

            if(target == null)
            {
                // The player is not connected
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                return;
            }

            if(player.GetData(EntityData.PLAYER_FACTION) != Constants.FACTION_EMERGENCY)
            {
                // The player is not a medic
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_emergency_faction);
                return;
            }

            if(target.Health >= 100)
            {
                // The target player is not injured
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_hurt);
                return;
            }

            // We heal the character
            target.Health = 100;

            foreach (Client targetPlayer in NAPI.Pools.GetAllPlayers())
            {
                if (targetPlayer.Position.DistanceTo(player.Position) < 20.0f)
                {
                    string message = string.Format(InfoRes.medic_reanimated, player.Name, target.Name);
                    targetPlayer.SendChatMessage(Constants.COLOR_CHAT_ME + message);
                }
            }


            string playerMessage = string.Format(InfoRes.medic_healed_player, target.Name);
            string targetMessage = string.Format(InfoRes.player_healed_medic, player.Name);
            player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
            target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
        }

        [Command(Commands.COM_REANIMATE, Commands.HLP_REANIMATE_COMMAND)]
        public void ReanimateCommand(Client player, string targetString)
        {
            if (player.GetData(EntityData.PLAYER_FACTION) != Constants.FACTION_EMERGENCY)
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_emergency_faction);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0)
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetSharedData(EntityData.PLAYER_KILLED) != 0)
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else
            {
                Client target = int.TryParse(targetString, out int targetId) ? Globals.GetPlayerById(targetId) : NAPI.Player.GetPlayerFromName(targetString);

                if (target != null)
                {
                    if (target.GetSharedData(EntityData.PLAYER_KILLED) != 0)
                    {
                        if (GetRemainingBlood() > 0)
                        {
                            CancelPlayerDeath(target);

                            // We create blood model
                            BloodModel bloodModel = new BloodModel();
                            {
                                bloodModel.doctor = player.GetData(EntityData.PLAYER_SQL_ID);
                                bloodModel.patient = target.GetData(EntityData.PLAYER_SQL_ID);
                                bloodModel.type = string.Empty;
                                bloodModel.used = true;
                            }

                            Task.Factory.StartNew(() =>
                            {
                                // Add the blood consumption to the database
                                bloodModel.id = Database.AddBloodTransaction(bloodModel);
                                bloodList.Add(bloodModel);

                                // Send the confirmation message to both players
                                string playerMessage = string.Format(InfoRes.player_reanimated, target.Name);
                                string targetMessage = string.Format(SuccRes.target_reanimated, player.Name);
                                player.SendChatMessage(Constants.COLOR_ADMIN_INFO + playerMessage);
                                target.SendChatMessage(Constants.COLOR_SUCCESS + targetMessage);
                            });
                        }
                        else
                        {
                            // There's no blood left
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_blood_left);
                        }
                    }
                    else
                    {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_dead);
                    }
                }
                else
                {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_EXTRACT, Commands.HLP_EXTRACT_COMMAND)]
        public void ExtractCommand(Client player, string targetString)
        {
            if (player.GetSharedData(EntityData.PLAYER_KILLED) != 0)
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0)
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else
            {
                Client target = int.TryParse(targetString, out int targetId) ? Globals.GetPlayerById(targetId) : NAPI.Player.GetPlayerFromName(targetString);

                if (target != null && player.GetData(EntityData.PLAYER_FACTION) == Constants.FACTION_EMERGENCY)
                {
                    if (target.Health > 15)
                    {
                        // We create the blood model
                        BloodModel blood = new BloodModel();
                        {
                            blood.doctor = player.GetData(EntityData.PLAYER_SQL_ID);
                            blood.patient = target.GetData(EntityData.PLAYER_SQL_ID);
                            blood.type = string.Empty;
                            blood.used = false;
                        }

                        Task.Factory.StartNew(() =>
                        {
                            // We add the blood unit to the database
                            blood.id = Database.AddBloodTransaction(blood);
                            bloodList.Add(blood);

                            target.Health -= 15;
                            
                            string playerMessage = string.Format(InfoRes.blood_extracted, target.Name);
                            string targetMessage = string.Format(InfoRes.blood_extracted, player.Name);
                            player.SendChatMessage(playerMessage);
                            target.SendChatMessage(targetMessage);
                        });
                    }
                    else
                    {
                        player.SendChatMessage(ErrRes.low_blood);
                    }
                }
                else
                {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_DIE)]
        public void DieCommand(Client player)
        {
            // Check if the player is dead
            if (player.GetData(EntityData.TIME_HOSPITAL_RESPAWN) != null)
            {
                int totalSeconds = Globals.GetTotalSeconds();

                if (player.GetData(EntityData.TIME_HOSPITAL_RESPAWN) <= totalSeconds)
                {
                    // Move player to the hospital
                    TeleportPlayerToHospital(player);

                    // Get the report generated with the death
                    FactionWarningModel factionWarn = Faction.GetFactionWarnByTarget(player.Value, Constants.FACTION_EMERGENCY);

                    if (factionWarn != null)
                    {
                        if (factionWarn.takenBy >= 0)
                        {
                            // Tell the player who attended the report it's been canceled
                            Client doctor = Globals.GetPlayerById(factionWarn.takenBy);
                            doctor.SendChatMessage(Constants.COLOR_INFO + InfoRes.faction_warn_canceled);
                        }

                        // Remove the report from the list
                        Faction.factionWarningList.Remove(factionWarn);
                    }

                }
                else
                {
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.death_time_not_passed);
                }
            }
            else
            {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_dead);
            }
        }
    }
}