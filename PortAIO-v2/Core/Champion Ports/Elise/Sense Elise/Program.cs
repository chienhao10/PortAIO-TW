using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;
using SPrediction;

using EloBuddy; 
 using LeagueSharp.Common; 
 namespace Sense_Elise
{
    class Program
    {
        private static Menu Option;
        private static AIHeroClient Player;
        private static Orbwalking.Orbwalker orbWalker;
        private static string championName = "Elise";
        private static Spell Q, W, E, R, Q2, W2, E2;

        private static readonly float[] HumanQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] HumanWcd = { 12, 12, 12, 12, 12 };
        private static readonly float[] HumanEcd = { 14, 13, 12, 11, 10 };
        private static readonly float[] SpiderQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] SpiderWcd = { 12, 12, 12, 12, 12 };
        private static readonly float[] SpiderEcd = { 26, 23, 20, 17, 14 };
        private static float _humQcd = 0, _humWcd = 0, _humEcd = 0;
        private static float _spidQcd = 0, _spidWcd = 0, _spidEcd = 0;
        private static float _humaQcd = 0, _humaWcd = 0, _humaEcd = 0;
        private static float _spideQcd = 0, _spideWcd = 0, _spideEcd = 0;
        

        public static void Main()
        {
            Game_OnGameLoad();
        }

        static void Game_OnGameLoad()
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != championName) return;

            Chat.Print("<font color='#5CD1E5'>[ Sense Elise ] </font><font color='#FF0000'> Thank you for using this assembly \n<font color='#1DDB16'>if you have any feedback, let me know that.</font>");

            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 960f);
            E = new Spell(SpellSlot.E, 1075f);

            Q2 = new Spell(SpellSlot.Q, 475f);
            W2 = new Spell(SpellSlot.W);
            E2 = new Spell(SpellSlot.E, 750f);

            R = new Spell(SpellSlot.R);

            W.SetSkillshot(0.25f, 100f, 1000, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 55f, 1600, true, SkillshotType.SkillshotLine);

            MainMenu();
            Game.OnUpdate += OnUpate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;

        }

        static void OnUpate(EventArgs args)
        {
            if (Player.IsDead) return;

            KillSteal();
            Instant_Rappel();
            Cooldowns();

            switch (orbWalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
            }

            if (Option.Item("Spider Combo E").GetValue<KeyBind>().Active)
                CastSpiderE();
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Human() && Option_Item("GapCloser Human E") && E.IsReady())
            {
                if (ObjectManager.Player.Distance(gapcloser.Sender) <= E.Range)
                    E.Cast(gapcloser.Sender);
            }

            if (Spider() && Option_Item("GapCloser Spider E") && E2.IsReady())
            {
                if (ObjectManager.Player.Distance(gapcloser.Sender) <= E2.Range)
                    E2.Cast(gapcloser.Sender);
            }
        }

        static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Human() && Option_Item("Interrupt Human E") && E.IsReady())
            {
                if (ObjectManager.Player.Distance(sender) <= E.Range && args.DangerLevel >= Interrupter2.DangerLevel.Medium && E.GetPrediction(sender).Hitchance >= HitChance.Medium)
                    E.Cast(sender);
            }
        }

        static void Harass()
        {
            if (Player.ManaPercent <= Option.Item("HMana").GetValue<Slider>().Value)
            {
                if (Option_Item("Human Harass E"))
                    CastHumanE();

                if (Option_Item("Human Harass W"))
                    CastHumanW();

                if (Option_Item("Human Harass Q"))
                    CastHumanQ();
            }


            if (Option_Item("Spider Harass Q"))
                CastSpiderQ();
            /*
                        if (Option_Item("Spider Harass W"))
                            CastSpiderW();
                            */
        }

        static void LaneClear()
        {
            var Minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (Minions != null)
            {
                if (Human() && Player.ManaPercent >= Option.Item("LMana").GetValue<Slider>().Value)
                {
                    if (Option_Item("Human Lane W") && W.IsReady())
                    {
                        MinionManager.FarmLocation farmLocation = W.GetLineFarmLocation(Minions);
                        if (farmLocation.MinionsHit >= 3)
                            W.Cast(farmLocation.Position, true);
                    }

                    if (Option_Item("Human Lane Q") && Q.IsReady())
                    {
                        var Minion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health < W.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Minion != null)
                            Q.Cast(Minion, true);
                    }
                }
                if (Spider())
                {
                    if (Option_Item("Spider Lane Q") && Q2.IsReady())
                    {


                        var Minion = MinionManager.GetMinions(Q2.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health < W.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Minion != null)
                            Q2.Cast(Minion, true);

                    }

                    if (Option_Item("Spider Lane W") && W2.IsReady())
                    {
                        var Minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 150, MinionTypes.All, MinionTeam.NotAlly);
                        if (!Orbwalking.CanAttack() && Orbwalking.CanMove(10) && Minion != null)
                            W2.Cast(true);
                    }
                }
            }
            if (Minions == null) return;
        }

        static void JungleClear()
        {
            var JungleMinions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (JungleMinions.Count >= 1)
            {
                foreach (var Mob in JungleMinions)
                {
                    if (Human())
                    {
                        if (Option_Item("Jungle R") && R.IsReady())
                            if (!Q.IsReady() && !W.IsReady())
                                if ((_spideQcd == 0 && _spideWcd <= 1.8f) || _humaQcd >= 1.2f)
                                    R.Cast(true);

                        if (Player.ManaPercent >= Option.Item("JMana").GetValue<Slider>().Value)
                        {
                            if (Option_Item("Human Jungle W") && W.IsReady())
                            {
                                MinionManager.FarmLocation Mobs = W.GetCircularFarmLocation(JungleMinions);
                                if (JungleMinions.Count == 4)
                                {
                                    if (Mobs.MinionsHit >= 3)
                                        W.Cast(Mobs.Position, true);
                                }
                                if (JungleMinions.Count == 3)
                                {
                                    if (Mobs.MinionsHit >= 2)
                                        W.Cast(Mobs.Position, true);
                                }
;
                                if (JungleMinions.Count <= 2)
                                    W.Cast(Mob.Position, true);

                                if (JungleMinions.Count == 0) return;
                            }

                            if (Option_Item("Human Jungle Q") && Q.IsReady())
                                Q.CastOnUnit(Mob, true);
                        }
                    }

                    if (Spider())
                    {
                        if (Option_Item("Jungle R") && R.IsReady())
                            if (!Q2.IsReady() && !W2.IsReady() && !Player.HasBuff("EliseSpiderW") && Player.ManaPercent >= Option.Item("JMana").GetValue<Slider>().Value)
                                if ((_humaQcd == 0 && _humaWcd <= 1.5f) && (_spideQcd >= 1.4f || _spideWcd >= 1.8f) && (JungleMinions.Count == 1 && Mob.Health >= Q.GetDamage(Mob) || Mob.Health >= W.GetDamage(Mob)))
                                    R.Cast(true);

                        if (Option_Item("Spider Jungle Q") && Q.IsReady())
                            Q.CastOnUnit(Mob, true);

                        if (Option_Item("Spider Jugnle W") && W2.IsReady())
                        {
                            var JungleMinion = MinionManager.GetMinions(Player.ServerPosition, 150, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                            if (!Orbwalking.CanAttack() && Orbwalking.CanMove(10))
                                if (JungleMinion != null)
                                    W2.Cast(true);
                        }
                    }
                }
            }

            if (JungleMinions == null) return;
        }

        static void Combo()
        {

            if (Option_Item("Combo R"))
                CastR();

            if (Option_Item("Human Combo E"))
                CastHumanE();

            if (Option_Item("Human Combo W"))
                CastHumanW();

            if (Option_Item("Human Combo Q"))
                CastHumanQ();

            if (Option_Item("Spider Combo Q"))
                CastSpiderQ();
            /*
            if (Option_Item("Spider Combo W"))
                CastSpiderW();
*/
            if (Option_Item("Spider Combo E Auto"))
                CastSpiderAutoE();
        }

        static void CastHumanQ()
        {
            if (Human() && Q.IsReady())
            {

                var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);
                if (Target != null)
                    Q.CastOnUnit(Target, true);
            }
        }

        static void CastHumanW()
        {
            if (Human() && W.IsReady())
            {
                var Target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical, true);
                var prediction = W.GetPrediction(Target);
                if (Target != null)
                {
                    if (prediction.CollisionObjects.Count == 0 && prediction.Hitchance >= HitChance.Medium)
                        W.Cast(Target.ServerPosition, true);
                }
            }
        }

        static void CastHumanE()
        {
            if (Human() && E.IsReady())
            {

                var Target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical, true);
                var Dashing = E.GetPrediction(Target);
                if (Target != null)
                {
                    var HC = HitChance.VeryHigh;
                    switch (Option.Item("E Hitchance").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            HC = HitChance.Impossible;
                            break;
                        case 1:
                            HC = HitChance.Low;
                            break;
                        case 2:
                            HC = HitChance.Medium;
                            break;
                        case 3:
                            HC = HitChance.High;
                            break;
                        case 4:
                            HC = HitChance.VeryHigh;
                            break;
                    }

                    if (Option.Item("Prediction M").GetValue<StringList>().SelectedIndex == 0)
                    {

                        if (Target.CanMove && Player.Distance(Target) < E.Range * 0.95)
                            E.CastIfHitchanceEquals(Target, HC, true);

                        if (!Target.CanMove)
                            E.CastIfHitchanceEquals(Target, HC, true);
                    }

                    if (Option.Item("Prediction M").GetValue<StringList>().SelectedIndex == 1)
                    {
                        E.SPredictionCast(Target, HC);
                    }
                        
                }
            }
        }

        static void CastSpiderQ()
        {
            if (Spider() && Q2.IsReady())
            {
                var Target = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Magical,true );
                if (Target != null)
                    Q2.CastOnUnit(Target, true);
            }
        }

        /*
        static void CastSpiderW()
        {
            if (Spider() && W2.IsReady())
            {
                var target = TargetSelector.GetTarget(150, TargetSelector.DamageType.Magical);
                if (target != null)
                    if (!Orbwalking.CanAttack() && Orbwalking.CanMove(10))
                        W2.Cast(true);
            }
        }
            */

        static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (Spider() && unit.IsMe)
                if (W.IsReady())
                    if ((orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Option_Item("Spider Harass W")) || (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Option_Item("Spider Lane W")) ||
                        (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Option_Item("Spider Jungle W")) || (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Option_Item("Spider Combo W")))
                        if (target is AIHeroClient || target is Obj_AI_Minion || target is Obj_AI_Turret)
                            W.Cast();
        }
        static void CastSpiderE()
        {
            if (Spider() && E2.IsReady())
            {
                var Target = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.True, true);
                var EQtarget = TargetSelector.GetTarget(E2.Range + Q.Range, TargetSelector.DamageType.True, true);
                var sEMinions = MinionManager.GetMinions(Player.ServerPosition, E2.Range).FirstOrDefault();
                var sE2Minions = MinionManager.GetMinions(E2.Range + Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).FirstOrDefault(x => x.Distance(Player.Position) < Q.Range && Player.Distance(sEMinions.Position) < E2.Range);

                if (Target != null)
                {
                    if (Target.CanMove && Player.Distance(Target) < E2.Range - 10)
                        E2.Cast(Target, true);
                    if (!Target.CanMove)
                        E2.Cast(Target);
                }

                if (EQtarget != null)
                {
                    if (Target.CanMove && Player.Distance(Target) < (E2.Range + Q2.Range) - 10)
                        E2.Cast(sE2Minions, true);
                }

            }
        }

        static void CastSpiderAutoE()
        {
            if (Spider() && E2.IsReady())
            {
                var target = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.True, true);
                if (target != null)
                {
                    if ((!Q2.IsReady() || Q2.Range <= Player.Distance(target)) && !W2.IsReady())
                        if (target.HasBuffOfType(BuffType.Stun))
                            E2.Cast(true);
                }
            }
        }

        static void CastR()
        {
            var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);
            var Target2 = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Magical, true);

            if (Target != null && R.IsReady())
            {
                if (Human())
                {
                    if (!Q.IsReady() && !W.IsReady() && !E.IsReady())
                        if (_spideQcd == 0 && _spideWcd <= 1.8f)
                        {
                            if ((Target.Health <= Q.GetDamage(Target) && _humaQcd <= 1.5f) && (_humaQcd <= 1.2f || _humaWcd <= 2)) return;
                            else
                                R.Cast(true);
                        }
                }

                if (Spider())
                {
                    if (!Q2.IsReady() && !W2.IsReady() && !Player.HasBuff("EliseSpiderW"))
                        if (_humaQcd == 0 || (_humaWcd <= 1.5f && _humaEcd <= 0.8f))
                            if ((_spideQcd <= 1.0f && Target2.Health <= Q2.GetDamage(Target2)) || (_spideQcd <= 1.4f || _spideWcd <= 1.9f)) return;
                            else
                                R.Cast();
                }
            }
        }

        static void KillSteal()
        {
            if (Human())
            {
                if (Option_Item("KillSteal Human Q") && Q.IsReady())
                {
                    var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);
                    if (Qtarget != null && Qtarget.Health <= Q.GetDamage(Qtarget))
                        Q.CastOnUnit(Qtarget, true);
                }

                if (Option_Item("KillSteal Human W") && W.IsReady())
                {
                    var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical, true);
                    var prediction = W.GetPrediction(Wtarget);
                    if (Wtarget != null && Wtarget.Health <= W.GetDamage(Wtarget) && prediction.CollisionObjects.Count == 0)
                        W.Cast(Wtarget.ServerPosition, true);
                }
            }
            if (Spider())
            {
                if (Option_Item("KillSteal Spider Q") && Q2.IsReady())
                {
                    var Q2target = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Magical, true);
                    if (Q2target != null && Q2target.Health <= Q2.GetDamage(Q2target))
                        Q2.CastOnUnit(Q2target, true);
                }
            }

        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                GetCDs(args);
        }

        static void Instant_Rappel()
        {
            if (Option.Item("Fast Instant Rappel").GetValue<KeyBind>().Active)
            {
                if (Human() && R.IsReady())
                {
                    {
                        R.Cast();
                        E2.Cast();
                    }
                }
                if (Spider())
                    E2.Cast();
            }
        }

        static bool Option_Item(string itemname)
        {
            return Option.Item(itemname).GetValue<bool>();
        }

        static bool Human()
        {
            return Player.Spellbook.GetSpell(SpellSlot.Q).Name == "EliseHumanQ";
        }

        static bool Spider()
        {
            return Player.Spellbook.GetSpell(SpellSlot.Q).Name == "EliseSpiderQCast";
        }

        static void Cooldowns()
        {
            _humaQcd = ((_humQcd - Game.Time) > 0) ? (_humQcd - Game.Time) : 0;
            _humaWcd = ((_humWcd - Game.Time) > 0) ? (_humWcd - Game.Time) : 0;
            _humaEcd = ((_humEcd - Game.Time) > 0) ? (_humEcd - Game.Time) : 0;
            _spideQcd = ((_spidQcd - Game.Time) > 0) ? (_spidQcd - Game.Time) : 0;
            _spideWcd = ((_spidWcd - Game.Time) > 0) ? (_spidWcd - Game.Time) : 0;
            _spideEcd = ((_spidEcd - Game.Time) > 0) ? (_spidEcd - Game.Time) : 0;
        }

        static void GetCDs(GameObjectProcessSpellCastEventArgs spell)
        {
            if (Human())
            {
                if (spell.SData.Name == "EliseHumanQ")
                    _humQcd = Game.Time + CalculateCd(HumanQcd[Q.Level]);
                if (spell.SData.Name == "EliseHumanW")
                    _humWcd = Game.Time + CalculateCd(HumanWcd[W.Level]);
                if (spell.SData.Name == "EliseHumanE")
                    _humEcd = Game.Time + CalculateCd(HumanEcd[E.Level]);
            }
            if (Spider())
            {
                if (spell.SData.Name == "EliseSpiderQCast")
                    _spidQcd = Game.Time + CalculateCd(SpiderQcd[Q2.Level]);
                if (spell.SData.Name == "EliseSpiderW")
                    _spidWcd = Game.Time + CalculateCd(SpiderWcd[W2.Level]);
                if (spell.SData.Name == "EliseSpiderEInitial")
                    _spidEcd = Game.Time + CalculateCd(SpiderEcd[E2.Level]);
            }
        }

        static float CalculateCd(float time)
        {
            return time + (time * Player.PercentCooldownMod);
        }

        static float GetComboDamage(AIHeroClient Enemy)
        {
            float damage = 0;

            if (Q.IsReady())
                damage += Q.GetDamage(Enemy);
            if (W.IsReady())
                damage += W.GetDamage(Enemy);
            if (Q2.IsReady())
                damage += Q2.GetDamage(Enemy);
            if (W2.IsReady())
                damage += W2.GetDamage(Enemy);
            if (!Player.Spellbook.IsAutoAttacking)
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(Enemy, true);


            return damage;
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var elise = Drawing.WorldToScreen(Player.Position);
            if (Human())
            {
                if (Option_Item("Human Q Draw"))
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.White, 1);

                if (Option_Item("Human W Draw"))
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Yellow, 1);

                if (Option_Item("Human E Draw Range"))
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Green, 1);

                var ETarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.True, true);
                if (Option_Item("Human E Draw Target"))
                    if (ETarget != null)
                        Drawing.DrawCircle(ETarget.Position, 150, Color.Green);

                if (Option_Item("Spider Skill Cooldown"))
                {

                    if (_spideQcd == 0 && Q2.Level > 0)
                        Drawing.DrawText(elise[0] - 70, elise[1], Color.White, "S-Q Ready");
                    else
                        Drawing.DrawText(elise[0] - 70, elise[1], Color.Orange, "S-Q: " + _spideQcd.ToString("0.0"));

                    if (_spideWcd == 0 && W2.Level > 0)
                        Drawing.DrawText(elise[0] - 20, elise[1] + 30, Color.White, "S-W Ready");
                    else
                        Drawing.DrawText(elise[0] - 20, elise[1] + 30, Color.Orange, "S-W: " + _spideWcd.ToString("0.0"));

                    if (_spideEcd == 0 && E2.Level > 0)
                        Drawing.DrawText(elise[0] + 20, elise[1], Color.White, "S-E Ready");
                    else
                        Drawing.DrawText(elise[0] + 20, elise[1], Color.Orange, "S-E: " + _spideEcd.ToString("0.0"));
                }
            }

            if (Spider())
            {
                if (Option_Item("Spider Q Draw"))
                    Render.Circle.DrawCircle(Player.Position, Q2.Range, Color.White, 1);

                if (Option_Item("Spider E Draw Range"))
                    Render.Circle.DrawCircle(Player.Position, E2.Range, Color.Yellow, 1);

                var E2target = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.True, true);
                if (Option_Item("Spider E Draw Target"))
                    if (E2target != null)
                        Drawing.DrawCircle(E2target.Position, 150, Color.Green);


                var EQtarget = TargetSelector.GetTarget(E2.Range + Q2.Range, TargetSelector.DamageType.True, true);
                var sEMinions = MinionManager.GetMinions(Player.ServerPosition, E2.Range).FirstOrDefault();
                var sE2Minions = MinionManager.GetMinions(E2.Range + Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).FirstOrDefault(x => x.Distance(Player.Position) < Q.Range && Player.Distance(sEMinions.Position) < E2.Range);
                if (Option_Item("Spider EQ Draw Target"))
                    if (EQtarget != null && E2target == null && sE2Minions != null)
                        Drawing.DrawCircle(EQtarget.Position, 150, Color.Blue);

                if (Option_Item("Human Skill Cooldown"))
                {
                    if (_humaQcd == 0 && Q.Level > 0)
                        Drawing.DrawText(elise[0] - 70, elise[1], Color.White, "H-Q Ready");
                    else
                        Drawing.DrawText(elise[0] - 70, elise[1], Color.Orange, "H-Q: " + _humaQcd.ToString("0.0"));

                    if (_humaWcd == 0 && W.Level > 0)
                        Drawing.DrawText(elise[0] - 20, elise[1] + 30, Color.White, "H-W Ready");
                    else
                        Drawing.DrawText(elise[0] - 20, elise[1] + 30, Color.Orange, "H-W: " + _humaWcd.ToString("0.0"));

                    if (_humaEcd == 0 && E.Level > 0)
                        Drawing.DrawText(elise[0] + 20, elise[1], Color.White, "H-E Ready");
                    else
                        Drawing.DrawText(elise[0] + 20, elise[1], Color.Orange, "H-E: " + _humaEcd.ToString("0.0"));
                }
            }
        }

        static void MainMenu()
        {
            Option = new Menu("Sense Elise", "Sense_Elise", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.SkyBlue); ;

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Option.AddSubMenu(targetSelectorMenu);

            Option.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            orbWalker = new Orbwalking.Orbwalker(Option.SubMenu("Orbwalker"));

            var Prediction = new Menu("Prediction Mode", "Prediction Mode");
            {
                Prediction.AddItem(new MenuItem("Prediction M", "Prediction Mode").SetValue(new StringList(new[] { "Common", "Sprediction"}, 0)));
                Prediction.AddItem(new MenuItem("E Hitchance", "Human E Hitchance").SetValue(new StringList(new[] { "Impossible", "Low", "Medium", "High", "VeryHigh" }, 3)));
            }
            Option.AddSubMenu(Prediction);

            var Harass = new Menu("Harass", "Harass");
            {
                Harass.SubMenu("Human Skill").AddItem(new MenuItem("Human Harass Q", "Use Q").SetValue(true));
                Harass.SubMenu("Human Skill").AddItem(new MenuItem("Human Harass W", "Use W").SetValue(true));
                Harass.SubMenu("Human Skill").AddItem(new MenuItem("Human Harass E", "Use E").SetValue(true));
                Harass.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Harass Q", "Use Q").SetValue(true));
                Harass.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Harass W", "Use W").SetValue(true));
                Harass.AddItem(new MenuItem("HMana", "Mana Manager (%)").SetValue(new Slider(40)));
            }
            Option.AddSubMenu(Harass);

            var LaneClear = new Menu("LaneClear", "LaneClear");
            {
                LaneClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Lane Q", "Use Q").SetValue(true));
                LaneClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Lane W", "Use W").SetValue(true));
                LaneClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Lane E", "Use E").SetValue(true));
                LaneClear.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Lane Q", "Use Q").SetValue(true));
                LaneClear.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Lane W", "Use W").SetValue(true));
                LaneClear.AddItem(new MenuItem("LMana", "Mana Manager (%)").SetValue(new Slider(40)));
            }
            Option.AddSubMenu(LaneClear);

            var JungleClear = new Menu("JungleClear", "JungleClear");
            {
                JungleClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Jungle Q", "Use Q").SetValue(true));
                JungleClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Jungle W", "Use W").SetValue(true));
                JungleClear.SubMenu("Human Skill").AddItem(new MenuItem("Human Jungle E", "Use E").SetValue(true));
                JungleClear.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Jungle Q", "Use Q").SetValue(true));
                JungleClear.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Jungle W", "Use W").SetValue(true));
                JungleClear.AddItem(new MenuItem("Jungle R", "Auto Switch Form").SetValue(true));
                JungleClear.AddItem(new MenuItem("JMana", "Mana Manager (%)").SetValue(new Slider(40)));
            }
            Option.AddSubMenu(JungleClear);

            var Combo = new Menu("Combo", "Combo");
            {
                Combo.SubMenu("Human Skill").AddItem(new MenuItem("Human Combo Q", "Use Q").SetValue(true));
                Combo.SubMenu("Human Skill").AddItem(new MenuItem("Human Combo W", "Use W").SetValue(true));
                Combo.SubMenu("Human Skill").AddItem(new MenuItem("Human Combo E", "Use E").SetValue(true));
                Combo.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Combo Q", "Use Q").SetValue(true));
                Combo.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Combo W", "Use W").SetValue(true));
                Combo.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Combo E Auto", "Use Auto E").SetValue(false));
                Combo.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Combo E", "Use E").SetValue(new KeyBind('T', KeyBindType.Press)));
                Combo.AddItem(new MenuItem("Combo R", "Auto Switch Form").SetValue(true));
            }
            Option.AddSubMenu(Combo);

            var Misc = new Menu("Misc", "Misc");
            {
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal Human Q", "Use Q").SetValue(true));
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal Human W", "Use W").SetValue(false));
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal Spider Q", "Use Q").SetValue(true));
                Misc.SubMenu("Interrupt").AddItem(new MenuItem("Interrupt Human E", "Use Human E").SetValue(true));
                Misc.SubMenu("Anti-GapCloser").AddItem(new MenuItem("GapCloser Human E", "Use Human E").SetValue(true));
                Misc.SubMenu("Anti-GapCloser").AddItem(new MenuItem("GapCloser Spider E", "Use Spider E").SetValue(false));
                /*
                Misc.SubMenu("Smite").AddItem(new MenuItem("Smite Blue", "Smite Use Blue").SetValue(false));
                Misc.SubMenu("Smite").AddItem(new MenuItem("Smite Red", "Smite Use Red").SetValue(false));
                Misc.SubMenu("Smite").AddItem(new MenuItem("Smite Dragon", "Smite Use Dragon").SetValue(true));
                Misc.SubMenu("Smite").AddItem(new MenuItem("Smite Baron", "Smite Use Dragon").SetValue(true));
                Misc.SubMenu("Smite").AddItem(new MenuItem("Smite Enemy", "Smite Use Enemy(Click the Target)").SetValue(true));
                */
                Misc.AddItem(new MenuItem("Fast Instant Rappel", "Fast Instant_Rappel").SetValue(new KeyBind('G', KeyBindType.Press)));
            }
            Option.AddSubMenu(Misc);

            var Drawing = new Menu("Drawing", "Drawing");
            {
                Drawing.SubMenu("Human Skill").AddItem(new MenuItem("Human Q Draw", "Use Q").SetValue(false));
                Drawing.SubMenu("Human Skill").AddItem(new MenuItem("Human W Draw", "Use W").SetValue(false));
                Drawing.SubMenu("Human Skill").AddItem(new MenuItem("Human E Draw Range", "Use E Range").SetValue(false));
                Drawing.SubMenu("Human Skill").AddItem(new MenuItem("Human E Draw Target", "Use E Target").SetValue(true));
                Drawing.SubMenu("Human Skill").AddItem(new MenuItem("Human Skill Cooldown", "Skill Cooldown").SetValue(true));
                Drawing.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Q Draw", "Use Q").SetValue(false));
                Drawing.SubMenu("Spider Skill").AddItem(new MenuItem("Spider E Draw Range", "Use E Range").SetValue(false));
                Drawing.SubMenu("Spider Skill").AddItem(new MenuItem("Spider E Draw Target", "Use E Target").SetValue(true));
                Drawing.SubMenu("Spider Skill").AddItem(new MenuItem("Spider EQ Draw Minion", "Use EQ Target(Minion Jump)").SetValue(true));
                Drawing.SubMenu("Spider Skill").AddItem(new MenuItem("Spider Skill Cooldown", "Skill Cooldown").SetValue(true));
                Drawing.AddItem(new MenuItem("DamageAfterCombo", "Draw Combo Damage").SetValue(true));
            }
            Option.AddSubMenu(Drawing);

            Option.AddToMainMenu();
        }
    }
}