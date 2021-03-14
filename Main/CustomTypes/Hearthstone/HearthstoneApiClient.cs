using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Linq;

namespace HerokuApp.Main.CustomTypes
{
    public static class HearthstoneApiClient
    {
        static readonly string accessToken;
        static readonly List<MinionInfo> battlegroundsMinionsInfos = new List<MinionInfo>();
        static readonly RestClient apiClient = new RestClient($"https://us.api.blizzard.com");

        public static List<AdditionalInfo> predefinedAdditionalInfos = GetAdditionalInfos();

        static HearthstoneApiClient()
        {
            var clientId = Config.GetConfigVariable("battlenet_client_id");
            var clientSecret = Config.GetConfigVariable("battlenet_client_secret");
            accessToken = GetAcessToken(clientId, clientSecret);
            battlegroundsMinionsInfos = LoadBattlegroundsMinionsInfos();
            battlegroundsMinionsInfos.ForEach(n => GetAdditionalInfo(n));
        }

        static void GetAdditionalInfo(MinionInfo minionInfo)
        {
            var predefMinion = predefinedAdditionalInfos.SingleOrDefault(n => n.Id == minionInfo.Id);
            if (predefMinion == null) return;
            minionInfo.AdditionalInfo = predefMinion;
        }

        static string GetAcessToken(string clientId, string clientSecret)
        {
            var client = new RestClient("https://us.battle.net/oauth/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("grant_type", "client_credentials");
            var battlenet_access_token = JObject.Parse(client.Execute(request).Content)["access_token"].Value<string>();
            return battlenet_access_token;
        }

        static List<MinionInfo> LoadBattlegroundsMinionsInfos()
        {
            var request = new RestRequest("hearthstone/cards", Method.GET);
            request.AddQueryParameter("locale", "ru_RU");
            request.AddQueryParameter("gameMode", "battlegrounds");
            request.AddQueryParameter("tier", "1,2,3,4,5,6");
            request.AddQueryParameter("access_token", accessToken);

            var battlegroundsMinions = new List<MinionInfo>();
            int currentPage = 0, pageCount;
            do
            {
                request.AddOrUpdateParameter("page", currentPage + 1);
                var json = JObject.Parse(apiClient.Execute(request).Content);
                var minions = json["cards"].Value<JArray>().ToObject<List<MinionInfo>>();
                battlegroundsMinions.AddRange(minions);
                currentPage = json["page"].Value<int>();
                pageCount = json["pageCount"].Value<int>();
            } while (currentPage < pageCount);
            return battlegroundsMinions;
        }

        static public List<MinionInfo> GetBattlegroundsMinions(
            MinionType type = MinionType.None,
            MinionKeyword keyword = MinionKeyword.None,
            MinionRarity rarity = MinionRarity.None,
            int? tavern = null)
        {
            List<MinionInfo> minions = battlegroundsMinionsInfos;
            if (type != MinionType.None)
            {
                if (type == MinionType.Without)
                {
                    minions = minions.Where(n => n.MinionType == type).ToList();
                }
                else
                {
                    minions = minions.Where(n => n.MinionType == type || n.MinionType == MinionType.All).ToList();
                }
            }
            if (tavern != null) minions = minions.Where(n => n.Battlegrounds.Tier == tavern).ToList();
            if (keyword != MinionKeyword.None)
            {
                minions = minions.Where(n => n.KeywordIds != null && n.KeywordIds.Contains(keyword)).ToList();
                if (keyword == MinionKeyword.Deathrattle) minions = minions.Where(n => n.AdditionalInfo != null && n.AdditionalInfo.DeathrattleEffect != null).ToList();
            }
            if (rarity != MinionRarity.None)
            {
                int? value;
                if (rarity == MinionRarity.Without)
                {
                    value = null;
                }
                else
                {
                    value = (int)rarity;
                }
                minions = minions.Where(n => n.Rarity == rarity).ToList();
            }
            return minions;
        }

        public static List<AdditionalInfo> GetAdditionalInfos()
        {
            #region YoHoOgre
            var YoHoOgre = new AdditionalInfo();
            YoHoOgre.Id = 61060;
            YoHoOgre.OnWasAttacked = (minion) => {
                if (!minion.IsDead) minion.AttackRandom();
            };
            #endregion

            #region ZappSlywick
            var ZappSlywick = new AdditionalInfo();
            ZappSlywick.Id = 60040;
            ZappSlywick.OnAttack = (opponentBoard) => {
                var minionWithLowestAttak = opponentBoard.GetMinions().OrderBy(n => n.AttackPoints).FirstOrDefault();
                return minionWithLowestAttak;
            };
            #endregion

            #region RedWhelp
            var RedWhelp = new AdditionalInfo();
            RedWhelp.Id = 59968;
            RedWhelp.OnBeforeFirstTurn = (minion, player) => {
                var opponentMinion = player.Opponent.Board.GetRandomMinion();
                opponentMinion.TakeDamage(1);
            };
            #endregion

            #region DrakonidEnforcer
            var DrakonidEnforcer = new AdditionalInfo();
            DrakonidEnforcer.Id = 61072;
            DrakonidEnforcer.OnSummoned = (minion, board) => {
                board.OnMinionLostDivineShield += (s, e) =>
                {
                    minion.AttackPoints += 2;
                    minion.HealthPoints += 2;
                };
            };
            #endregion

            #region Bolvar
            var Bolvar = new AdditionalInfo();
            Bolvar.Id = 45392;
            Bolvar.OnSummoned = (minion, board) => {
                board.OnMinionLostDivineShield += (s, e) =>
                {
                    minion.AttackPoints += 2;
                };
            };
            #endregion

            #region FiendishServant
            var FiendishServant = new AdditionalInfo();
            FiendishServant.Id = 56112;
            FiendishServant.DeathrattleEffect = (minion, owner) => {
                var randMinion = owner.Board.GetRandomMinion();
                if (randMinion == null) return;
                randMinion.AttackPoints += minion.AttackPoints;
            };
            #endregion

            #region GentleDjinni
            var GentleDjinni = new AdditionalInfo();
            GentleDjinni.Id = 64062;
            GentleDjinni.DeathrattleEffect = (minion, owner) => {
                var elems = GetBattlegroundsMinions(type: MinionType.Elemental);
                var elemsExceptThis = elems.Except(new List<MinionInfo> { minion.Info });
                var randomElem = elemsExceptThis.RandomElement();
                owner.Board.SummonBeside(randomElem, minion);
            };
            #endregion

            #region Ghastcoiler
            var Ghastcoiler = new AdditionalInfo();
            Ghastcoiler.Id = 59687;
            Ghastcoiler.DeathrattleEffect = (minion, owner) => {
                var deathrattleMinions = GetBattlegroundsMinions(keyword: MinionKeyword.Deathrattle);
                var firstMinion = deathrattleMinions.RandomElement();
                var secondMinion = deathrattleMinions.RandomElement();
                owner.Board.SummonBeside(firstMinion, minion);
                owner.Board.SummonBeside(secondMinion, minion);
            };
            #endregion

            #region Goldrinn
            var Goldrinn = new AdditionalInfo();
            Goldrinn.Id = 59955;
            Goldrinn.DeathrattleEffect = (minion, owner) => {
                var minionsOnBoard = owner.Board.GetMinions();
                foreach (var minionOnBoard in minionsOnBoard)
                {
                    if (minionOnBoard.Info.MinionType == MinionType.Beast)
                    {
                        minion.AttackPoints += 5;
                        minion.HealthPoints += 5;
                    }
                }
            };
            #endregion

            #region HarvestGolem
            var HarvestGolem = new AdditionalInfo();
            HarvestGolem.Id = 778;
            HarvestGolem.DeathrattleEffect = (minion, owner) => {
                var golemInfo = new MinionInfo() { Name = "Damaged Golem", Attack = 2, Health = 1 };
                owner.Board.SummonBeside(golemInfo, minion);
            };
            #endregion

            #region Imprisoner
            var Imprisoner = new AdditionalInfo();
            Imprisoner.Id = 59937;
            Imprisoner.DeathrattleEffect = (minion, owner) => {
                var impInfo = new MinionInfo() { Name = "Imp", Attack = 1, Health = 1 };
                owner.Board.SummonBeside(impInfo, minion);
            };
            #endregion

            #region InfestedWolf
            var InfestedWolf = new AdditionalInfo();
            InfestedWolf.Id = 38734;
            InfestedWolf.DeathrattleEffect = (minion, owner) => {
                var spiderInfo = new MinionInfo() { Name = "Spider", Attack = 1, Health = 1 };
                for (int i = 0; i < 2; i++)
                {
                    owner.Board.SummonBeside(spiderInfo, minion);
                }
            };
            #endregion

            #region KaboomBot
            var KaboomBot = new AdditionalInfo();
            KaboomBot.Id = 49279;
            KaboomBot.DeathrattleEffect = (minion, owner) => {
                var minionToDamage = owner.Opponent.Board.GetRandomMinion();
                if (minionToDamage == null) return;
                minionToDamage.TakeDamage(4);
            };
            #endregion

            #region KindlyGrandmother
            var KindlyGrandmother = new AdditionalInfo();
            KindlyGrandmother.Id = 39481;
            KindlyGrandmother.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Big Bad Wolf", Attack = 3, Health = 2 };
                owner.Board.SummonBeside(tokenInfo, minion);
            };
            #endregion

            #region KingBagurgle
            var KingBagurgle = new AdditionalInfo();
            KingBagurgle.Id = 60247;
            KingBagurgle.DeathrattleEffect = (minion, owner) => {
                var minionsOnBoard = owner.Board.GetMinions();
                foreach (var minionOnBoard in minionsOnBoard)
                {
                    if (minionOnBoard.Info.MinionType == MinionType.Murloc)
                    {
                        minionOnBoard.AttackPoints += 2;
                        minionOnBoard.HealthPoints += 2;
                    }
                }
            };
            #endregion

            #region MechanoEgg
            var MechanoEgg = new AdditionalInfo();
            MechanoEgg.Id = 49169;
            MechanoEgg.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Robosaur", Attack = 8, Health = 8 };
                owner.Board.SummonBeside(tokenInfo, minion);
            };
            #endregion

            #region NadinaTheRed
            var NadinaTheRed = new AdditionalInfo();
            NadinaTheRed.Id = 60629;
            NadinaTheRed.DeathrattleEffect = (minion, owner) => {
                var minionsOnBoard = owner.Board.GetMinions();
                foreach (var minionOnBoard in minionsOnBoard)
                {
                    if (minionOnBoard.Info.MinionType == MinionType.Dragon)
                    {
                        minionOnBoard.HasDivineShield = true;
                    }
                }
            };
            #endregion

            #region PilotedShredder
            var PilotedShredder = new AdditionalInfo();
            PilotedShredder.Id = 60048;
            PilotedShredder.DeathrattleEffect = (minion, owner) => {
                var twoCostTokenInfo = GetBattlegroundsMinions(tavern: 2).RandomElement();
                owner.Board.SummonBeside(twoCostTokenInfo, minion);
            };
            #endregion

            #region RatPack
            var RatPack = new AdditionalInfo();
            RatPack.Id = 40428;
            RatPack.DeathrattleEffect = (minion, owner) => {
                var ratInfo = new MinionInfo() { Name = "Rat", Attack = 1, Health = 1 };
                var numOfRats = minion.AttackPoints;
                numOfRats = numOfRats > 7 ? 7 : numOfRats;
                for (int i = 0; i < numOfRats; i++)
                {
                    owner.Board.SummonBeside(ratInfo, minion);
                }
            };
            #endregion

            #region ReplicatingMenace
            var ReplicatingMenace = new AdditionalInfo();
            ReplicatingMenace.Id = 48536;
            ReplicatingMenace.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Microbot", Attack = 1, Health = 1 };
                for (int i = 0; i < 3; i++)
                {
                    owner.Board.SummonBeside(tokenInfo, minion);
                }
            };
            #endregion      

            #region RingMatron
            var RingMatron = new AdditionalInfo();
            RingMatron.Id = 61884;
            RingMatron.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Imp", Attack = 3, Health = 2 };
                for (int i = 0; i < 2; i++)
                {
                    owner.Board.SummonBeside(tokenInfo, minion);
                }
            };
            #endregion

            #region SavannahHighmane
            var SavannahHighmane = new AdditionalInfo();
            SavannahHighmane.Id = 1261;
            SavannahHighmane.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Hyena", Attack = 2, Health = 2 };
                for (int i = 0; i < 2; i++)
                {
                    owner.Board.SummonBeside(tokenInfo, minion);
                }
            };
            #endregion

            #region Scallywag
            var Scallywag = new AdditionalInfo();
            Scallywag.Id = 61061;
            Scallywag.DeathrattleEffect = (minion, owner) => {
                var pirateInfo = new MinionInfo() { Name = "Pirate", Attack = 1, Health = 1 };
                var pirate = owner.Board.SummonBeside(pirateInfo, minion);
                if (pirate == null) return;
                pirate.AttackRandom();
            };
            #endregion

            #region SelflessHero
            var SelflessHero = new AdditionalInfo();
            SelflessHero.Id = 38740;
            SelflessHero.DeathrattleEffect = (minion, owner) => {
                var opponentMinions = owner.Board.GetMinions();
                var minionWithoutDivineShield = opponentMinions.Where(n => !n.HasDivineShield).RandomElement();
                minionWithoutDivineShield.HasDivineShield = true;
            };
            #endregion

            #region SneedOldShredder
            var SneedOldShredder = new AdditionalInfo();
            SneedOldShredder.Id = 59682;
            SneedOldShredder.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = GetBattlegroundsMinions(rarity: MinionRarity.Legendary).RandomElement();
                owner.Board.SummonBeside(tokenInfo, minion);
            };
            #endregion

            #region TheTideRazor
            var TheTideRazor = new AdditionalInfo();
            TheTideRazor.Id = 62232;
            TheTideRazor.DeathrattleEffect = (minion, owner) => {
                var pirates = GetBattlegroundsMinions(type: MinionType.Pirate);
                for (int i = 0; i < 3; i++)
                {
                    var pirateToken = pirates.RandomElement();
                    owner.Board.SummonBeside(pirateToken, minion);
                }
            };
            #endregion

            #region SpawnOfNZoth
            var SpawnOfNZoth = new AdditionalInfo();
            SpawnOfNZoth.Id = 38797;
            SpawnOfNZoth.DeathrattleEffect = (minion, owner) => {
                var minionsOnBoard = owner.Board.GetMinions();
                foreach (var minionOnBoard in minionsOnBoard)
                {
                    minionOnBoard.AttackPoints += 1;
                    minionOnBoard.HealthPoints += 1;
                }
            };
            #endregion

            #region Voidlord
            var Voidlord = new AdditionalInfo();
            Voidlord.Id = 46056;
            Voidlord.DeathrattleEffect = (minion, owner) => {
                var tokenInfo = new MinionInfo() { Name = "Demon", Attack = 1, Health = 3, HasTaunt = true };
                for (int i = 0; i < 3; i++)
                {
                    owner.Board.SummonBeside(tokenInfo, minion);
                }
            };
            #endregion

            #region UnstableGhoul
            var UnstableGhoul = new AdditionalInfo();
            UnstableGhoul.Id = 1808;
            UnstableGhoul.DeathrattleEffect = (minion, owner) => {
                var minionsOnBoard = owner.Board.GetMinions();
                var minionsOnOpponentBoard = owner.Opponent.Board.GetMinions();
                foreach (var minionOnBoard in minionsOnBoard.Concat(minionsOnOpponentBoard))
                {
                    minionOnBoard.TakeDamage(1);
                }
            };
            #endregion

            return new List<AdditionalInfo>
            {
                Bolvar,
                DrakonidEnforcer,
                FiendishServant,
                GentleDjinni,
                Ghastcoiler,
                Goldrinn,
                HarvestGolem,
                Imprisoner,
                InfestedWolf,
                KaboomBot,
                KindlyGrandmother,
                KingBagurgle,
                MechanoEgg,
                NadinaTheRed,
                PilotedShredder,
                RatPack,
                RedWhelp,
                ReplicatingMenace,
                RingMatron,
                SavannahHighmane,
                Scallywag,
                SelflessHero,
                SneedOldShredder,
                SpawnOfNZoth,
                TheTideRazor,
                Voidlord,
                YoHoOgre,
                UnstableGhoul,
                ZappSlywick,
            };
        }
    }
}