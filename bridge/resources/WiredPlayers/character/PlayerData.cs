﻿using GTANetworkAPI;
using System.Linq;
using WiredPlayers.model;
using WiredPlayers.globals;
using WiredPlayers.messages.general;
using WiredPlayers.factions;

namespace WiredPlayers.character
{
    public class PlayerData : Script
    {
        public static void RetrieveBasicDataEvent(Client asker, Client player)
        {
            // Get the basic data
            string age = player.GetData(EntityData.PLAYER_AGE) + GenRes.years;
            string sex = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE ? GenRes.sex_male : GenRes.sex_female;
            string money = player.GetSharedData(EntityData.PLAYER_MONEY) + "$";
            string bank = player.GetSharedData(EntityData.PLAYER_BANK) + "$";
            string job = GenRes.unemployed;
            string rank = string.Empty;

            // Get the job
            JobModel jobModel = Constants.JOB_LIST.Where(j => player.GetData(EntityData.PLAYER_JOB) == j.job).First();

            if (jobModel == null)
            {
                // Get the player's faction
                FactionModel factionModel = Constants.FACTION_RANK_LIST.Where(f => player.GetData(EntityData.PLAYER_FACTION) == f.faction && player.GetData(EntityData.PLAYER_RANK) == f.rank).First();

                if (factionModel != null)
                {
                    switch (factionModel.faction)
                    {
                        case Constants.FACTION_POLICE:
                            job = GenRes.police_faction;
                            break;
                        case Constants.FACTION_EMERGENCY:
                            job = GenRes.emergency_faction;
                            break;
                        case Constants.FACTION_NEWS:
                            job = GenRes.news_faction;
                            break;
                        case Constants.FACTION_TOWNHALL:
                            job = GenRes.townhall_faction;
                            break;
                        case Constants.FACTION_TAXI_DRIVER:
                            job = GenRes.transport_faction;
                            break;
                    }

                    // Set player's rank
                    rank = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE ? factionModel.descriptionMale : factionModel.descriptionFemale;
                }
            }
            else
            {
                // Set the player's job
                job = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE ? jobModel.descriptionMale : jobModel.descriptionFemale;
            }

            // Show the data for the player
            asker.TriggerEvent("showPlayerData", player.Value, age, sex, money, bank, job, rank, !Faction.IsPoliceMember(asker));
        }
    }
}
