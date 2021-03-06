﻿using System.ComponentModel;
using System.IO;
using Styx.Helpers;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    public enum WarlockPet
    {
        None        = 0,
        Auto        = 1,
        Imp         = 23,       // Pet.CreatureFamily.Id
        Voidwalker  = 16,
        Succubus    = 17,
        Felhunter   = 15,
        Felguard    = 29,
        Doomguard   = 19,
        Infernal	= 108,
        Other       = 99999     // a quest or other pet forced upon us for some reason
    }

    public enum Soulstone
    {
        None = 0,
        Auto,
        Self,
        Ressurect
    }

    internal class WarlockSettings : Styx.Helpers.Settings
    {

        public WarlockSettings()
            : base(Path.Combine(SingularSettings.SingularSettingsPath, "Warlock.xml"))
        {
        }

        #region Category: Artifact Weapon
        [Setting]
        [DefaultValue(UseDPSArtifactWeaponWhen.OnCooldown)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Use When...")]
        [Description("Toggle when the artifact weapon ability should be used. NOTE: OnCooldown and AtHighestDPSOpportunity does not affect the Demonology and Affliction artifact weapon.")]
        public UseDPSArtifactWeaponWhen UseDPSArtifactWeaponWhen { get; set; }

        [Setting]
        [DefaultValue(5)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Affliction: Tormented Soul Count")]
        [Description("This is how many tormented souls must be spawned before the Affliction artifact weapon is used.")]
        public int ArtifactTormentedSoulCount { get; set; }

        [Setting]
        [DefaultValue(5)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Demonology: Active Demon Count")]
        [Description("This is how many demons we must have summoned before the Demonology artifact weapon is used.")]
        public int ArtifactDemonCount { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Use Only In AoE")]
        [Description("If set to true, this will make the artifact waepon only be used when more than one mob is attacking us.")]
        public bool UseArtifactOnlyInAoE { get; set; }
        #endregion

        [Setting]
        [DefaultValue(WarlockPet.Auto)]
        [Category("Pet")]
        [DisplayName("Pet to Summon")]
        [Description("Auto: will automatically select best pet.")]
        public WarlockPet Pet { get; set; }

        [Setting]
        [DefaultValue(60)]
        [Category("Pet")]
        [DisplayName("Health Funnel at %")]
        [Description("Pet Health % to begin Health Funnel in combat")]
        public int HealthFunnelCast { get; set; }

        [Setting]
        [DefaultValue(95)]
        [Category("Pet")]
        [DisplayName("Health Funnel cancel at %")]
        [Description("Pet Health % to cancel Health Funnel in combat")]
        public int HealthFunnelCancel { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Pet")]
        [DisplayName("Health Funnel resting below %")]
        [Description("Pet Health % to cast Health Funnel while resting")]
        public int HealthFunnelRest { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Pet")]
        [DisplayName("Use Disarm")]
        [Description("True: use Disarm on cooldown; False: do not cast")]
        public bool UseDisarm { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Common")]
        [DisplayName("Mortal Coil at Health %")]
        [Description("Will use Mortal Coil to heal, will not cast if Use Fear is set to False.")]
        public int MortalCoilHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Create Soulwell If In Group")]
        [Description("Creates a Soulwell if in a Group at certain point (Battlefield start, etc)")]
        public bool CreateSoulwell { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Use Fear")]
        [Description("Use Fear when low health or controlling adds")]
        public bool UseFear { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Common")]
        [DisplayName("Use Fear Count")]
        [Description("Use Fear when this many attacking Warlock (not pet); 0 to disable mob count based check")]
        public int UseFearCount { get; set; }

        [Setting]
        [DefaultValue(Soulstone.Auto)]
        [Category("Common")]
        [DisplayName("Use Soulstone")]
        [Description("Auto: Instances=Ressurect, Normal/Battleground=Self, Disabled Movement=None -- Ressurrect requires Singular Combat Rez settings to be set as well")]
        public Soulstone UseSoulstone { get; set; }

        [Setting]
        [DefaultValue(90)]
        [Category("Common")]
        [DisplayName("Burning Rush Health %")]
        [Description("Will cast Burning Rush if moving and Health above this %")]
        public int BurningRushHealthCast { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Common")]
        [DisplayName("Burning Rush Cancel %")]
        [Description("Will cancel Burning Rush if Health falls below this %")]
        public int BurningRushHealthCancel { get; set; }

        [Setting]
        [DefaultValue(250)]
        [Category("Common")]
        [DisplayName("Burning Rush Cancel Stopped (ms)")]
        [Description("Will cancel Burning Rush if stopped for this many milliseconds")]
        public int BurningRushStopTimeCancel { get; set; }

        [Setting]
        [DefaultValue(1500)]
        [Category("Common")]
        [DisplayName("Burning Rush Suspend Max (ms)")]
        [Description("Will prevent Burning Rush cast for max time (ms) after cancelling")]
        public int BurningRushMaxSuspend { get; set; }

        [Setting]
        [DefaultValue(750)]
        [Category("Common")]
        [DisplayName("Burning Rush Suspend Min (ms)")]
        [Description("Will prevent Burning Rush cast for min time (ms) after cancelling")]
        public int BurningRushMinSuspend { get; set; }

        [Setting]
        [DefaultValue(1)]
        [Category("Demonology")]
        [DisplayName("Felstorm Mob Count")]
        [Description("0: disable ability, otherwise mob count required within 8 yds.  Controls Wrathstorm also")]
        public int FelstormMobCount { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Demonology")]
        [DisplayName("Stun While Solo")]
        [Description("True: use Axe Toss on cooldown when Solo; False: save for Casters and Enemy Players")]
        public bool StunWhileSolo { get; set; }

        public enum SpellPriority
        {
            Noxxic = 1,
            IcyVeins = 2
        }
#if SUPPORT_MULTIPLE_PRIORITIES
        [Setting]
        [DefaultValue(SpellPriority.Noxxic )]
        [Category("Destruction")]
        [DisplayName("Spell Priority Selection")]
        public SpellPriority DestructionSpellPriority { get; set; }
#endif


#region Setting Helpers


#endregion

    }
}