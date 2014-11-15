using System.Collections.Generic;
using System.Linq;
using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace RioSyndra
{
    class Program
    {
        private const string ChampName = "Syndra";
        private static Obj_AI_Hero Player = ObjectManager.Player;

        //Create spells
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static Spell EQ;

        //Summoner spells
        public static SpellSlot IgniteSlot;

        //Items
        public static Items.Item DFG;

        private static Menu Menu;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampName) return;
            
            //Spells data
            Q = new Spell(SpellSlot.Q, 800);
            Q.SetSkillshot(0.6f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 925);
            W.SetSkillshot(0.25f, 190f, 1450f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700);
            E.SetSkillshot(0.25f, (float)(45 * 0.5), 2500, false, SkillshotType.SkillshotCone);         

            R = new Spell(SpellSlot.R, 675);
            R.SetTargetted(0.5f, 1100f);

            EQ = new Spell(SpellSlot.E, 1292);
            EQ.SetSkillshot(0f, 60f, 1600f, false, SkillshotType.SkillshotLine);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            //Base menu
            Menu = new Menu("RioSyndra", "RioSyndra", true);

            //SimpleTs
            Menu.AddSubMenu(new Menu("SimpleTs", "SimpleTs"));
            SimpleTs.AddToMenu(Menu.SubMenu("SimpleTs"));

            //Orbwalker
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

            //Combo
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseEQ", "Use EQ").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Harass
            Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassAAQ", "Harass enemy AA by Q").SetValue(false));
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseWH", "Use W").SetValue(false));
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseEH", "Use E").SetValue(false));
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseEQH", "Use EQ").SetValue(false));
            Menu.SubMenu("Harass").AddItem(new MenuItem("CheckM", "Mana check").SetValue(new Slider(50, 0, 100)));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Misc
            Menu.AddSubMenu(new Menu("Misc", "Misc"));
            Menu.SubMenu("Misc").AddItem(new MenuItem("AntiGap", "Anti gaploser").SetValue(true));
            Menu.SubMenu("Misc").AddItem(new MenuItem("Interrupt", "Interrupt spells").SetValue(true));

            //EQ Setting
            Menu.AddSubMenu(new Menu("EQ Seting", "EQSeting"));
            Menu.SubMenu("EQSeting").AddItem(new MenuItem("EQDelay", "EQ delay").SetValue(new Slider(0, 0, 150)));
            Menu.SubMenu("EQSeting").AddItem(new MenuItem("UseEQC", "EQ closest to cursor").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            //R
            Menu.AddSubMenu(new Menu("R Seting", "RSeting"));
            Menu.SubMenu("RSeting").AddItem(new MenuItem("AntiOK", "Anti over skill").SetValue(true));
            Menu.SubMenu("RSeting").AddSubMenu(new Menu("Dont use R on", "DontR"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Menu.SubMenu("RSeting").SubMenu("DontR").AddItem(new MenuItem("DontR" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

            //Drawing
            Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawQ", "Q Range").SetValue(true));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawW", "W Range").SetValue(false));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawE", "E Range").SetValue(false));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawR", "R Range").SetValue(false));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawEQ", "EQ Range").SetValue(false));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawEQC", "EQ  indicator").SetValue(true));

            //Add main menu
            Menu.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("RioSyndra Loaded!");
        }


        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            //Update R range
            R.Range = R.Level == 3 ? 750f : 675f;

            //Update E width
            E.Width = E.Level == 5 ? 45f : (float)(45 * 0.5);

            //Use EQ closest to cursor 
            if (Menu.Item("UseEQC").GetValue<KeyBind>().Active && E.IsReady() && Q.IsReady())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team && Player.Distance(enemy, true) <= Math.Pow(EQ.Range, 2)))
                {
                   if (enemy.Distance(Game.CursorPos, true) <= 150 * 150)
                        UseEQ(enemy);
                }

            //Combo
            if (Menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            
            //Harass
            else if (Menu.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
        }

        private static void Combo()
        {
            UseSpells(Menu.Item("UseQ").GetValue<bool>(), //Q
                      Menu.Item("UseW").GetValue<bool>(), //W
                      Menu.Item("UseE").GetValue<bool>(), //E
                      Menu.Item("UseR").GetValue<bool>(), //R
                      Menu.Item("UseEQ").GetValue<bool>() //EQ
                      );
        }

        private static void Harass()
        {
            if (Player.Mana / Player.MaxMana * 100 < Menu.Item("CheckM").GetValue<Slider>().Value) return;
            UseSpells(Menu.Item("UseQH").GetValue<bool>(), //Q
                      Menu.Item("UseWH").GetValue<bool>(), //W
                      Menu.Item("UseEH").GetValue<bool>(), //E
                      false,                               //R
                      Menu.Item("UseEQH").GetValue<bool>() //EQ 
                      );
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {   
            //Last cast time of spells
            if (sender.IsMe)
            {
                if (args.SData.Name.ToString() == "SyndraQ")
                    Q.LastCastAttemptT = Environment.TickCount;
                if (args.SData.Name.ToString() == "SyndraW" || args.SData.Name.ToString() == "syndrawcast")
                    W.LastCastAttemptT = Environment.TickCount;
                if (args.SData.Name.ToString() == "SyndraE" || args.SData.Name.ToString() == "syndrae5")
                    E.LastCastAttemptT = Environment.TickCount;
            }
            
            //Harass when enemy do attack
            if (Menu.Item("HarassAAQ").GetValue<bool>() && sender.Type == Player.Type && sender.Team != Player.Team && args.SData.Name.ToLower().Contains("attack") && Player.Distance(sender, true) <= Math.Pow(Q.Range, 2) && Player.Mana / Player.MaxMana * 100 > Menu.Item("CheckM").GetValue<Slider>().Value)  
            {
                UseQ((Obj_AI_Hero)sender);
            }
        }
        
        //Anti gapcloser
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("AntiGap").GetValue<bool>()) return;

            if (E.IsReady() && Player.Distance(gapcloser.Sender, true) <= Math.Pow(E.Range, 2))
            {
                if (Q.IsReady())
                {
                    UseEQ((Obj_AI_Hero)gapcloser.Sender);
                }
                else
                    E.Cast(gapcloser.End);
            }
        }

        //Interrupt dangerous spells
        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Menu.Item("Interrupt").GetValue<bool>()) return;

            if (E.IsReady() && Player.Distance(unit, true) <= Math.Pow(E.Range, 2))
            {
                if (Q.IsReady())
                    UseEQ((Obj_AI_Hero)unit);
                else
                    E.Cast(unit);
            }
            else if (Q.IsReady() && E.IsReady() && Player.Distance(unit, true) <= Math.Pow(EQ.Range, 2))
                UseEQ((Obj_AI_Hero)unit);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Menu.Item("ComboActive").GetValue<KeyBind>().Active)
                args.Process = !Q.IsReady() && (!W.IsReady() || !E.IsReady());
        }

        private static float GetComboDamage(Obj_AI_Hero enemy, bool UQ, bool UW, bool UE, bool UR)
        {
            var damage = 0d;

            //Add damage Q
            if (Q.IsReady() && UQ) damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            //Add damage W
            if (W.IsReady() && UW) damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            //Add damage E
            if (E.IsReady() && UE) damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            //Add damage R
            if (R.IsReady() && UR) damage += GetRDamage(enemy);

            //Add damage DFG
            if (DFG.IsReady()) damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            return (float)((DFG.IsReady() || DFGBuff(enemy)) ? damage * 1.2 : damage);
        }

        private static float GetRDamage(Obj_AI_Hero enemy)
        {
            if (!R.IsReady()) return 0f;
            float damage = 45 + R.Level * 45 + Player.FlatMagicDamageMod * 0.2f; 
            return (float)Player.CalcDamage(enemy, Damage.DamageType.Magical, damage) * Player.Spellbook.GetSpell(SpellSlot.R).Ammo;
        }

        private static float GetIgniteDamage(Obj_AI_Hero enemy)
        {
            if (IgniteSlot == SpellSlot.Unknown || Player.SummonerSpellbook.CanUseSpell(IgniteSlot) != SpellState.Ready) return 0f;
            return (float)Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
        }

        private static bool DFGBuff(Obj_AI_Hero enemy)
        {
            return (enemy.HasBuff("deathfiregraspspell", true) || enemy.HasBuff("itemblackfiretorchspell", true)) ? true : false;
        }
       
        //Check R Over skill
        private static bool RCheck(Obj_AI_Hero enemy)
        {
            //Menu check
            if (!Menu.Item("AntiOK").GetValue<bool>()) return true;

            //Check Q is ready && Q is skillable
            else if (Q.IsReady() || Player.GetSpellDamage(enemy, SpellSlot.Q) >= enemy.Health) return false;

            //Check W is ready && W is skillable
            else if (W.IsReady() || Player.GetSpellDamage(enemy, SpellSlot.W) >= enemy.Health) return false;

            //Check AA is skillable
            else if (Player.GetAutoAttackDamage(enemy) >= enemy.Health) return false;
            else return true;  
        }

        private static void UseSpells(bool UQ, bool UW, bool UE, bool UR, bool UEQ)
        {   
            //Set Target
            Obj_AI_Hero QTarget = SimpleTs.GetTarget(Q.Range + 25f, SimpleTs.DamageType.Magical);
            Obj_AI_Hero WTarget = SimpleTs.GetTarget(W.Range + 25f, SimpleTs.DamageType.Magical);
            Obj_AI_Hero EQTarget = SimpleTs.GetTarget(EQ.Range, SimpleTs.DamageType.Magical);

            //Use Q
            if (UQ && QTarget != null)
            {
                UseQ(QTarget);
            }

            //Use E
            if (UE && E.IsReady())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                {
                    if (Player.Distance(enemy, true) <= Math.Pow(EQ.Range, 2) && Environment.TickCount - EQ.LastCastAttemptT > 600) 
                        UseE(enemy);
                }
            
            //Use EQ
            if (UEQ && EQTarget != null && Q.IsReady() && E.IsReady())
                UseEQ(EQTarget);

            //Use W1
            if (UW && EQTarget != null && W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1 )
            {
                Vector3 gObjectPos = OrbManager.GetOrbToGrab((int)(W.Range));

                if (gObjectPos.To2D().IsValid() && Environment.TickCount - Q.LastCastAttemptT > 600 + Game.Ping && Environment.TickCount - E.LastCastAttemptT > 600 + Game.Ping && Environment.TickCount - W.LastCastAttemptT > 600 + Game.Ping)
                {
                    W.Cast(gObjectPos);
                }
            }

            //Use W2
            if (UW && W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 1 && EQTarget != null && !(OrbManager.WObject(false).Name.ToLower() == "heimertblue"))
            {
                W.UpdateSourcePosition(OrbManager.WObject(false).ServerPosition);
                PredictionOutput Pos = W.GetPrediction(WTarget, true);
                if (Pos.Hitchance >= HitChance.High)
                    W.Cast(Pos.CastPosition);
            }

            //DFG, R, Ignite 
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                if (!enemy.HasBuff("UndyingRage") && !enemy.HasBuff("JudicatorIntervention"))
                {
                    if (GetComboDamage(enemy, UQ, UW, UE, UR) + GetIgniteDamage(enemy) > enemy.Health)
                    {   
                        //DFG
                        if (DFG.IsReady() && Player.Distance(enemy, true) <= Math.Pow(DFG.Range, 2))
                            DFG.Cast(enemy);
    
                        //R
                        bool UseR = Menu.Item("DontR" + enemy.BaseSkinName) != null && Menu.Item("DontR" + enemy.BaseSkinName).GetValue<bool>() == false && UR;
                        if (UseR && R.IsReady() && Player.Distance(enemy, true) <= Math.Pow(R.Range, 2) && (DFGBuff(enemy) ? GetRDamage(enemy) * 1.2 : GetRDamage(enemy)) > enemy.Health && RCheck(enemy))
                            R.CastOnUnit(enemy);
                    }
                    
                    //Ignite
                    if (Player.Distance(enemy, true) <= 600 * 600 && GetIgniteDamage(enemy) > enemy.Health)
                        Player.SummonerSpellbook.CastSpell(IgniteSlot, enemy);  
                }
        }

        private static void UseQ(Obj_AI_Hero Target)
        {
            if (!Q.IsReady()) return;
            PredictionOutput Pos = Q.GetPrediction(Target, true);
            if (Pos.Hitchance >= HitChance.High)
                Q.Cast(Pos.CastPosition);
        }


        private static void UseE(Obj_AI_Hero Target)
        {
            foreach (var orb in OrbManager.GetOrbs(true).Where(orb => orb.To2D().IsValid() && Player.Distance(orb, true) < Math.Pow(E.Range, 2)))
                {
                    Vector2 SP = orb.To2D() + Vector2.Normalize(Player.ServerPosition.To2D() - orb.To2D()) * 100f;
                    Vector2 EP = orb.To2D() + Vector2.Normalize(orb.To2D() - Player.ServerPosition.To2D()) * 592;
                    EQ.Delay = E.Delay + Player.Distance(orb) / E.Speed;
                    EQ.UpdateSourcePosition(orb);
                    var PPo = EQ.GetPrediction(Target).UnitPosition.To2D();
                    if (PPo.Distance(SP, EP, true, true) <= Math.Pow(EQ.Width + Target.BoundingRadius, 2))
                        E.Cast(orb);                
                }
        }

        private static void UseEQ(Obj_AI_Hero Target)
        {
            if (!Q.IsReady() || !E.IsReady()) return;
            Vector3 SPos = Prediction.GetPrediction(Target, 0.53f).UnitPosition;
            if (Player.Distance(SPos, true) > Math.Pow(700, 2))
            {
                EQ.Delay = 0.53f;
                EQ.From = Player.ServerPosition + Vector3.Normalize(Target.ServerPosition - Player.ServerPosition) * 700;
                var TPos = EQ.GetPrediction(Target);
                if (TPos.Hitchance >= HitChance.High)
                {
                    Vector3 Pos = Player.ServerPosition + Vector3.Normalize(TPos.UnitPosition - Player.ServerPosition) * 700;
                    UseEQ2(Target, Pos);
                }
            }
            else
            {
                Q.Width = 60f;
                var Pos = Q.GetPrediction(Target);
                if (Pos.Hitchance >= HitChance.High)
                {
                   
                    UseEQ2(Target, Pos.UnitPosition);
                }
            }
        }


        private static void UseEQ2(Obj_AI_Hero Target, Vector3 Pos)
        {
            if (Player.Distance(Pos, true) <= Math.Pow(E.Range, 2))
            {
                Vector3 SP = Pos + Vector3.Normalize(Player.ServerPosition - Pos) * 100f;
                Vector3 EP = Pos + Vector3.Normalize(Pos - Player.ServerPosition) * 592;
                EQ.Delay = E.Delay + Player.ServerPosition.Distance(Pos) / E.Speed;
                EQ.UpdateSourcePosition(Pos);
                var PPo = EQ.GetPrediction(Target).UnitPosition.To2D().ProjectOn(SP.To2D(), EP.To2D());
                if (PPo.IsOnSegment && PPo.SegmentPoint.Distance(Target, true) <= Math.Pow(EQ.Width + Target.BoundingRadius, 2))
                {
                    int Delay = 280 - (int)(Player.Distance(Pos) / 2.5) + Menu.Item("EQDelay").GetValue<Slider>().Value;
                    Utility.DelayAction.Add(Math.Max(0, Delay), () => E.Cast(Pos));
                    EQ.LastCastAttemptT = Environment.TickCount;
                    Q.Cast(Pos);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Menu.Item("DrawQ").GetValue<bool>())
                Utility.DrawCircle(Player.ServerPosition, Q.Range, Color.Green, 1);

            if (Menu.Item("DrawW").GetValue<bool>())
                Utility.DrawCircle(Player.ServerPosition, W.Range, Color.Green, 1);

            if (Menu.Item("DrawE").GetValue<bool>())
                Utility.DrawCircle(Player.ServerPosition, E.Range, Color.Green, 1);

            if (Menu.Item("DrawR").GetValue<bool>())
                Utility.DrawCircle(Player.ServerPosition, R.Range, Color.Green, 1);

            if (Menu.Item("DrawEQ").GetValue<bool>())
                Utility.DrawCircle(Player.ServerPosition, EQ.Range, Color.Green, 1);

            if (Menu.Item("DrawEQC").GetValue<bool>())
                if (Menu.Item("UseEQC").GetValue<KeyBind>().Active && E.IsReady() && Q.IsReady())
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                    Utility.DrawCircle(Game.CursorPos, 150f, (enemy.Distance(Game.CursorPos, true) <= 150 * 150) ? Color.Red : Color.Green, 3);
        }
    }
}
