using GTANetworkAPI;
using WiredPlayers.model;
using WiredPlayers.globals;
using WiredPlayers.house;
using WiredPlayers.messages.general;
using System.Collections.Generic;
using System.Linq;

namespace WiredPlayers.character
{
    public class PlayerData : Script
    {
        [RemoteEvent("retrieveBasicData")]
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

            if (jobModel.job == 0)
            {
                // Get the player's faction
                FactionModel factionModel = Constants.FACTION_RANK_LIST.Where(f => player.GetData(EntityData.PLAYER_FACTION) == f.faction && player.GetData(EntityData.PLAYER_RANK) == f.rank).First();

                if (factionModel.faction > 0)
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
            asker.TriggerEvent("showPlayerData", player.Value, player.Name, age, sex, money, bank, job, rank, asker == player || asker.GetData(EntityData.PLAYER_ADMIN_RANK) > Constants.STAFF_NONE);
        }

        [RemoteEvent("retrievePropertiesData")]
        public static void RetrievePropertiesDataEvent(Client player, Client target)
        {
            // Initialize the variables
            List<string> houseAddresses = new List<string>();
            string rentedHouse = string.Empty;

            // Get the houses where the player is the owner
            List<HouseModel> houseList = House.houseList.Where(h => h.owner == target.Name).ToList();

            foreach (HouseModel house in houseList)
            {
                // Add the name of the house to the list
                houseAddresses.Add(house.name);
            }

            if (target.GetData(EntityData.PLAYER_RENT_HOUSE) > 0)
            {
                // Get the name of the rented house
                int houseId = target.GetData(EntityData.PLAYER_RENT_HOUSE);
                rentedHouse = House.houseList.Where(h => h.id == houseId).First().name;
            }

            // Show the data for the player
            player.TriggerEvent("showPropertiesData", NAPI.Util.ToJson(houseAddresses), rentedHouse);
        }

        [RemoteEvent("retrieveVehiclesData")]
        public static void RetrieveVehiclesDataEvent(Client player, Client target)
        {
            // Initialize the variables
            List<string> houseAddresses = new List<string>();
            string rentedHouse = string.Empty;

            // Get the houses where the player is the owner
            List<HouseModel> houseList = House.houseList.Where(h => h.owner == target.Name).ToList();

            foreach (HouseModel house in houseList)
            {
                // Add the name of the house to the list
                houseAddresses.Add(house.name);
            }

            if (target.GetData(EntityData.PLAYER_RENT_HOUSE) > 0)
            {
                // Get the name of the rented house
                int houseId = target.GetData(EntityData.PLAYER_RENT_HOUSE);
                rentedHouse = House.houseList.Where(h => h.id == houseId).First().name;
            }

            // Show the data for the player
            player.TriggerEvent("showPropertiesData", NAPI.Util.ToJson(houseAddresses), rentedHouse);
        }
    }
}
