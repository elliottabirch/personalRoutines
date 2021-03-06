﻿
using System;
using System.ComponentModel;
using System.IO;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    internal class HunterSettings : Styx.Helpers.Settings
    {
        public HunterSettings()
            : base(Path.Combine(SingularSettings.SingularSettingsPath, "Hunter.xml"))
        {
        }

        #region Category: Pet

        [Setting]
        [DefaultValue(1)]
        [Category("Pet")]
        [DisplayName("(Solo) Pet Number ( 1 thru 5 )")]
        public int PetNumberSolo { get; set; }

        [Setting]
        [DefaultValue(1)]
        [Category("Pet")]
        [DisplayName("(PvP) Pet Number ( 1 thru 5 )")]
        public int PetNumberPvp { get; set; }

        [Setting]
        [DefaultValue(1)]
        [Category("Pet")]
        [DisplayName("(Instance) Pet Number ( 1 thru 5 )")]
        public int PetNumberInstance { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Pet")]
        [DisplayName("Mend Pet Percent")]
        public int MendPetPct { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Pet")]
        [DisplayName("Use Pet Buffs")]
        public bool PetBuffs { get; set; }

        #endregion

        #region Category: Common

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Use Fetch")]
        [Description("Will loot mobs further than 10 yds away via Fetch")]
        public bool UseFetch { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Use Barrage")]
        [Description("True: Will cast barrage; False: don't cast.")]
        public bool UseBarrage { get; set; }

		[Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Use Misdirection in Instances")]
        [Description("Cast Misdirection on Tank during Pull in Instances, and on Pet if all Tanks dead and Hunter gets aggro")]
        public bool UseMisdirectionInInstances { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Widow Venom")]
        [Description("True: keep debuff up on players; False: don't cast.")]
        public bool UseWidowVenom { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Crowd Control")]
        [DisplayName("Crowd Ctrl Focus Unit")]
        [Description("True: Crowd Control used only for Focus Unit; False: used on any add.")]
        public bool CrowdControlFocus { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Common")]
        [DisplayName("Deterrence Health %")]
        [Description("Health % to cast Deterrence if in Combat")]
        public int DeterrenceHealth { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Common")]
        [DisplayName("Deterrence Count")]
        [Description("Number of enemies attacking Hunter (pets not included)")]
        public int DeterrenceCount { get; set; }

        [Setting]
        [DefaultValue(10)]
        [Category("Common")]
        [DisplayName("Feign Death Health %")]
        [Description("Health % where we cast Feign Death")]
        public int FeignDeathHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Feign Death for Enemy Pets")]
        [Description("Cast FD on cooldown if being attacked by a Player Pet")]
        public bool FeignDeathPvpEnemyPets { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Feign Death in Instances")]
        [Description("Cast FD to drop agro if targeted during fight; does not attempt to survive wipes")]
        public bool FeignDeathInInstances { get; set; }

        [Setting]
        [DefaultValue(5)]
        [Category("Common")]
        [DisplayName("Camouflage: Health %")]
        [Description("Cast if health below this % and have Enhanced Camouflage (Level 92+); 0: disable")]
        public int ImprovedCamouflageHealth { get; set; }


        #endregion

        #region Category: Artifact Weapon
        [Setting]
        [DefaultValue(UseDPSArtifactWeaponWhen.OnCooldown)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Use When...")]
        [Description("Toggle when the artifact weapon ability should be used.")]
        public UseDPSArtifactWeaponWhen UseDPSArtifactWeaponWhen { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Artifact Weapon Usage")]
        [DisplayName("Use Only In AoE")]
        [Description("If set to true, this will make the artifact waepon only be used when more than one mob is attacking us.")]
        public bool UseArtifactOnlyInAoE { get; set; }
        #endregion
    }
}