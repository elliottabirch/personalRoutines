﻿using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;

using Styx;

using Styx.CommonBot;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Singular.Settings;

using Action = Styx.TreeSharp.Action;
using CommonBehaviors.Actions;
using System.Drawing;


namespace Singular.ClassSpecific.Warlock
{
    public static class Affliction
    {
        private static LocalPlayer Me => StyxWoW.Me;
        private static WarlockSettings WarlockSettings => SingularSettings.Instance.Warlock();

        private static int _mobCount;

        private static int DotCountNeeded;
        private static int MaxDotCount;

        [Behavior(BehaviorType.Initialize, WoWClass.Warlock, WoWSpec.WarlockAffliction)]
        public static Composite CreateWarlockAfflictionInit()
        {
            MaxDotCount = 0;
            if (SpellManager.HasSpell("Agony"))
                ++MaxDotCount;
            if (SpellManager.HasSpell("Corruption"))
                ++MaxDotCount;
            if (SpellManager.HasSpell("Unstable Affliction"))
                ++MaxDotCount;

            return null;
        }


        [Behavior(BehaviorType.Pull, WoWClass.Warlock, WoWSpec.WarlockAffliction)]
        public static Composite CreateWarlockAfflictionNormalPull()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                Spell.WaitForCast(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        new Action(ret =>
                        {
                            _mobCount = Common.TargetsInCombat.Count();
                            return RunStatus.Failure;
                        }),

                        CreateWarlockDiagnosticOutputBehavior("Pull"),
                        SingularRoutine.CurrentWoWContext == WoWContext.Instances
                            ? CreateApplyDotsBehaviorInstance(onUnit => Me.CurrentTarget, ret => true)
                            : CreateApplyDotsBehaviorNormal(onUnit => Me.CurrentTarget)
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Warlock, WoWSpec.WarlockAffliction, priority: 999)]
        public static Composite CreateWarlockAfflictionHeal()
        {
            return new PrioritySelector(
                CreateWarlockDiagnosticOutputBehavior("Combat")
                );
        }


        [Behavior(BehaviorType.Combat, WoWClass.Warlock, WoWSpec.WarlockAffliction)]
        public static Composite CreateWarlockAfflictionNormalCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                // Movement.CreateEnsureMovementStoppedBehavior(35f),

                new Action(r => { if ( Me.GotTarget()) Me.CurrentTarget.TimeToDeath(); return RunStatus.Failure; } ),

                // cancel an early drain soul if DoTs are falling off
                CancelChanneledCastBehavior(),

                Spell.WaitForCastOrChannel(),

                new Decorator(ret => !Spell.IsGlobalCooldown(),

                    new PrioritySelector(

                        Helpers.Common.CreateInterruptBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        new Action(ret =>
                        {
                            _mobCount = Common.TargetsInCombat.Count();
                            return RunStatus.Failure;
                        }),

                        // Artifact Weapon
                        new Decorator(
                            ret => WarlockSettings.UseArtifactOnlyInAoE && Unit.NearbyUnitsInCombatWithMeOrMyStuff.Count() > 1,
                            new PrioritySelector(
                                Spell.Cast("Reap Souls", ret => WarlockSettings.UseDPSArtifactWeaponWhen != UseDPSArtifactWeaponWhen.None && Me.GetAuraByName("Tormented Souls")?.StackCount >= WarlockSettings.ArtifactTormentedSoulCount)
                            )
                        ),
                        Spell.Cast("Reap Souls", ret => !WarlockSettings.UseArtifactOnlyInAoE && WarlockSettings.UseDPSArtifactWeaponWhen != UseDPSArtifactWeaponWhen.None && Me.GetAuraByName("Tormented Souls")?.StackCount >= WarlockSettings.ArtifactTormentedSoulCount),

                        Spell.BuffSelf("Soul Harvest", when => Me.Level < 100 || Me.HasAura("Deadwind Harvester") || Me.GetAuraStacks("Wrath of Consumption") > 2),

                        CreateAoeBehavior(),

                        Common.CastCataclysm(),

                        CreateApplyDotsBehaviorNormal(on => Me.CurrentTarget),
                        Spell.Cast("Summon Doomguard", req => !Common.HasTalent(WarlockTalents.GrimoireOfSupremacy)),
                        Spell.Cast("Haunt"),
                        Spell.Cast("Grimoire: Felhunter"),
                        Spell.Buff("Life Tap", when => Me.ManaPercent < 50),
                        Spell.Cast("Drain Soul"),
                        Spell.Cast("Shadow Bolt")
                        )
                    )
                );

        }

        private static Composite CancelChanneledCastBehavior()
        {
            return new PrioritySelector(

                ctx => Me.ChanneledSpell == null ? null : Me.ChanneledSpell.Name,

                new Decorator(
                    ret => {
                        // true: evaluate if we need to cancel, false: let it continue
                        if (ret != null && Me.GotTarget())
                            return ((string)ret) == "Drain Soul" || ((string)ret) == "Drain Life";
                        return false;
                        },
                    new PrioritySelector(
                        new Decorator(
                            ret =>
                            {
                                if (Spell.IsGlobalCooldown())
                                    return false;

                                return ShouldWeCancelChanneledCast();
                            },
                            new Sequence(
                                new Action(ret => SpellManager.StopCasting()),
                                new WaitContinue(TimeSpan.FromMilliseconds(500), ret => Me.ChanneledSpell == null, new ActionAlwaysSucceed())
                                )
                            )
                        )
                    )
                );
        }

        public static bool ShouldWeCancelChanneledCast()
        {
            string spellName = Me.ChanneledSpell == null ? null : Me.ChanneledSpell.Name;

            if (spellName == null)
                return false;

            int dotsNeeded = GetDotsMissing(Me.CurrentTarget);
            if (dotsNeeded > (MaxDotCount - 1) && Me.CurrentTarget.IsPlayer)
            {
                Logger.Write(LogColor.Cancel, "/cancel {0}: player {1} needs DoTs {2}/{3}", spellName, Me.CurrentTarget.SafeName(), dotsNeeded, MaxDotCount);
                return true;
            }
            long ttd = Me.CurrentTarget.TimeToDeath(0);
            if (dotsNeeded > (MaxDotCount - 1) && ttd > 6)
            {
                Logger.Write(LogColor.Cancel, "/cancel {0}: {1} needs all DoTs {2}/{3}", spellName, Me.CurrentTarget.SafeName(), dotsNeeded, MaxDotCount);
                return true;
            }
            if (dotsNeeded == MaxDotCount && ttd > 2)
            {
                Logger.Write(LogColor.Cancel, "/cancel {0}: {1} needs more DoTs {2}/{3}", spellName, Me.CurrentTarget.SafeName(), dotsNeeded, MaxDotCount);
                return true;
            }
            return false;
        }


        public static Composite CreateAoeBehavior()
        {
            return new Decorator(
                ret => Spell.UseAOE,
                new PrioritySelector(

                    new Decorator(
                        ret => _mobCount >= 4 && SpellManager.HasSpell("Seed of Corruption"),
                        new PrioritySelector(
                            // roll SoC on targets in combat that we are facing
                            new PrioritySelector(
                                ctx => Common.TargetsInCombat.FirstOrDefault(m => !m.HasAura("Seed of Corruption")),
                                new Sequence(
                                    new PrioritySelector(
                                        Common.CreateCastSoulburn(req => req != null),
                                        new ActionAlwaysSucceed()
                                        ),
                                    Spell.Cast("Seed of Corruption", on => (WoWUnit)on)
                                    )
                                )
                            )
                        ),

                    Common.CastCataclysm(),

                    new Decorator(
                        ret => _mobCount >= 2,
                        new PrioritySelector(
                            CreateApplyDotsBehaviorNormal(ctx => Common.TargetsInCombat.FirstOrDefault(m => m.HasAuraExpired("Corruption", 0))),
                            CreateApplyDotsBehaviorNormal(ctx => Common.TargetsInCombat.FirstOrDefault(m => m.HasAuraExpired("Agony", 2)))
                            // CreateApplyDotsBehaviorInstance(ctx => Common.TargetsInCombat.FirstOrDefault(m => m.HasAuraExpired("Unstable Affliction", 2)), soulBurn => true)
                            )
                        )
                    )
                );
        }

        public static Composite CreateApplyDotsBehaviorNormal(UnitSelectionDelegate onUnit)
        {
            System.Diagnostics.Debug.Assert(onUnit != null);

            return new PrioritySelector(

                ctx => onUnit(ctx),

                new Decorator(
                    req => Unit.ValidUnit((WoWUnit) req),

                    new PrioritySelector(
                        CreateCastSoulSwap(on => (WoWUnit) on),

                        new Decorator(
                            req => GetSoulSwapDotsNeeded((WoWUnit)req) > 0,
                            new PrioritySelector(
                                Spell.Buff("Agony", 3, on => (WoWUnit)on, ret => true),
                                Spell.Buff("Corruption", 3, on => (WoWUnit) on, ret => !Common.HasTalent(WarlockTalents.AbsoluteCorruption)),
                                Spell.Buff("Corruption", ret => Common.HasTalent(WarlockTalents.AbsoluteCorruption)),
                                Spell.Buff("Siphon Life"),
                                Spell.Cast("Unstable Affliction", req =>
                                    !((WoWUnit)req).HasAura("Unstable Affliction")
                                    || (((WoWUnit)req).GetAuraStacks("Unstable Affliciton") <= 2 && Me.GetPowerInfo(WoWPowerType.SoulShards).Current >= 3 && ((WoWUnit)req).TimeToDeath() > 4)
                                ),
                                new Action(r => {
                                    Logger.WriteDebug("ApplyDots: no DoTs needed on {0}", ((WoWUnit)r).SafeName());
                                    return RunStatus.Failure;
                                    })
                                )
                            )
                        )
                    )
                );
        }

        public static Composite CreateApplyDotsBehaviorInstance(UnitSelectionDelegate onUnit, SimpleBooleanDelegate soulBurn)
        {
            System.Diagnostics.Debug.Assert(onUnit != null);

            return new PrioritySelector(

                new Decorator(
                    ret => !Me.HasAura("Soulburn"),
                    new PrioritySelector(
                // target below 20% we have a higher prior on Haunt (but skip if soulburn already up...)
                        Spell.Buff("Haunt",
                            2,
                            onUnit,
                            req => Me.CurrentSoulShards > 0
                                && Me.CurrentTarget.HealthPercent < 20
                                && !Me.HasAura("Soulburn")
                            ),

                        // otherwise, save 2 shards for Soulburn and instant pet rez if needed (unless Misery buff up)
                        Spell.Buff("Haunt", 2, onUnit, req => Me.CurrentSoulShards > 2 || Me.HasAura("Dark Soul: Misery"))
                        )
                    ),

                CreateCastSoulSwap(onUnit),

                new Action(ret =>
                {
                    DotCountNeeded = 0;
                    if (onUnit != null && onUnit(ret) != null)
                    {
                        // if mob dying very soon, skip DoTs
                        if (onUnit(ret).TimeToDeath(99) < 4)
                            DotCountNeeded = 4;
                        else
                        {
                            if (!onUnit(ret).HasAuraExpired("Agony", 3))
                                ++DotCountNeeded;
                            if (!onUnit(ret).HasAuraExpired("Corruption", 3))
                                ++DotCountNeeded;
                            if (!onUnit(ret).HasAuraExpired("Unstable Affliction", 3))
                                ++DotCountNeeded;
                            if (!onUnit(ret).HasAuraExpired("Haunt", 3))
                                ++DotCountNeeded;
                        }
                    }
                    // Logger.WriteDebug("CreateApplyDotsBehavior: DotCount={0}", DotCountNeeded );
                    return RunStatus.Failure;
                }),
                new Decorator(
                    req => DotCountNeeded < 4,
                    new PrioritySelector(
                        Spell.Buff("Agony", 3, onUnit, ret => true),
                        Spell.Buff("Corruption", 3, onUnit, ret => true),
                        Spell.Buff("Unstable Affliction", 3, onUnit, req => true)
                        )
                    )
                );
        }


        delegate bool NeedSoulSwapDelegate(WoWUnit unit);

        public static Composite CreateCastSoulSwap(UnitSelectionDelegate onUnit)
        {
            const string SOUL_SWAP = "Soul Swap";
            if (!SpellManager.HasSpell(SOUL_SWAP))
                return new ActionAlwaysFail();

            NeedSoulSwapDelegate needSoulSwap = NeedSoulSwapNormal;

            return new Decorator(
                req => Me.CurrentSoulShards >= 2,
                new Sequence(
                    ctx => onUnit(ctx),
                    new Decorator(
                        req => needSoulSwap((WoWUnit)req),
                        new ActionAlwaysSucceed()
                        ),
                    Common.CreateCastSoulburn(req => true),
                    new Action(ret =>
                    {
                        Logger.Write(LogColor.SpellNonHeal,
                            $"*Soul Swap on {((WoWUnit) ret).SafeName()} @ {((WoWUnit) ret).HealthPercent:F1}% at {((WoWUnit) ret).SpellDistance():F1} yds");
                        if (!Spell.CastPrimative("Soul Swap", onUnit(ret)))
                            return RunStatus.Failure;
                        return RunStatus.Success;
                    })
                    )
                );
        }

        private static bool NeedSoulSwapNormal(WoWUnit unit)
        {
            if (!unit.IsAlive)
                return false;

            if (GetSoulSwapDotsNeeded(unit) < 2)
                return false;

            if (unit.SpellDistance() > 40)
                return false;

            return unit.InLineOfSpellSight;
        }

        private static bool NeedSoulburnHauntNormal(WoWUnit unit)
        {
            if (!unit.IsAlive)
                return false;

            return !(unit.SpellDistance() > 40) && unit.InLineOfSpellSight;
        }

        private static int GetSoulSwapDotsNeeded(WoWUnit unit)
        {
            int dotCount = 0;
            if (unit.HasAuraExpired("Agony"))
                dotCount++;
            if (unit.HasAuraExpired("Corruption"))
                dotCount++;
            if (unit.HasAuraExpired("Unstable Affliction"))
                dotCount++;
            return dotCount;
        }

        private static int GetDotsMissing(WoWUnit unit)
        {
            int dotCount = 0;
            if (unit.HasAuraExpired("Agony", 0))
                dotCount++;
            if (unit.HasAuraExpired("Corruption", 0))
                dotCount++;
            if (unit.HasAuraExpired("Unstable Affliction", 0))
                dotCount++;
            return dotCount;
        }

        private static WoWUnit GetBestAoeTarget()
        {
            WoWUnit unit = null;

            if (SpellManager.HasSpell("Seed of Corruption"))
                unit = Clusters.GetBestUnitForCluster(Common.TargetsInCombat.Where(m => !m.HasAura("Seed of Corruption")), ClusterType.Radius, 15f);

            if (SpellManager.HasSpell("Agony"))
                unit = Common.TargetsInCombat.FirstOrDefault(t => !t.HasMyAura("Agony"));

            return unit;
        }

        private static Composite CreateWarlockDiagnosticOutputBehavior(string sState = null)
        {
            if (!SingularSettings.Debug)
                return new Action(ret => RunStatus.Failure);

            return new ThrottlePasses(1,
                new Action(ret =>
                {
                    string sMsg;
                    sMsg =
                        $".... [{sState}] h={Me.HealthPercent:F1}%, m={Me.ManaPercent:F1}%, moving={Me.IsMoving.ToYN()}, pet={(Me.GotAlivePet ? Me.Pet.HealthPercent : 0):F0}% @ {(Me.GotAlivePet ? Me.Pet.Distance : 0):F0} yds, soulburn={(long) Me.GetAuraTimeLeft("Soulburn", true).TotalMilliseconds}";

                    WoWUnit target = Me.CurrentTarget;
                    if (target != null)
                    {
                        sMsg +=
                            $", {target.SafeName()}, {target.HealthPercent:F1}%, dies={target.TimeToDeath()} secs, {target.Distance:F1} yds, loss={target.InLineOfSpellSight.ToYN()}, face={Me.IsSafelyFacing(target).ToYN()}, agony={(long) target.GetAuraTimeLeft("Agony", true).TotalMilliseconds}, corr={(long) target.GetAuraTimeLeft("Corruption", true).TotalMilliseconds}, ua={(long) target.GetAuraTimeLeft("Unstable Affliction", true).TotalMilliseconds}, haunt={(long) target.GetAuraTimeLeft("Haunt", true).TotalMilliseconds}, seed={(long) target.GetAuraTimeLeft("Seed of Corruption", true).TotalMilliseconds}, aoe={_mobCount}";
                    }

                    Logger.WriteDebug(Color.LightYellow, sMsg);
                    return RunStatus.Failure;
                })
                );
        }
    }
}