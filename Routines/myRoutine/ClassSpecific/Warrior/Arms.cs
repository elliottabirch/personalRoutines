using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;

using Styx;

using Styx.CommonBot;
using Styx.TreeSharp;
using System;
using Styx.WoWInternals;
using Action = Styx.TreeSharp.Action;
using Styx.WoWInternals.WoWObjects;
using System.Drawing;
using CommonBehaviors.Actions;

namespace Singular.ClassSpecific.Warrior
{
    /// <summary>
    /// plaguerized from Apoc's simple Arms Warrior CC
    /// see http://www.thebuddyforum.com/honorbuddy-forum/combat-routines/warrior/79699-arms-armed-quick-dirty-simple-fast.html#post815973
    /// </summary>
    public class Arms
    {

        #region Common

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static WoWUnit Target { get { return StyxWoW.Me.CurrentTarget; } }
        private static WarriorSettings WarriorSettings { get { return SingularSettings.Instance.Warrior(); } }
        private static bool HasTalent(WarriorTalents tal) { return TalentManager.IsSelected((int)tal); }
        private static double CooldownMortalStrike { get { return Spell.GetSpellCooldown("Mortal Strike").TotalSeconds; } }
        private static double CooldownColossusSmash { get { return Spell.GetSpellCooldown("Colossus Smash").TotalSeconds; } }
        private static double DebuffColossusSmash { get { return Target.GetAuraTimeLeft("Colossus Smash").TotalSeconds; } }
        private static bool DebuffColossusSmashUp { get { return DebuffColossusSmash > 0; } }
        private static double DebuffRend { get { return Target.GetAuraTimeLeft("Rend").TotalSeconds; } }
        private static bool DebuffRendTicking { get { return DebuffRend > 0; } }
        private static CombatScenario scenario { get; set; }
        private static WoWUnit CSCycleTarget {
          get
          {
            return Unit.UnfriendlyUnits(8).First(u => !u.HasAura("Colossus Smash"));
          }
        }
        private static WoWUnit RendCycleTarget {
          get
          {
            return Unit.UnfriendlyUnits(8).First(u => !u.HasAura("Rend"));
          }
        }
        private static int NumTier20Pieces
        {
            get
            {
                int count = StyxWoW.Me.Inventory.Equipped.Hands.ItemInfo.Guid == 147189 ? 1 : 0;
                count += StyxWoW.Me.Inventory.Equipped.Legs.ItemInfo.Guid == 147191 ? 1 : 0;
                count += StyxWoW.Me.Inventory.Equipped.Chest.ItemInfo.Guid == 147187 ? 1 : 0;
                count += StyxWoW.Me.Inventory.Equipped.Shoulder.ItemInfo.Guid == 147192 ? 1 : 0;
                count += StyxWoW.Me.Inventory.Equipped.Head.ItemInfo.Guid == 147190 ? 1 : 0;
                count += StyxWoW.Me.Inventory.Equipped.Back.ItemInfo.Guid == 147188 ? 1 : 0;
                return count;
            }
        }

        [Behavior(BehaviorType.Initialize, WoWClass.Warrior, WoWSpec.WarriorArms)]
        public static Composite CreateArmsInitialize()
        {
            scenario = new CombatScenario(8, 1.5f);
            Logger.WriteDiagnostic("CreateArmsInitialize: Arms init complete");
            return null;
        }

        [Behavior(BehaviorType.Rest, WoWClass.Warrior, WoWSpec.WarriorArms)]
        public static Composite CreateArmsRest()
        {
            return new PrioritySelector(

                Common.CheckIfWeShouldCancelBladestorm(),

                Singular.Helpers.Rest.CreateDefaultRestBehaviour(),

                CheckThatWeaponIsEquipped()
                );
        }


        [Behavior(BehaviorType.Pull, WoWClass.Warrior, WoWSpec.WarriorArms)]
        public static Composite CreateArmsNormalPull()
        {
            return new PrioritySelector(
            //     Helpers.Common.EnsureReadyToAttackFromMelee(),

            //     Spell.WaitForCast()

                // new Decorator(
                //     ret => !Spell.IsGlobalCooldown(),
                //     new PrioritySelector(

                //         CreateDiagnosticOutputBehavior("Pull"),

                //         new Throttle(2, Spell.BuffSelf(Common.SelectedShoutAsSpellName)),

                //         Movement.WaitForFacing(),
                //         Movement.WaitForLineOfSpellSight(),

                //         Common.CreateAttackFlyingOrUnreachableMobs(),

                //         Spell.Cast("Storm Bolt", ret => WarriorSettings.ThrowPull == ThrowPull.StormBolt || WarriorSettings.ThrowPull == ThrowPull.Auto),
                //         Spell.Cast("Heroic Throw", ret => WarriorSettings.ThrowPull == ThrowPull.HeroicThrow || WarriorSettings.ThrowPull == ThrowPull.Auto),
                //         Common.CreateChargeBehavior(),

                //         Spell.Cast("Mortal Strike")
                //         )
                //     )
                new ActionAlwaysFail()
                );
        }

        #endregion

        #region Normal



        [Behavior(BehaviorType.Combat, WoWClass.Warrior, WoWSpec.WarriorArms)]
        public static Composite CreateArmsCombatNormal()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),

                Spell.WaitForCast(),

                Common.CheckIfWeShouldCancelBladestorm(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),

                    new PrioritySelector(

                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),

                        CreateDiagnosticOutputBehavior("Combat"),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Helpers.Common.CreateInterruptBehavior(),
						            Common.CreateSpellReflectBehavior(),

                        Common.CreateVictoryRushBehavior(),

                        // special "in combat" pull logic for mobs not tagged and out of melee range
                        Common.CreateWarriorCombatPullMore(),

                        Common.CreateExecuteOnSuddenDeath(),

                        CreateArmsAoeCombat(ret => Unit.NearbyUnfriendlyUnits.Count(u => u.SpellDistance() < (Common.DistanceWindAndThunder(8)))),

                        // Noxxic
                        //----------------
                        new Decorator(
                          ret => Me.GotTarget(), // execute
                              new PrioritySelector (
                                new Decorator(
                                  ret => Target.HealthPercent < 20 && Unit.UnfriendlyUnits(8).Count() < 5,
                                  new PrioritySelector(
                                    Spell.Cast("BladeStorm", ret => Spell.UseAOE && Me.HasAura("Battle Cry") && (NumTier20Pieces >= 4 || StyxWoW.Me.Inventory.Equipped.Head.ItemInfo.Guid == 151823)),
                                    Spell.Cast("Colossus Smash", ret => !Me.HasAura("Shattered Defenses") && !Me.HasAura("Battle Cry")),
                                    Spell.Cast("Warbreaker", ret => Spell.UseAOE && Spell.GetSpellCooldown("Mortal Strike").TotalSeconds < 1 && !Me.HasAura("Shattered Defenses") && Target.GetAuraByName("Executioner's Precision").stackCount == 2),
                                    Spell.Cast("Rend", ret => Spell.UseAOE && DebuffRend < 5 && Spell.GetSpellCooldown("Battle Cry").TotalSeconds < 2 && Spell.GetSpellCooldown("Bladestorm").TotalSeconds < 2),
                                    Spell.Cast("Ravager", ret => Spell.UseAOE && Spell.GetSpellCooldown("Battle Cry").TotalSeconds < 1 && DebuffColossusSmash >= 6),
                                    Spell.Cast("Mortal Strike", ret => Target.GetAuraByName("Executioner's Precision").stackCount == 2 && Me.HasAura("Shattered Defenses")),
                                    Spell.Cast("Execute", ret => !Me.HasAura("Shattered Defenses") || Me.CurrentRage >= 40 || Common.HasTalent(WarriorTalents.Dauntless) && Me.CurrentRage >= 36 ),
                                    Spell.Cast("BladeStorm", ret => Spell.UseAOE && NumTier20Pieces <= 4),
                                    new ActionAlwaysFail()
                                  )
                                ),
                              new PrioritySelector( //normal
                                  Spell.Cast("Bladestorm", ret => Spell.UseAOE && Me.HasAura("Battle Cry") && (NumTier20Pieces >= 4 || StyxWoW.Me.Inventory.Equipped.Head.ItemInfo.Guid == 151823)),
                                  Spell.Cast("Colossus Smash", ret => !Me.HasAura("Shattered Defenses")),
                                  Spell.Cast("Warbreaker", ret => Spell.UseAOE && (Common.HasTalent(WarriorTalents.FervorOfBattle) && DebuffColossusSmash < 1) || (!Common.HasTalent(WarriorTalents.FervorOfBattle) && (Me.HasAura("Stone Heart") || CooldownMortalStrike < 1) && !Me.HasAura("Shattered Defenses"))),
                                  Spell.Cast("Rend", ret => DebuffRend <= 1 || Spell.UseAOE && DebuffRend < 5 && Spell.GetSpellCooldown("Battle Cry").TotalSeconds < 2 && Spell.GetSpellCooldown("Bladestorm").TotalSeconds < 2),
                                  Spell.Cast("Ravager", ret => Spell.UseAOE && Spell.GetSpellCooldown("Battle Cry").TotalSeconds < 1 && (DebuffColossusSmash >= 6 )),
                                  Spell.Cast("Execute", ret => Me.HasAura("Stone Heart")),
                                  Spell.Cast("Mortal Strike", ret => Target.GetAuraByName("Executioner's Precision").stackCount == 0 || Me.HasAura("Shattered Defenses")),
                                  Spell.Cast("Rend", ret => Target.GetAuraByName("Rend").TimeLeft() < Target.GetAuraByName("Rend").Duration * .3),
                                  Spell.Cast("Whirlwind", ret => Unit.UnfriendlyUnits(8).Count() > 1 || Common.HasTalent(WarriorTalents.FervorOfBattle)),
                                  Spell.Cast("Slam", ret => !Common.HasTalent(WarriorTalents.FervorOfBattle) && (Me.CurrentRage >=52 || !Common.HasTalent(WarriorTalents.Rend) || !Common.HasTalent(WarriorTalents.Ravager))),
                                  Spell.Cast("BladeStorm", ret => Spell.UseAOE && NumTier20Pieces <= 4)
                                  )
                              )
                          ),
                        Common.CreateChargeBehavior(),
                        Common.CreateAttackFlyingOrUnreachableMobs()
                        )
                    )
              );
        }

        private static Composite CreateArmsAoeCombat(SimpleIntDelegate aoeCount)
        {
            return new PrioritySelector( //cleave
              new Decorator(
                ret => Spell.UseAOE && aoeCount(ret) >= 2 && Common.HasTalent(WarriorTalents.SweepingStrikes),
                new PrioritySelector(
                    Spell.Cast("Mortal Strike"),
                    Spell.Cast("Execute", ret => Me.HasAura("Stone Heart")),
                    Spell.Cast("Colossus Smash", ret => !Me.HasAura("Shattered Defenses") && !Target.HasAura("Precise Strikes")),
                    Spell.Cast("Warbreaker", ret => !Me.HasAura("Shattered Defenses")),
                    Spell.Cast("Whirlwind", ret => Common.HasTalent(WarriorTalents.FervorOfBattle) && (DebuffColossusSmashUp || (Me.CurrentRage >= Me.MaxRage - 50)) && Me.HasAura("Cleave")),
                    Spell.Cast("Rend", ret => Target.GetAuraByName("Rend").TimeLeft() < Target.GetAuraByName("Rend").Duration * .3 ),
                    Spell.Cast("Bladestorm"),
                    Spell.Cast("Cleave"),
                    Spell.Cast("Whirlwind", ret => Me.CurrentRage >= 40 || Me.HasAura("Cleave")),
                    new ActionAlwaysFail()
                  )),
              new Decorator( //aoe
                ret => Spell.UseAOE && aoeCount(ret) >= 5 && Common.HasTalent(WarriorTalents.SweepingStrikes),
                new PrioritySelector(
                    Spell.Cast("Warbreaker", ret => Spell.GetSpellCooldown("Bladestorm").TotalSeconds < 1 && Spell.GetSpellCooldown("Battle Cry").TotalSeconds < 1),
                    Spell.Cast("Bladestorm", ret => Me.HasAura("Battle Cry") && (NumTier20Pieces >= 4 || StyxWoW.Me.Inventory.Equipped.Head.ItemInfo.Guid == 151823)),
                    Spell.Cast("Colossus Smash", ret => !Me.HasAura("In For The Kill") && Common.HasTalent(WarriorTalents.InForTheKill)),
                    Spell.Cast("Colossus Smash", ret => !DebuffColossusSmashUp && Unit.UnfriendlyUnits(8).Count() <= 10),
                    Spell.Cast("Cleave"),
                    Spell.Cast("Whirlwind", ret => Me.HasAura("Cleave")),
                    Spell.Cast("Whirlwind", ret => Unit.UnfriendlyUnits(8).Count() >= 7),
                    Spell.Cast("Colossus Smash", ret => !Me.HasAura("Shattered Defenses")),
                    Spell.Cast("Execute", ret => Me.HasAura("Stone Heart")),
                    Spell.Cast("Mortal Strike", ret => Me.HasAura("Shattered Defenses") || Target.GetAuraByName("Executioner's Precision").stackCount == 0),
                    Spell.Cast("Rend", ret => Target.GetAuraByName("Rend").TimeLeft() < Target.GetAuraByName("Rend").Duration * .3 && Unit.UnfriendlyUnits(8).Count() <= 3, RendCycleTarget),
                    Spell.Cast("Whirlwind")
                )
              )
            );
        }

        private static Composite CreateDiagnosticOutputBehavior(string context = null)
        {
            if (!SingularSettings.Debug)
                return new ActionAlwaysFail();

            if (context == null)
                context = Dynamics.CompositeBuilder.CurrentBehaviorType.ToString();

            context = "<<" + context + ">>";

            return new ThrottlePasses(
                1, TimeSpan.FromSeconds(1.5), RunStatus.Failure,
                new Action(ret =>
                {
                    string log;
                    log = string.Format(context + " h={0:F1}%/r={1:F1}%, stance={2}, Enrage={3} Coloss={4} MortStrk={5}",
                        Me.HealthPercent,
                        Me.Shapeshift,
                        Me.ActiveAuras.ContainsKey("Enrage"),
                        (int)Spell.GetSpellCooldown("Colossus Smash", -1).TotalMilliseconds,
                        (int)Spell.GetSpellCooldown("Mortal Strike", -1).TotalMilliseconds
                        );

                    WoWUnit target = Me.CurrentTarget;
                    if (target != null)
                    {
                        log += string.Format(", th={0:F1}%, dist={1:F1}, inmelee={2}, face={3}, loss={4}, dead={5} secs, flying={6}",
                            target.HealthPercent,
                            target.Distance,
                            target.IsWithinMeleeRange.ToYN(),
                            Me.IsSafelyFacing(target).ToYN(),
                            target.InLineOfSpellSight.ToYN(),
                            target.TimeToDeath(),
                            target.IsFlying.ToYN()
                            );
                    }

                    int mobc;
                    bool avoidaoe;
                    int mobcc;

                    if (scenario == null)
                    {
                        mobc = 0;
                        avoidaoe = false;
                        mobcc = 0;
                    }
                    else
                    {
                        mobc = scenario.MobCount;
                        avoidaoe = scenario.AvoidAOE;
                        mobcc = scenario.Mobs == null ? 0 : scenario.Mobs.Count();
                    }

                    log += string.Format(
                        "cdcs={0:F2}, cdms={1:F2}, mobs={2}, avoidaoe={3}, enemies={4}",
                        CooldownColossusSmash,
                        CooldownMortalStrike,
                        mobc,
                        avoidaoe.ToYN(),
                        mobcc
                        );

                    Logger.WriteDebug(Color.AntiqueWhite, log);
                    return RunStatus.Failure;
                })
                );
        }

        #endregion

        private static Composite _checkWeapons = null;
        public static Composite CheckThatWeaponIsEquipped()
        {
            if (_checkWeapons == null)
            {
                _checkWeapons = new ThrottlePasses(60,
                    new Sequence(
                        new DecoratorContinue(
                            ret => !Me.Disarmed && !IsWeapon2H(Me.Inventory.Equipped.MainHand),
                            new Action(ret => Logger.Write(Color.HotPink, "User Error: a {0} requires a Two Handed Weapon equipped to be effective", SingularRoutine.SpecAndClassName()))
                            ),
                        new ActionAlwaysFail()
                        )
                    );
            }
            return _checkWeapons;
        }
        public static bool IsWeapon2H(WoWItem hand)
        {
            return hand != null
                && hand.ItemInfo.ItemClass == WoWItemClass.Weapon
                && hand.ItemInfo.InventoryType == InventoryType.TwoHandWeapon;
        }
    }
}