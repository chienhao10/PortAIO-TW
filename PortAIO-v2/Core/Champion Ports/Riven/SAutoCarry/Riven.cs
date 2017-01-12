﻿using System;
using System.Linq;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.PluginBase;
using SPrediction;
using SCommon.Maths;
using SCommon.Database;
using SCommon.Evade;

using SAutoCarry.Champions.Helpers;
using SharpDX;
using EloBuddy;

namespace SAutoCarry.Champions
{
    public class Riven : SCommon.PluginBase.Champion
    {
        public bool IsDoingFastQ { get; set; }
        public bool IsCrestcentReady
        {
            get { return (Items.HasItem(3077) && Items.CanUseItem(3077)) || (Items.HasItem(3074) && Items.CanUseItem(3074)) || (Items.HasItem(3748) && Items.CanUseItem(3748)); }
        }

        public TargetedSpellEvader m_targetedEvader;
        public SpellSlot SummonerFlash = ObjectManager.Player.GetSpellSlot("summonerflash");
        private Dictionary<string, StringList> ComboMethodBackup = new Dictionary<string, StringList>();

        public Riven()
            : base ("Riven", "SAutoCarry - Riven")
        {
            OnUpdate += BeforeOrbwalk;
            OnDraw += BeforeDraw;
            OnCombo += Combo;
            OnHarass += Combo; //same function because harass mode is just same combo w/o flash & r (which already implemented in combo)
            OnLaneClear += LaneClear;
            OnLastHit += LastHit;

            SCommon.Orbwalking.Events.AfterAttack += Animation.AfterAttack;
            AIHeroClient.OnPlayAnimation += Animation.OnPlay;
            Animation.OnAnimationCastable += Animation_OnAnimationCastable;
            Game.OnWndProc += Game_OnWndProc;
            //SCommon.Prediction.predMenu.Item("SPREDDRAWINGS").SetValue(false);

            IsDoingFastQ = false;
            //Orbwalker.Configuration.DontMoveInRange = true;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("連招", "combo");
            combo.AddItem(new MenuItem("CDISABLER", "關閉 R 使用").SetValue(new KeyBind('J', KeyBindType.Toggle)))
                    .ValueChanged += (s, ar) =>
                    {
                        ConfigMenu.Item("CR1MODE").Show(!ar.GetNewValue<KeyBind>().Active);
                        ConfigMenu.Item("CR2MODE").Show(!ar.GetNewValue<KeyBind>().Active);
                    };
            combo.AddItem(new MenuItem("CR1MODE", "R1 模式").SetValue(new StringList(new string[] { "總是", "當可擊殺時R2", "智能" }))).Show(!combo.Item("CDISABLER").GetValue<KeyBind>().Active);
            combo.AddItem(new MenuItem("CR2MODE", "R2 模式").SetValue(new StringList(new string[] { "總是", "當可擊殺時", "如果超出範圍", "最大傷害" }, 3))).Show(!combo.Item("CDISABLER").GetValue<KeyBind>().Active);
            combo.AddItem(new MenuItem("CEMODE", "E 模式").SetValue(new StringList(new string[] { "E 至敵人", "E 鼠標位置", "E 退回", "不使用 E" }, 0)));
            combo.AddItem(new MenuItem("CALWAYSE", "連招總是使用E").SetTooltip("為更好的使用連招").SetValue(false));
            combo.AddItem(new MenuItem("CUSEF", "使用閃現爆發連招").SetValue(new KeyBind('G', KeyBindType.Toggle))).Permashow();

            Menu comboType = new Menu("連招模式", "combomethod");
            foreach (var enemy in HeroManager.Enemies)
            {
                ComboMethodBackup.Add(String.Format("CMETHOD{0}", enemy.ChampionName), new StringList(new string[] { "預設", "Shy 爆發連招", "閃現爆發連招" }));
                comboType.AddItem(new MenuItem(String.Format("CMETHOD{0}", enemy.ChampionName), enemy.ChampionName).SetValue(new StringList(new string[] { "預設", "Shy 爆發連招", "閃現爆發連招" })))
                    .ValueChanged += (s, ar) =>
                    {
                        if (!comboType.Item("CSHYKEY").GetValue<KeyBind>().Active && !comboType.Item("CFLASHKEY").GetValue<KeyBind>().Active)
                            ComboMethodBackup[((MenuItem)s).Name] = ar.GetNewValue<StringList>();
                    };
            }
            comboType.AddItem(new MenuItem("CSHYKEY", "在按熱鍵使用Shy模式爆發連招").SetValue(new KeyBind('T', KeyBindType.Press))).ValueChanged += (s, ar) => Orbwalker.Configuration.Combo = ar.GetNewValue<KeyBind>().Active;
            comboType.AddItem(new MenuItem("CFLASHKEY", "在按熱鍵使用閃現爆發連招").SetValue(new KeyBind('Z', KeyBindType.Press)));
            combo.AddSubMenu(comboType);


            Menu harass = new Menu("騷擾", "harass");
            harass.AddItem(new MenuItem("HEMODE", "E 模式:").SetValue(new StringList(new string[] { "E 至敵人", "E 鼠標位置", "E 退回", "不使用 E" }, 0)));


            Menu laneclear = new Menu("清線/清野", "laneclear");
            laneclear.AddItem(new MenuItem("LUSEQ", "使用 Q").SetValue(true));
            laneclear.AddItem(new MenuItem("LUSEW", "使用 W").SetValue(true))
                .ValueChanged += (s, ar) =>
                {
                    laneclear.Item("LMINW").Show(ar.GetNewValue<bool>());
                };
            laneclear.AddItem(new MenuItem("LMINW", "x數量使用 W").SetValue(new Slider(1, 1, 6))).Show(laneclear.Item("LUSEW").GetValue<bool>());
            laneclear.AddItem(new MenuItem("LUSETIAMAT", "使用海神斧/九頭蛇").SetValue(true));
            laneclear.AddItem(new MenuItem("LSEMIQJUNG", "使用Q清野").SetValue(true));
            laneclear.AddItem(new MenuItem("LASTUSETIAMAT", "使用海神斧/九頭蛇農兵").SetValue(true));

            Menu misc = new Menu("雜項", "misc");
            misc.AddItem(new MenuItem("MFLEEKEY", "逃跑熱鍵").SetValue(new KeyBind('A', KeyBindType.Press)));
            misc.AddItem(new MenuItem("MFLEEWJ", "使用過牆，而逃跑").SetValue(true)).Permashow();
            misc.AddItem(new MenuItem("MKEEPQ", "保持 Q 靈活 (至鼠標)").SetValue(false));
            misc.AddItem(new MenuItem("MMINDIST", "防突進(目標距離)").SetValue(new Slider(390, 250, 750)));
            misc.AddItem(new MenuItem("MAUTOINTRW", "使用 W 中斷技能").SetValue(true));
            misc.AddItem(new MenuItem("MAUTOINTRQ", "嘗試中斷技能使用 & Q3").SetValue(false));
            misc.AddItem(new MenuItem("MANTIGAPW", "使用 W 防突進").SetValue(true));
            misc.AddItem(new MenuItem("MANTIGAPQ", "嘗試防突進使用 & Q3").SetValue(false));
            misc.AddItem(new MenuItem("MAUTOANIMCANCEL", "自動取消動畫").SetValue(true));
            misc.AddItem(new MenuItem("DDRAWCOMBOMODE", "顯示連招模式").SetValue(true));
            misc.AddItem(new MenuItem("DRAWULTSTATUS", "顯示 R 狀態").SetValue(true));

            m_targetedEvader = new TargetedSpellEvader(TargetedSpell_Evade, misc);
            //DamageIndicator.Initialize((t) => (float)CalculateComboDamage(t) + (float)CalculateDamageR2(t), misc);


            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddSubMenu(laneclear);
            ConfigMenu.AddSubMenu(misc);
            ConfigMenu.AddToMainMenu();

            ComboInstance.Initialize(this);
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 260f);
            Spells[W] = new Spell(SpellSlot.W, 230f);
            Spells[E] = new Spell(SpellSlot.E, 270f);
            Spells[R] = new Spell(SpellSlot.R, 900f);
            Spells[R].SetSkillshot(0.25f, 225f, 1600f, false, SkillshotType.SkillshotCone);
        }

        public void BeforeOrbwalk()
        {
            if (!Spells[Q].IsReady(1000))
            {
                Animation.QStacks = 0;
                IsDoingFastQ = false;
            }

            if (!Spells[R].IsReady())
                Animation.UltActive = false;

            if (ConfigMenu.Item("MFLEEKEY").GetValue<KeyBind>().Active)
                Flee();

            if (ConfigMenu.Item("CSHYKEY").GetValue<KeyBind>().Active)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != 1)
                    {
                        typeVal.SelectedIndex = 1;
                        ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
                var target = Target.Get(1000);
                //Orbwalker.Orbwalk(TargetSelector.SelectedTarget);
                //Combo();
                return;
            }
            else if (ConfigMenu.Item("CFLASHKEY").GetValue<KeyBind>().Active)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != 2)
                    {
                        typeVal.SelectedIndex = 2;
                        ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
                var target = LeagueSharp.Common.TargetSelector.GetTarget(-1, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                Orbwalker.Orbwalk(target, Game.CursorPos);
                Combo();
                return;
            }
            else
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != ComboMethodBackup[String.Format("CMETHOD{0}", enemy.ChampionName)].SelectedIndex)
                    {
                        typeVal.SelectedIndex = ComboMethodBackup[String.Format("CMETHOD{0}", enemy.ChampionName)].SelectedIndex;
                        ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
            }

            if (ConfigMenu.Item("MKEEPQ").GetValue<bool>() && Animation.QStacks != 0 && Utils.TickCount - Animation.LastQTick >= 3500)
                Spells[Q].Cast(Game.CursorPos);
        }

        public void BeforeDraw()
        {
            if (ConfigMenu.Item("DDRAWCOMBOMODE").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (!enemy.IsDead && enemy.IsVisible)
                    {
                        var text_pos = Drawing.WorldToScreen(enemy.Position);
                        Drawing.DrawText((int)text_pos.X - 20, (int)text_pos.Y + 35, System.Drawing.Color.Aqua, ConfigMenu.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>().SelectedValue);
                    }
                }
            }

            if(ConfigMenu.Item("DRAWULTSTATUS").GetValue<bool>())
            {
                var text_pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                Drawing.DrawText((int)text_pos.X - 20, (int)text_pos.Y + 35, System.Drawing.Color.Aqua, "Disable R Usage: " + (ConfigMenu.Item("CDISABLER").GetValue<KeyBind>().Active ? "On" : "Off"));
            }
        }

        public void Combo()
        {
            var t = Target.Get(600, true);
            if (t != null)
                ComboInstance.MethodsOnUpdate[ConfigMenu.Item(String.Format("CMETHOD{0}", t.ChampionName)).GetValue<StringList>().SelectedIndex](t);
        }

        public void LaneClear()
        {
            var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
            if (minion != null)
            {
                if (ConfigMenu.Item("LUSEQ").GetValue<bool>() && Spells[Q].IsReady())
                {
                    Animation.SetAttack(true);
                    if (!IsDoingFastQ && !SCommon.Orbwalking.Utility.InAARange(minion))
                        Spells[Q].Cast(minion.ServerPosition);
                    IsDoingFastQ = true;
                }

                if (ConfigMenu.Item("LUSEW").GetValue<bool>() && Spells[W].IsReady() && (ObjectManager.Get<Obj_AI_Minion>().Count(p => MinionManager.IsMinion(p) && p.IsValidTarget(Spells[W].Range)) >= ConfigMenu.Item("LMINW").GetValue<Slider>().Value || minion.IsJungleMinion()))
                {
                    if (ConfigMenu.Item("LUSETIAMAT").GetValue<bool>())
                        CastCrescent();
                    Spells[W].Cast();
                }
            }
        }

        public void LastHit()
        {
            if (ConfigMenu.Item("LASTUSETIAMAT").GetValue<bool>() && IsCrestcentReady)
            {
                var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (minion != null)
                {
                    float dist = minion.Distance(ObjectManager.Player.ServerPosition);
                    double dmg = (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * (1 - dist * 0.001);
                    if (minion.Health <= dmg)
                        CastCrescent();
                }
            }
        }

        public void Flee()
        {
            if (Spells[Q].IsReady() && Animation.QStacks != 2)
                Spells[Q].Cast(Game.CursorPos);

            if (ConfigMenu.Item("MFLEEWJ").GetValue<bool>())
            {
                if (Spells[Q].IsReady())
                {
                    var curSpot = WallJump.GetSpot(ObjectManager.Player.ServerPosition);
                    if (curSpot.Start != Vector3.Zero && Animation.QStacks == 2)
                    {
                        if (Spells[E].IsReady())
                            Spells[E].Cast(curSpot.End);
                        else
                            if (Items.GetWardSlot() != null)
                                Items.UseItem((int)Items.GetWardSlot().Id, curSpot.End);
                        Spells[Q].Cast(curSpot.End);
                        return;
                    }
                    var spot = WallJump.GetNearest(Game.CursorPos);
                    if (spot.Start != Vector3.Zero)
                    {
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, spot.Start);
                        return;
                    }
                    else
                        Spells[E].Cast(Game.CursorPos);
                }
            }
            else
            {
                if (Spells[Q].IsReady() && Animation.QStacks == 2)
                    Spells[Q].Cast(Game.CursorPos);

                if (Spells[E].IsReady())
                    Spells[E].Cast(Game.CursorPos);
            }

            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }

        public void FastQCombo(bool dontCheckQ = false)
        {
            if (Spells[Q].IsReady() || dontCheckQ)
            {
                var t = Target.Get(Spells[Q].Range);
                if (t != null)
                {
                    Target.Set(t);
                    Orbwalker.ForcedTarget = t;
                    Animation.SetAttack(true);
                    if (!IsDoingFastQ && !SCommon.Orbwalking.Utility.InAARange(t))
                    {
                        if (!Spells[W].IsReady(1000))
                            CastCrescent();
                        if (Spells[E].IsReady() && ObjectManager.Player.Distance(t.ServerPosition) > 125 && ConfigMenu.Item("CALWAYSE").GetValue<bool>())
                            Spells[E].Cast(t.ServerPosition);
                        Spells[Q].Cast(t.ServerPosition);
                    }
                    IsDoingFastQ = true;
                }
            }
        }

        public bool CheckR1(AIHeroClient t)
        {
            if (!ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !ConfigMenu.Item("CDISABLER").GetValue<KeyBind>().Active && Spells[R].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < 500 && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo)
            {
                if (ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q) * 2 + ObjectManager.Player.GetSpellDamage(t, SpellSlot.W) + CalculateAADamage(t, 2) >= t.Health && ObjectManager.Player.CountEnemiesInRange(1000) == 1)
                    return false;

                if (ObjectManager.Player.ServerPosition.CountEnemiesInRange(500) > 1)
                    return true;

                switch (ConfigMenu.Item("CR1MODE").GetValue<StringList>().SelectedIndex)
                {
                    case 1: if (!(t.Health - CalculateComboDamage(t) - CalculateDamageR2(t) <= 0)) return false;
                        break;
                    case 2: if (!(t.Health - CalculateComboDamage(t) < 1000 && t.Health >= 1000)) return false;
                        break;
                }
                return true;
            }
            return false;
        }

        public bool CheckR2(AIHeroClient t)
        {
            if (ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !ConfigMenu.Item("CDISABLER").GetValue<KeyBind>().Active && Spells[R].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < 900 && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo)
            {
                switch (ConfigMenu.Item("CR2MODE").GetValue<StringList>().SelectedIndex)
                {
                    case 1: if (t.Health - CalculateDamageR2(t) > 0 || t.Distance(ObjectManager.Player.ServerPosition) > 650f) return false;
                        break;
                    case 2: if (t.Distance(ObjectManager.Player.ServerPosition) < 600) return false;
                        break;
                    case 3: if ((t.HealthPercent > 25 && t.Health - CalculateDamageR2(t) - CalculateComboDamage(t) - CalculateAADamage(t, 2) > 0) || t.Distance(ObjectManager.Player.ServerPosition) > 650f) return false;
                        break;
                }
                return true;
            }
            return false;
        }

        public void CastCrescent()
        {
            if (ObjectManager.Player.CountEnemiesInRange(500) > 0 || Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.LaneClear)
            {
                if (Items.HasItem(3077) && Items.CanUseItem(3077)) //tiamat
                    Items.UseItem(3077);
                else if (Items.HasItem(3074) && Items.CanUseItem(3074)) //hydra
                    Items.UseItem(3074);
                else if (Items.HasItem(3748) && Items.CanUseItem(3748)) //titanic
                    Items.UseItem(3748);

                Animation.CanCastAnimation = true;
            }
        }

        public override double CalculateAADamage(AIHeroClient target, int aacount = 3)
        {
            double dmg = base.CalculateAADamage(target, aacount);                                                                                                                                                                                                                                                               /*          PBE            */
            dmg += ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, new[] { 0.2, 0.2, 0.25, 0.25, 0.25, 0.3, 0.3, 0.3, 0.35, 0.35, 0.35, 0.4, 0.4, 0.4, 0.45, 0.45, 0.45, 0.5 }[ObjectManager.Player.Level - 1] * (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * 5) /** (1 + EdgeCount * 0.001)*/;
            return dmg;
        }

        public override double CalculateDamageQ(AIHeroClient target)
        {
            if (!Spells[Q].IsReady())
                return 0.0d;

            return base.CalculateDamageQ(target) * (3 - Animation.QStacks);
        }

        public override double CalculateDamageR(AIHeroClient target)
        {
            if (!Spells[R].IsReady())
                return 0.0d;
            return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, ObjectManager.Player.FlatPhysicalDamageMod * 0.2 * 3);
        }

        public double CalculateDamageR2(AIHeroClient target)
        {
            if (Spells[R].IsReady())
                return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (new[] { 80, 120, 160 }[Spells[R].Level - 1] + ObjectManager.Player.FlatPhysicalDamageMod * 0.6) * (1 + ((100 - target.HealthPercent) > 75 ? 75 : (100 - target.HealthPercent)) * 0.0267d));
            return 0.0d;
        }

        protected override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.IsAutoAttack())
                    Animation.SetLastAATick(Utils.TickCount);
                else if (args.SData.Name == "RivenTriCleave" || args.SData.Name == "rivenizunablade")
                    Orbwalker.ResetAATimer();
            }
            else if (Target.Get(1000, true) != null)
            {
                if (args.SData.Name == "summonerflash")
                {
                    if (args.End.Distance(ObjectManager.Player.ServerPosition) > 300 && args.End.Distance(ObjectManager.Player.ServerPosition) < 500 && !Spells[E].IsReady())
                        Target.SetFlashed();
                }
            }
        }

        protected override void Interrupter_OnPossibleToInterrupt(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Spells[W].IsReady() && sender.IsEnemy && sender.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= Spells[W].Range && ConfigMenu.Item("MAUTOINTRW").GetValue<bool>())
                Spells[W].Cast();
        }

        protected override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsEnemy)
                return;

            if (Spells[W].IsReady() && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= Spells[W].Range && ConfigMenu.Item("MANTIGAPW").GetValue<bool>())
                LeagueSharp.Common.Utility.DelayAction.Add(100 + Game.Ping, () => Spells[W].Cast());              
            
            if (ConfigMenu.Item("MANTIGAPQ").GetValue<bool>() && Animation.QStacks == 2)
            {
                if (gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.MissileSpeed != 0)
                {
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(gapcloser.End.Distance(gapcloser.Start) / gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.MissileSpeed * 1000f) - Game.Ping, () =>
                    {
                        if (Items.GetWardSlot() != null)
                            Items.UseItem((int)Items.GetWardSlot().Id, ObjectManager.Player.ServerPosition + (gapcloser.End - gapcloser.Start).Normalized() * 40);
                        Spells[Q].Cast(ObjectManager.Player.ServerPosition);
                    });
                }
            }
        }

        private void TargetedSpell_Evade(DetectedTargetedSpellArgs data)
        {
            if (Spells[E].IsReady())
            {
                if (Orbwalker.ActiveMode != SCommon.Orbwalking.Orbwalker.Mode.Combo || !m_targetedEvader.DisableInComboMode)
                {
                    var pos = Vector2.Zero;
                    if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && Orbwalker.GetTarget().NetworkId == data.Caster.NetworkId)
                    {
                        if (data.Caster.ServerPosition.CountEnemiesInRange(1000) <= 1 || !data.SpellData.IsDangerous)
                            pos = Orbwalker.GetTarget().Position.To2D();
                        else if (data.SpellData.IsDangerous)
                            pos = SCommon.Maths.Geometry.Deviation(ObjectManager.Player.ServerPosition.To2D(), data.Caster.ServerPosition.To2D(), 90);
                        else
                            pos = ObjectManager.Player.ServerPosition.Extend(data.Caster.ServerPosition, -400).To2D();
                    }
                    else
                    {
                        if (data.SpellData.IsDangerous)
                            pos = ObjectManager.Player.ServerPosition.Extend(data.Caster.ServerPosition, -400).To2D();
                        else
                            pos = SCommon.Maths.Geometry.Deviation(ObjectManager.Player.ServerPosition.To2D(), data.Caster.ServerPosition.To2D(), 90);
                    }
                    if (pos.IsValid())
                        Spells[E].Cast(pos);
                }                
            }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLK)
            {
                var clickedTarget = HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();

                if (clickedTarget != null)
                {
                    var typeVal = ConfigMenu.Item(String.Format("CMETHOD{0}", clickedTarget.ChampionName)).GetValue<StringList>();
                    typeVal.SelectedIndex = (typeVal.SelectedIndex + 1) % 3;
                    ConfigMenu.Item(String.Format("CMETHOD{0}", clickedTarget.ChampionName)).SetValue(typeVal);
                }
            }
        }

        private void Animation_OnAnimationCastable(string animname)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo || Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Mixed || ConfigMenu.Item("CSHYKEY").GetValue<KeyBind>().Active || ConfigMenu.Item("CFLASHKEY").GetValue<KeyBind>().Active)
            {
                var t = Target.Get(1000);
                if (t != null)
                    ComboInstance.MethodsOnAnimation[ConfigMenu.Item(String.Format("CMETHOD{0}", t.ChampionName)).GetValue<StringList>().SelectedIndex](t, animname);
            }
        }
    }
}
