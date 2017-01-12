using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;

using EloBuddy; namespace KappaSeries
{
    class Aatrox
    {
        public Aatrox()
        {
            Load();
        }

        private static Orbwalking.Orbwalker _orbwalker;
        public static readonly List<Spell> SpellList = new List<Spell>();
        //public static int[]  Leveluplane = new int[]   { 2, 1, 0, 1, 2, 3, 1, 2, 1, 2, 3, 1, 2, 0, 0, 3, 0, 0 };
        //public static int[] LevelupJungle = new int[] { 1, 2, 0, 1, 1, 3, 1, 2, 1, 2, 3, 2, 2, 0, 0, 3, 0, 0 };
        private static Spell _q;
        private static Spell _w;
        private static Spell _e;
        private static Spell _r;
        public static SpellSlot IgniteSlot;
        public static SpellSlot SmiteSlot;
        private static Menu _cfg;
        private static AIHeroClient _player;

        static void Load()
        {
            _player = ObjectManager.Player;

            _q = new Spell(SpellSlot.Q, 676f);
            _w = new Spell(SpellSlot.W, Orbwalking.GetRealAutoAttackRange(_player));
            _e = new Spell(SpellSlot.E, 980f);
            _r = new Spell(SpellSlot.R, 550f);

            _q.SetSkillshot(_q.Instance.SData.SpellCastTime, 280f, _q.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);
            _e.SetSkillshot(_e.Instance.SData.SpellCastTime,_e.Instance.SData.LineWidth,_e.Instance.SData.MissileSpeed,false,SkillshotType.SkillshotLine);
           
            SpellList.Add(_q);
            SpellList.Add(_w);
            SpellList.Add(_e);
            SpellList.Add(_r);

            IgniteSlot = _player.GetSpellSlot("SummonerDot");
            SmiteSlot = _player.GetSpellSlot("summonersmite");

            _cfg = new Menu("厄薩斯","Aatrox",true);

            var targetSelectorMenu = new Menu("目標選擇器", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _cfg.AddSubMenu(targetSelectorMenu);

            _cfg.AddSubMenu(new Menu("走砍", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_cfg.SubMenu("Orbwalking"));

            _cfg.AddSubMenu(new Menu("連招", "Combo"));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo", "連招!").SetValue(new KeyBind(32, KeyBindType.Press)));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "使用 Q")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "使用 W")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "使用 E")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "使用 R")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("minW", "最小血量 % W")).SetValue(new Slider(50,0,100));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("maxW", "最大血量 % W")).SetValue(new Slider(80, 0, 100));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("minR", "當敵人數量x使用 R")).SetValue(new Slider(2, 0, 5));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("DontQ", "防止使用Q敵人時進入塔下")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("Dive", "當敵人血量低於%時，進入塔殺")).SetValue(true);
            _cfg.SubMenu("Combo").AddItem(new MenuItem("DiveMHP", "自身血量 % 塔殺")).SetValue(new Slider(60, 0, 100));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("DiveTHP", "目標血量 % 塔殺")).SetValue(new Slider(10, 0, 100));
            _cfg.SubMenu("Combo").AddItem(new MenuItem("UseItems", "使用物品")).SetValue(true);
            
            _cfg.AddSubMenu(new Menu("騷擾", "Harass"));
            _cfg.SubMenu("Harass").AddItem(new MenuItem("ActiveHarass", "騷擾!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            _cfg.SubMenu("Harass").AddItem(new MenuItem("HarQ", " 使用 Q").SetValue(false));
            _cfg.SubMenu("Harass").AddItem(new MenuItem("HarE", "使用 E").SetValue(true));
            

            _cfg.AddSubMenu(new Menu("清線", "LaneClear"));
            _cfg.SubMenu("LaneClear").AddItem(new MenuItem("ActiveLane", "清線!").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
            _cfg.SubMenu("LaneClear").AddItem(new MenuItem("UseQLane", "使用 Q")).SetValue(false);
            _cfg.SubMenu("LaneClear").AddItem(new MenuItem("UseWLane", "使用 W")).SetValue(true);
            _cfg.SubMenu("LaneClear").AddItem(new MenuItem("UseELane", "使用 E")).SetValue(true);

            _cfg.AddSubMenu(new Menu("清野", "JungleClear"));
            _cfg.SubMenu("JungleClear").AddItem(new MenuItem("ActiveJungle", "清野!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            _cfg.SubMenu("JungleClear").AddItem(new MenuItem("UseQJungle", "使用 Q")).SetValue(true);
            _cfg.SubMenu("JungleClear").AddItem(new MenuItem("UseWJungle", "使用 W")).SetValue(true);
            _cfg.SubMenu("JungleClear").AddItem(new MenuItem("UseEJungle", "使用 E")).SetValue(true);

            _cfg.AddSubMenu(new Menu("搶頭", "KillSteal"));
            _cfg.SubMenu("KillSteal").AddItem(new MenuItem("SmartKS", "智能搶頭")).SetValue(true);
            _cfg.SubMenu("KillSteal").AddItem(new MenuItem("RKS", "使用R搶頭")).SetValue(false);


            _cfg.AddSubMenu(new Menu("顯示", "Drawings"));
            _cfg.SubMenu("Drawings").AddItem(new MenuItem("Qdraw", "顯示 Q 範圍")).SetValue(true);
            _cfg.SubMenu("Drawings").AddItem(new MenuItem("Edraw", "顯示 E 範圍")).SetValue(true);
            _cfg.SubMenu("Drawings").AddItem(new MenuItem("LagFree", "Lag Free Cirlces")).SetValue(true);
            _cfg.SubMenu("Drawings").AddItem(new MenuItem("CircleThickness", "圓圈密度").SetValue(new Slider(1, 10, 1)));
            

            _cfg.AddSubMenu(new Menu("雜項", "Misc"));
            //_cfg.SubMenu("Misc").AddItem(new MenuItem("AutoLevel", "Auto Level")).SetValue(false);
            //_cfg.SubMenu("Misc").AddItem(new MenuItem("LevelSeq", "Leveling Style").SetValue(new StringList(new[] { "Lane", "Jungle" })));
            _cfg.SubMenu("Misc").AddItem(new MenuItem("TowerQ", "自動 Q 塔下")).SetValue(false);
            _cfg.SubMenu("Misc").AddItem(new MenuItem("IntQ", "打斷技能使用 Q")).SetValue(false);
            _cfg.SubMenu("Misc").AddItem(new MenuItem("IntMed", "中斷危險技能時使用")).SetValue(false);
            _cfg.SubMenu("Misc").AddItem(new MenuItem("SmartW", "智能 W 邏輯")).SetValue(true);

            _cfg.AddSubMenu(new Menu("逃跑", "Flee"));
            _cfg.SubMenu("Flee").AddItem(new MenuItem("ActiveFlee", "逃跑!").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));

            _cfg.AddToMainMenu();

           Game.OnUpdate += OnUpdate;
           Drawing.OnDraw += OnDraw;
           Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
           Orbwalking.AfterAttack += OrbwalkingAfterAttack;

        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
            {
                return;
            }
            if (_cfg.Item("ActiveCombo").IsActive() || _cfg.Item("ActiveLane").IsActive() ||
                _cfg.Item("ActiveJungle").IsActive() && _cfg.Item("UseItems").IsActive())
            {
                var hydId = ItemData.Ravenous_Hydra_Melee_Only.Id;
                var tiaId = ItemData.Tiamat_Melee_Only.Id;

                if (Items.HasItem(hydId))
                {
                    if (Items.CanUseItem(hydId))
                    {
                        Items.UseItem(hydId);
                    }
                }

                if (Items.HasItem(tiaId))
                {
                    if (Items.CanUseItem(tiaId))
                    {
                        Items.UseItem(tiaId);
                    }
                }
            }
        }

        private static void OnPossibleToInterrupt(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!_cfg.Item("IntQ").IsActive() || !_q.IsReady() || !sender.IsValidTarget(_q.Range))
            {
                return;
            }
            if (args.DangerLevel == Interrupter2.DangerLevel.High || args.DangerLevel == Interrupter2.DangerLevel.Medium && _cfg.Item("IntMed").IsActive())
            {
                _q.Cast(sender);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (_cfg.Item("LagFree").IsActive())
            {
                if (_cfg.Item("Qdraw").IsActive())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Cyan,
                        _cfg.Item("CircleThickness").GetValue<Slider>().Value);
                }
                
                if (_cfg.Item("Edraw").IsActive())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.Crimson,
                        _cfg.Item("CircleThickness").GetValue<Slider>().Value);
                }
                
            }
            else
            {
                if (_cfg.Item("Qdraw").IsActive())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Cyan);
                }
                
                if (_cfg.Item("Edraw").IsActive())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.Crimson);
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (_player.IsDead)
            {
                return;
            }
            if (_cfg.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (_cfg.Item("ActiveHarass").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (_cfg.Item("ActiveJungle").GetValue<KeyBind>().Active)
            {
                Jungleclear();
            }
            if (_cfg.Item("ActiveLane").GetValue<KeyBind>().Active)
            {
               Laneclear();
            }
            if (_cfg.Item("SmartKS").IsActive())
            {
                Smartks();
            }
            if (_cfg.Item("ActiveFlee").GetValue<KeyBind>().Active)
            {
                Flee();
            }
            if (_cfg.Item("TowerQ").IsActive())
            {
                Towerq();
            }
        }

        
        private static void Towerq()
        {
            var allyturret = ObjectManager.Get<Obj_AI_Turret>().First(obj => obj.IsAlly && obj.Distance(_player) <= 775f);
            var minUnderTur = MinionManager.GetMinions(allyturret.ServerPosition, 775, MinionTypes.All, MinionTeam.Enemy);

            foreach (var target in ObjectManager.Get<AIHeroClient>().Where(target => target.IsValidTarget(_e.Range)))
            {
                if (allyturret != null && minUnderTur == null && target.IsValidTarget())
                {
                    _q.Cast(target);
                }
            }
        }

        private static void Flee()
        {
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
           if (_q.IsReady())
            {
                _q.Cast(Game.CursorPos);
            }
           var t = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
           if (_e.IsReady() && t.IsValidTarget(_e.Range))
           {
                _e.Cast(t);
           }
        }

        private static void Smartks()
        {
            foreach (var t in ObjectManager.Get<AIHeroClient>().Where(t => t.IsEnemy).Where(t => t.IsValidTarget(_q.Range))) {
                #region e
                if (t.Health < _e.GetDamage(t) && _e.IsReady())
                {
                    _e.Cast(t);
                }
                    #endregion
                    #region q
                else if (t.Health < _q.GetDamage(t) && _q.IsReady())
                {
                    _q.Cast(t.ServerPosition);

                }
                    #endregion
                    #region eq
                else if (t.Health < (_q.GetDamage(t) + _e.GetDamage(t)) && _e.IsReady() && _q.IsReady())
                {
                    if(_e.Cast(t)== Spell.CastStates.SuccessfullyCasted)
                    {
                        _q.Cast(t.ServerPosition, false);
                    }
                }
                    #endregion
                    #region eq ignite
                else  if (t.Health < (_q.GetDamage(t) + _e.GetDamage(t) + _player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite)) && _e.IsReady() && _q.IsReady() && IgniteSlot.IsReady() && IgniteSlot != SpellSlot.Unknown) 
                {
                    _e.Cast(t);
                    if (!_e.IsCharging)
                    {
                        _q.Cast(t, false, true);
                    }
                    _player.Spellbook.CastSpell(IgniteSlot, t);
                }
                    #endregion
                    #region eq smite
                else if (t.Health < (_q.GetDamage(t) + _e.GetDamage(t) + Smitedamage()) && _e.IsReady() && _q.IsReady() && SmiteSlot.IsReady() && SmiteSlot != SpellSlot.Unknown)
                {
                    _e.Cast(t);
                    if (!_e.IsCharging)
                    {
                        _q.Cast(t, false, true);
                    }
                    _player.Spellbook.CastSpell(SmiteSlot, t);
                }
                    #endregion
                    #region eq smite R
                else if (_cfg.Item("RKS").IsActive() && t.Health < (_q.GetDamage(t) + _e.GetDamage(t) + Smitedamage() + _r.GetDamage(t)) && _e.IsReady() && _q.IsReady() && SmiteSlot.IsReady() && _r.IsReady() && SmiteSlot != SpellSlot.Unknown)
                {
                    _e.Cast(t);
                    if (!_e.IsCharging)
                    {
                        _q.Cast(t, false, true);
                    }
                    if (_player.Distance(t) < 500)
                    {
                        _player.Spellbook.CastSpell(SmiteSlot, t);
                    }

                    if (_player.Distance(t) < _r.Range && !_e.IsCharging && !_q.IsCharging)
                    {
                        _r.Cast();
                    }
                }
                    #endregion
                    #region eq ignite R
                else if (_cfg.Item("RKS").IsActive() && t.Health < (_q.GetDamage(t) + _e.GetDamage(t) + _player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) + _r.GetDamage(t)) && _e.IsReady() && _q.IsReady() && IgniteSlot.IsReady() && _r.IsReady() && IgniteSlot != SpellSlot.Unknown)
                {
                    _e.Cast(t);
                    if (!_e.IsCharging)
                    {
                        _q.Cast(t, false, true);
                    }
                    if (_player.Distance(t) < 600)
                    {
                        _player.Spellbook.CastSpell(IgniteSlot, t);
                    }

                    if (_player.Distance(t) < _r.Range && !_e.IsCharging && !_q.IsCharging)
                    {
                        _r.Cast();
                    }

                }
                    #endregion
                else return;
            }
        }

        private static void Laneclear()
        {
            var minion = MinionManager.GetMinions(_player.ServerPosition, _q.Range);

            if (minion.Count < 3)
                return;

            if (_cfg.Item("UseQLane").IsActive() && _q.IsReady())
            {
                _q.Cast(minion[0].ServerPosition);
            }
            if (_cfg.Item("UseWLane").IsActive() && _w.IsReady() && _player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2)
            {
                _w.Cast();
            }
            if (_cfg.Item("UseELane").IsActive() && _e.IsReady())
            {
                _e.Cast(minion[0].ServerPosition);
            }
        }

        private static void Jungleclear()
        {
            var junglemonster = MinionManager.GetMinions(_player.ServerPosition, _q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (junglemonster.Count == 0) return;

            if (_cfg.Item("UseEJungle").IsActive() && _e.IsReady())
            {
                _e.Cast(junglemonster[0].ServerPosition);
            }
            if (_cfg.Item("UseQJungle").IsActive() && _q.IsReady())
            {
                _q.Cast(junglemonster[0].ServerPosition);
            }
            if (_cfg.Item("UseWJungle").IsActive() && _player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2 && _w.IsReady())
            {
                _w.Cast();
            }
            
        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            if (t == null) return;

            if (t.IsValidTarget() && _e.IsReady() && _cfg.Item("HarE").IsActive())
            {
                _e.Cast(t);
            }
            if (t.IsValidTarget() && _e.IsReady() && _cfg.Item("HarQ").IsActive() && !t.UnderTurret())
            {
                _q.Cast(t);
            }

        }

        private static float Smitedamage()
        {
            int lvl = _player.Level;
            int smitedamage = (20 + 8 * lvl);

            return smitedamage;
        }

        private static float GetHealthPercent(AIHeroClient player)
        {
            return player.Health * 100 / player.MaxHealth;
        }

        private static void Combo()
        {
            var smitedmg = Smitedamage();
            var t = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Physical);
            var youm = ItemData.Youmuus_Ghostblade;
            var bil = ItemData.Bilgewater_Cutlass;
            var botrk = ItemData.Blade_of_the_Ruined_King;

            #region Items
            if (_cfg.Item("UseItems").IsActive())
            {
                if (Items.HasItem(youm.Id))
                {
                    if (Items.CanUseItem(youm.Id) && _player.Distance(t) <= 200f)
                    {
                        Items.UseItem(youm.Id);
                    }
                }
                if (Items.HasItem(bil.Id))
                {
                    if (Items.CanUseItem(bil.Id) && _player.Distance(t) <= bil.Range)
                    {
                        Items.UseItem(bil.Id, t);
                    }
                }
                if (Items.HasItem(botrk.Id))
                {
                    if (Items.CanUseItem(botrk.Id) && _player.Distance(t) <= botrk.Range)
                    {
                        Items.UseItem(botrk.Id, t);
                    }
                }
            }
            #endregion

            if (t == null) return;
            
            #region E
            if (_e.IsReady() && _cfg.Item("UseECombo").IsActive() && _player.Distance(t) <= _e.Range)
            {
                _e.Cast(t);
            }
            #endregion
            #region Q

            if (_q.IsReady() && _cfg.Item("UseQCombo").IsActive() && _player.Distance(t) <= _q.Range)
            {
                if (_cfg.Item("DontQ").IsActive() && t.UnderTurret())
                {
                    if (_cfg.Item("Dive").IsActive())
                    {
                        if (GetHealthPercent(t) <= _cfg.Item("DiveTHP").GetValue<Slider>().Value)
                        {
                            if (GetHealthPercent(_player) >= _cfg.Item("DiveMHP").GetValue<Slider>().Value)
                            {
                                if(_q.Cast(t.ServerPosition))
                                return;
                            }
                        }
                    }
                }
                else
                {
                    _q.Cast(t.ServerPosition);
                    
                }
                    
            }
            

            #endregion
            #region W
            if (_w.IsReady() && _cfg.Item("UseWCombo").IsActive())
            {
                #region Smart W
                if (_cfg.Item("SmartW").IsActive())
                {
                    if(_player.Health < (_player.MaxHealth * 0.95))
                    {
                        if (_player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2)
                        {
                            _w.Cast();
                        }
                    }
                    else if (_player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                    {
                        _w.Cast();
                    }
                }
                #endregion
                #region not smart W
                else if (!_cfg.Item("SmartW").IsActive())
                {
                    if (_w.IsReady() && GetHealthPercent(_player) < (_cfg.Item("minW").GetValue<Slider>().Value))
                    {
                        if (_player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2)
                        {
                            _w.Cast();
                        }
                    }
                    if (_w.IsReady() && GetHealthPercent(_player) > (_cfg.Item("maxW").GetValue<Slider>().Value))
                    {
                        if (_player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                        {
                            _w.Cast();
                        }
                    }
                }

            }
            #endregion
            #endregion
            #region R
            if (_r.IsReady() && _cfg.Item("UseRCombo").IsActive() && _player.CountEnemiesInRange(_r.Range) >= _cfg.Item("minR").GetValue<Slider>().Value && _player.Distance(t) <= _r.Range)
            {
                _r.Cast();
            }
            #endregion
        }
    } 
}
