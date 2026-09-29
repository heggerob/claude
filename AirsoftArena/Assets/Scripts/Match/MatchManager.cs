using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum Role { Soldier, Referee }

    public enum MatchPhase { Lobby, Countdown, Playing, Results }

    public class MatchSettings
    {
        public Role role = Role.Soldier;
        public WeaponData primary;
        public WeaponData secondary;
        /// <summary>The AI referee the players hire (Soldier role only).</summary>
        public RefereeProfile referee;
        public bool autoCallHits;
        public int teamSize = 4;
        public float duration = 180f;
        public int scoreLimit = 20;
        public MapDefinition map;
        public GameMode mode = GameMode.TeamDeathmatch;

        public int PlayerCount { get { return teamSize * 2; } }

        /// <summary>What the human pays up front to play.</summary>
        public int EntryCost { get { return role == Role.Soldier && referee != null ? referee.FeePerPlayer : 0; } }
    }

    public class MatchResult
    {
        public Role role;
        public int blueScore, redScore;
        public bool draw;
        public Team winner;
        public SoldierStats playerStats;
        public bool playerWon;
        public int moneyBefore, moneyAfter;
        public readonly List<string> moneyLines = new List<string>();
        public readonly List<string> xpLines = new List<string>();
        public int xpGained, levelBefore, levelAfter;
        public bool soundPlayed;
        public float skillBefore, skillAfter, honorBefore, honorAfter;

        public string refereeName;
        public int refCaught, refMissed, refWrongCalls, refCorrectCalls;
        public float refLobbyStars;
        public float refStarsBefore, refStarsAfter;
        public bool playerRatedReferee;
    }

    public class FeedEntry
    {
        public string text;
        public float time;
    }

    /// <summary>
    /// Runs a Team Deathmatch round: spawning, score, timer, hit calls, referee decisions and the payout afterwards.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        static readonly string[] BotNames =
        {
            "Sneaky Pete", "Big Lars", "Tommy Tactical", "Mall Ninja", "Camper Carl", "Rambo Ragnar",
            "Budget Bjorn", "Tryhard Tor", "Kjetil", "Sindre", "Magnus", "Ola Nordmann", "Gearhead Geir",
            "Sniper Sofie", "Hopup Henrik", "Loud Leif",
        };

        const float CountdownTime = 3f;
        const float OvershootGrace = 1.5f;
        const float NearMissRadius = 0.8f;
        const int CheatFine = 40;
        const int RefShotFine = 25;

        public MatchPhase Phase { get; private set; }
        public MatchSettings Settings { get; private set; }
        public readonly List<Soldier> Soldiers = new List<Soldier>();
        public Soldier PlayerSoldier { get; private set; }
        public RefereeNPC Referee { get; private set; }
        public PlayerRefereeController PlayerReferee { get; private set; }
        public readonly int[] Score = new int[2];
        public float TimeLeft { get; private set; }
        public float CountdownLeft { get { return Mathf.Max(0f, countdownEnd - Time.time); } }
        public bool Paused { get; private set; }
        public readonly List<FeedEntry> Feed = new List<FeedEntry>();
        public MatchResult LastResult { get; private set; }
        /// <summary>The rules of the current game mode (TDM, CTF, KOTH).</summary>
        public ModeRules Rules { get; private set; }

        public bool IsPlaying { get { return Phase == MatchPhase.Playing && !Paused; } }

        // Ground truth the referee is judged on.
        public int ZombieEpisodes { get; private set; }
        public int SelfResolved { get; private set; }
        public int Caught { get; private set; }
        public int CorrectCalls { get; private set; }
        public int WrongCalls { get; private set; }

        Transform matchRoot;
        float countdownEnd;
        int playerOvershootsSeen;

        void Awake()
        {
            Instance = this;
            Phase = MatchPhase.Lobby;
        }

        // ---------------------------------------------------------------- flow

        public void StartMatch(MatchSettings settings)
        {
            Cleanup();
            Settings = settings;
            Score[0] = Score[1] = 0;
            TimeLeft = settings.duration;
            ZombieEpisodes = SelfResolved = Caught = CorrectCalls = WrongCalls = 0;
            playerOvershootsSeen = 0;
            LastResult = new MatchResult { role = settings.role };

            var profile = PlayerProfile.Current;
            LastResult.moneyBefore = profile.money;
            LastResult.skillBefore = profile.skillRating;
            LastResult.honorBefore = profile.honor;
            if (settings.EntryCost > 0)
            {
                profile.money -= settings.EntryCost;
                LastResult.moneyLines.Add("Referee fee (" + settings.referee.name + ")  -$" + settings.EntryCost);
                PlayerProfile.Save();
            }

            if (settings.map == null) settings.map = MapLibrary.Get(profile.mapId);
            if (MapBuilder.Current != settings.map) MapBuilder.Build(transform, settings.map);

            matchRoot = new GameObject("Match").transform;
            matchRoot.SetParent(transform, false);

            var names = new List<string>(BotNames);
            for (int team = 0; team < 2; team++)
            {
                for (int i = 0; i < settings.teamSize; i++)
                {
                    bool human = settings.role == Role.Soldier && team == 0 && i == 0;
                    string name = human ? profile.playerName : TakeRandom(names);
                    var loadout = human
                        ? new[] { settings.primary, settings.secondary, WeaponCatalog.MeleeWeapons[0] }
                        : new[] { Pick(WeaponCatalog.Primaries), Pick(WeaponCatalog.Secondaries), WeaponCatalog.MeleeWeapons[0] };

                    var look = human ? profile.Look : CosmeticCatalog.RandomLook();
                    var soldier = Soldier.Create(matchRoot, name, (Team)team, human, loadout, look, RandomSpawnPoint((Team)team));
                    if (human)
                    {
                        var pc = soldier.gameObject.AddComponent<PlayerController>();
                        pc.AutoCallHits = settings.autoCallHits;
                        PlayerSoldier = soldier;
                    }
                    else
                    {
                        var bot = soldier.gameObject.AddComponent<BotController>();
                        bot.Skill = Random.Range(0.35f, 0.85f);
                        // Most players are honest. Some... are not.
                        soldier.Honesty = Random.value < 0.65f ? Random.Range(0.92f, 1f) : Random.Range(0.15f, 0.6f);
                    }
                    Soldiers.Add(soldier);
                }
            }

            if (settings.role == Role.Referee)
            {
                var me = new RefereeProfile { name = profile.playerName + " (Ref)", skill = 1f };
                Referee = RefereeNPC.Create(matchRoot, me, true, RefereeStart());
                PlayerReferee = Referee.gameObject.AddComponent<PlayerRefereeController>();
            }
            else
            {
                Referee = RefereeNPC.Create(matchRoot, settings.referee, false, RefereeStart());
            }

            BBSystem.Instance.Wind = Random.insideUnitCircle * Random.Range(0f, 2.5f);
            settings.scoreLimit = GameModes.ScoreLimit(settings.mode);
            Rules = GameModes.Create(settings.mode);
            Rules.Begin(this, matchRoot);
            AddFeed("<b>" + GameModes.Name(settings.mode) + "</b> on <b>" + settings.map.name + "</b>. Referee: <b>" + Referee.Profile.name + "</b>");

            var follow = CameraFollow.Instance;
            if (follow != null) follow.Follow(PlayerSoldier != null ? PlayerSoldier.transform : Referee.transform, 9f);

            Phase = MatchPhase.Countdown;
            countdownEnd = Time.time + CountdownTime;
        }

        public void SetPaused(bool paused)
        {
            if (Phase != MatchPhase.Playing) paused = false;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void EndMatch()
        {
            if (Phase != MatchPhase.Playing && Phase != MatchPhase.Countdown) return;
            SetPaused(false);
            Phase = MatchPhase.Results;
            foreach (var s in Soldiers) s.Freeze();
            Referee.ClearPending();
            Payout();
        }

        public void ReturnToLobby()
        {
            SetPaused(false);
            Cleanup();
            Phase = MatchPhase.Lobby;
            var follow = CameraFollow.Instance;
            if (follow != null) follow.ShowOverview();
        }

        void Cleanup()
        {
            if (matchRoot != null) Destroy(matchRoot.gameObject);
            matchRoot = null;
            Soldiers.Clear();
            PlayerSoldier = null;
            Referee = null;
            PlayerReferee = null;
            Rules = null;
            Feed.Clear();
            if (BBSystem.Instance != null) BBSystem.Instance.Clear();
            if (Effects.Instance != null) Effects.Instance.ClearAll();
        }

        void Update()
        {
            if (Phase == MatchPhase.Countdown && Time.time >= countdownEnd)
            {
                Phase = MatchPhase.Playing;
                AddFeed("<b>GAME ON!</b> Call your hits.");
            }

            if (Phase != MatchPhase.Playing) return;

            if (GameInput.Pressed(GameKey.Pause)) SetPaused(!Paused);
            if (Paused) return;

            TimeLeft -= Time.deltaTime;
            Rules.Tick(Time.deltaTime);
            if (TimeLeft <= 0f || Score[0] >= Settings.scoreLimit || Score[1] >= Settings.scoreLimit) EndMatch();
        }

        // ---------------------------------------------------------------- events from soldiers and BBs

        public void OnSoldierHit(Soldier victim, Soldier shooter, Vector2 point)
        {
            victim.LastHitSeenByReferee = Referee != null && Referee.CanSee(point);
            if (Referee != null) Referee.OnSoldierHit(victim, point);
        }

        public void OnHitNotCalled(Soldier soldier)
        {
            ZombieEpisodes++;
            if (Referee != null) Referee.OnHitNotCalled(soldier);
        }

        public void OnSoldierOut(Soldier soldier, OutReason reason, bool wasHit)
        {
            if (soldier.HasUncalledHit && (reason == OutReason.CalledHit || reason == OutReason.CalledLate)) SelfResolved++;

            if (Rules != null) Rules.OnSoldierOut(soldier);
            var shooter = soldier.LastHitBy;
            switch (reason)
            {
                case OutReason.CalledHit:
                case OutReason.CalledLate:
                    if (shooter != null) Award(shooter, soldier, reason == OutReason.CalledLate ? " (late call)" : "");
                    break;
                case OutReason.CaughtByReferee:
                    if (wasHit && shooter != null) Award(shooter, soldier, " (ref call)");
                    break;
            }
        }

        void Award(Soldier shooter, Soldier victim, string note)
        {
            if (shooter.Team == victim.Team)
            {
                // A hit is a hit, even from your own team. No point for it though.
                AddFeed("Friendly fire! " + Colored(shooter) + " hit teammate " + Colored(victim));
                return;
            }
            if (Rules.HitsScore) Score[(int)shooter.Team]++;
            shooter.Stats.pointsScored++;
            AddFeed(Colored(shooter) + " hit " + Colored(victim) + note);
        }

        /// <summary>The referee (AI or human) calls someone out. The ref's word is final, even when wrong.</summary>
        public void RefereeCallOut(Soldier soldier, RefereeNPC referee)
        {
            if (!soldier.InPlay || Phase != MatchPhase.Playing) return;

            bool zombie = soldier.HasUncalledHit;
            bool correct = zombie || soldier.State == SoldierState.Hit;
            referee.Whistle(soldier.DisplayName + ", you're OUT!");

            if (correct)
            {
                CorrectCalls++;
                if (zombie)
                {
                    Caught++;
                    // Cheating costs the team an extra point (in modes where hits score).
                    var other = Teams.Other(soldier.Team);
                    if (Rules.HitsScore)
                    {
                        Score[(int)other]++;
                        AddFeed("<color=#ffe14a>REF:</color> " + Colored(soldier) + " didn't call their hit! +1 penalty point to " + Teams.Name(other));
                    }
                    else
                    {
                        AddFeed("<color=#ffe14a>REF:</color> " + Colored(soldier) + " didn't call their hit!");
                    }
                    if (soldier.IsHuman) PlayerProfile.Current.honor -= 6f;
                }
                soldier.GoOut(OutReason.CaughtByReferee);
            }
            else
            {
                WrongCalls++;
                AddFeed("<color=#ffe14a>REF:</color> called out " + Colored(soldier) + "... who was never hit!");
                soldier.GoOut(OutReason.WrongReferee);
            }
        }

        public void OnOvershoot(Soldier shooter, Soldier victim)
        {
            if (Time.time - victim.OutSince < OvershootGrace) return;
            shooter.Stats.overshoots++;
            Effects.Text(victim.Position + new Vector2(0f, 1f), "OI! I'M OUT!", new Color(1f, 0.6f, 0.2f), 1.2f, 13);
            if (Referee == null || Referee.HumanControlled || !Referee.CanSee(victim.Position) || Random.value > Referee.Profile.skill) return;
            AddFeed("<color=#ffe14a>REF:</color> " + Colored(shooter) + " shot someone with their rag up. Warning!");
            if (shooter.IsHuman) playerOvershootsSeen++;
        }

        public void OnRefereeShot(Soldier shooter)
        {
            shooter.Stats.shotTheReferee++;
            AddFeed(Colored(shooter) + " shot the <color=#ffe14a>referee</color>. Fined $" + RefShotFine + ".");
        }

        public void OnBBLanded(Vector2 point, Soldier owner)
        {
            if (Phase != MatchPhase.Playing || owner == null || Referee == null || Referee.HumanControlled) return;
            foreach (var s in Soldiers)
            {
                if (s.Team == owner.Team) continue;
                if (Vector2.Distance(s.Position, point) < NearMissRadius) Referee.OnNearMiss(s);
            }
        }

        // ---------------------------------------------------------------- payout and ratings

        void Payout()
        {
            var r = LastResult;
            var profile = PlayerProfile.Current;
            r.blueScore = Score[0];
            r.redScore = Score[1];
            r.draw = Score[0] == Score[1];
            r.winner = Score[0] > Score[1] ? Team.Blue : Team.Red;

            int opportunities = Mathf.Max(0, ZombieEpisodes - SelfResolved);
            r.refCaught = Caught;
            r.refMissed = Mathf.Max(0, opportunities - Caught);
            r.refWrongCalls = WrongCalls;
            r.refCorrectCalls = CorrectCalls;
            r.refereeName = Referee.Profile.name;

            float accuracy = opportunities > 0 ? (float)Caught / opportunities : 1f;
            float performance = Mathf.Clamp01(accuracy - WrongCalls * 0.2f);
            // The players' combined verdict on the referee.
            r.refLobbyStars = Mathf.Clamp(1f + 4f * performance + Random.Range(-0.4f, 0.4f), 1f, 5f);

            if (Settings.role == Role.Soldier)
            {
                var referee = Settings.referee;
                r.refStarsBefore = referee.Stars;
                referee.matches++;
                referee.caught += r.refCaught;
                referee.missed += r.refMissed;
                referee.wrongCalls += r.refWrongCalls;
                referee.AddRating(r.refLobbyStars);
                r.refStarsAfter = referee.Stars;
                RefereeMarket.Save();

                var stats = PlayerSoldier.Stats;
                r.playerStats = stats;
                r.playerWon = !r.draw && r.winner == PlayerSoldier.Team;

                int result = r.draw ? 80 : r.playerWon ? 120 : 50;
                AddMoney(profile, r, result, r.draw ? "Draw" : r.playerWon ? "Win bonus" : "Participation");
                AddMoney(profile, r, stats.pointsScored * 6, "Hits scored x" + stats.pointsScored);
                AddMoney(profile, r, stats.captures * 30, "Flag captures x" + stats.captures);
                AddMoney(profile, r, stats.flagReturns * 10, "Flag returns x" + stats.flagReturns);
                AddMoney(profile, r, Mathf.FloorToInt(stats.hillSeconds / 3f), "Time on the hill " + Mathf.FloorToInt(stats.hillSeconds) + " s");
                AddMoney(profile, r, -stats.caughtByReferee * CheatFine, "Caught not calling hits x" + stats.caughtByReferee);
                AddMoney(profile, r, -stats.shotTheReferee * RefShotFine, "Shot the referee x" + stats.shotTheReferee);

                float matchScore = r.draw ? 0.5f : r.playerWon ? 1f : 0f;
                profile.skillRating = Mathf.Max(100f, profile.skillRating + 24f * (matchScore - 0.5f) + (stats.pointsScored - stats.timesHit) * 1.2f);
                profile.honor += stats.hitsCalled * 1f - playerOvershootsSeen * 2f - stats.shotTheReferee * 3f;
                profile.matches++;
                if (r.playerWon) profile.wins++;
            }
            else
            {
                r.refStarsBefore = profile.RefStars;
                int fee = profile.RefFeePerPlayer;
                AddMoney(profile, r, fee * Settings.PlayerCount, "Referee fee $" + fee + " x " + Settings.PlayerCount + " players");
                if (performance >= 0.8f) AddMoney(profile, r, 40, "Tips from happy players");
                profile.refStarsTotal += r.refLobbyStars;
                profile.refRatings++;
                profile.refMatches++;
                profile.refCorrectCalls += CorrectCalls;
                profile.refWrongCalls += WrongCalls;
                profile.refMissed += r.refMissed;
                r.refStarsAfter = profile.RefStars;
            }

            profile.honor = Mathf.Clamp(profile.honor, 0f, 100f);
            r.moneyAfter = profile.money;
            r.skillAfter = profile.skillRating;
            r.honorAfter = profile.honor;

            r.levelBefore = profile.Level;
            if (Settings.role == Role.Soldier)
                r.xpGained = Progression.SoldierXp(PlayerSoldier.Stats, r.playerWon, r.draw, r.xpLines);
            else
                r.xpGained = Progression.RefereeXp(CorrectCalls, WrongCalls, r.xpLines);
            // Rank-up cash lands in the money lines too.
            Progression.AddXp(profile, r.xpGained, r.moneyLines);
            r.levelAfter = profile.Level;
            r.moneyAfter = profile.money;
            PlayerProfile.Save();
        }

        static void AddMoney(PlayerProfile profile, MatchResult r, int amount, string label)
        {
            if (amount == 0) return;
            profile.money += amount;
            r.moneyLines.Add(label + "  " + (amount > 0 ? "+$" + amount : "-$" + (-amount)));
        }

        /// <summary>The human's own star rating of the hired referee, given on the results screen.</summary>
        public void RateReferee(int stars)
        {
            var r = LastResult;
            if (r == null || r.playerRatedReferee || Settings == null || Settings.role != Role.Soldier) return;
            Settings.referee.AddRating(stars);
            r.playerRatedReferee = true;
            r.refStarsAfter = Settings.referee.Stars;
            RefereeMarket.Save();
        }

        // ---------------------------------------------------------------- helpers

        public Rect SpawnZone(Team team) { return MapBuilder.SpawnZones[(int)team]; }
        public Vector2 SpawnCenter(Team team) { return SpawnZone(team).center; }
        public bool InSpawnZone(Team team, Vector2 point) { return SpawnZone(team).Contains(point); }

        static Vector2 RefereeStart()
        {
            var b = MapBuilder.Bounds;
            return new Vector2(b.center.x, b.yMin + 2f);
        }

        Vector2 RandomSpawnPoint(Team team)
        {
            Rect r = SpawnZone(team);
            return new Vector2(Random.Range(r.xMin + 0.6f, r.xMax - 0.6f), Random.Range(r.yMin + 0.6f, r.yMax - 0.6f));
        }

        static readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>();

        /// <summary>True when no full-height wall is between a and b. Low cover only counts when lowCoverBlocks is set.</summary>
        public static bool HasLineOfSight(Vector2 a, Vector2 b, bool lowCoverBlocks)
        {
            sightHits.Clear();
            Physics2D.Linecast(a, b, new ContactFilter2D().NoFilter(), sightHits);
            foreach (var hit in sightHits)
            {
                var obstacle = hit.collider.GetComponent<Obstacle>();
                if (obstacle != null && (obstacle.BlocksSight || lowCoverBlocks)) return false;
            }
            return true;
        }

        public void AddFeed(string text)
        {
            Feed.Add(new FeedEntry { text = text, time = Time.time });
            if (Feed.Count > 30) Feed.RemoveAt(0);
        }

        public static string Colored(Soldier s)
        {
            return "<color=" + Teams.Hex(s.Team) + ">" + s.DisplayName + "</color>";
        }

        static T Pick<T>(List<T> list) { return list[Random.Range(0, list.Count)]; }

        static string TakeRandom(List<string> list)
        {
            if (list.Count == 0) return "Bot";
            int i = Random.Range(0, list.Count);
            string s = list[i];
            list.RemoveAt(i);
            return s;
        }
    }
}
