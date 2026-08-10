using SwordMastery.Content.Items.FlyingSword.AGlobalControl;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.FlyingSword
{
    public static class SwordProjectileGroup
    {
        public static readonly HashSet<int> AllTypes = new()
        {
            ModContent.ProjectileType<FlyingIronBroadswordProj>(),
            ModContent.ProjectileType<FlyingSilverBroadswordProj>(),
            ModContent.ProjectileType<FlyingGoldBroadswordProj>(),
            ModContent.ProjectileType<FlyingLightsBaneProj>(),
            ModContent.ProjectileType<FlyingBloodButchererProj>(),
            ModContent.ProjectileType<FlyingBladeofGrassProj>(),
            ModContent.ProjectileType<FlyingBeeKeeperProj>(),
            ModContent.ProjectileType<FlyingStarfuryProj>(),
            ModContent.ProjectileType<FlyingEnchantedSwordProj>(),
            ModContent.ProjectileType<FlyingIceBladeProj>(),
            ModContent.ProjectileType<FlyingFieryGreatswordProj>(),
            ModContent.ProjectileType<FlyingBoneSwordProj>(),
            ModContent.ProjectileType<FlyingTerragrimProj>(),
            ModContent.ProjectileType<FlyingNightsEdgeProj>(),
            ModContent.ProjectileType<DemonBladeProj>(),
            ModContent.ProjectileType<FlyingLeadBroadswordProj>(),
            ModContent.ProjectileType<FlyingTungstenBroadswordProj>(),
            ModContent.ProjectileType<FlyingPlatinumBroadswordProj>(),
            ModContent.ProjectileType<FlyingCobaltSwordProj>(),
            ModContent.ProjectileType<FlyingPalladiumSwordProj>(),
            ModContent.ProjectileType<FlyingMuramasaProj>(),
            ModContent.ProjectileType<FlyingMythrilSwordProj>(),
            ModContent.ProjectileType<FlyingOrichalcumSwordProj>(),
            ModContent.ProjectileType<FlyingTitaniumSwordProj>(),
            ModContent.ProjectileType<FlyingAdamantiteSwordProj>(),
            ModContent.ProjectileType<FlyingExcaliburProj>(),
            ModContent.ProjectileType<FlyingTrueExcaliburProj>(),
            ModContent.ProjectileType<FlyingTrueNightsEdgeProj>(),
            ModContent.ProjectileType<FlyingTerraBladeProj>(),
            ModContent.ProjectileType<FlyingswordProj>(),
            ModContent.ProjectileType<FlyingSeedlerProj>(),
            ModContent.ProjectileType<FlyingTizonaSwordProj>(),
            ModContent.ProjectileType<FlyingTheHorsemansBladeProj>(),
            ModContent.ProjectileType<FlyingInfluxWaverProj>(),
            ModContent.ProjectileType<FlyingStarWrathProj>(),
            ModContent.ProjectileType<FlyingMeowmereProj>(),
            ModContent.ProjectileType<FlyingZenithProj>(),
            ModContent.ProjectileType<DemonSuppressingSwordProj>(),
        };
    }
}