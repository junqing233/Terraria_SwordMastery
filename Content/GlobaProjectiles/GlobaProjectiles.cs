using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SwordMastery.Content.GlobaProjectiles
{
    public class SwordMasteryConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("体验设置")]
        [Label("模组强度体验")]
        [Tooltip("选择模组整体体验强度：'我即神明'为超模体验，'追求平凡'为普通体验")]
        [DefaultValue(StrengthMode.Ordinary)]
        public StrengthMode StrengthExperience;

        [Label("启用剑之大师模组前缀")]
        [Tooltip("开启后，可激活模组前缀")]
        [DefaultValue(true)]
        public bool EnableSwordMasteryPrefixes { get; set; }

        [Label("启用模组额外超模词条")]
        [Tooltip("开启后，在‘我即神明’强度下，可以激活额外超模词条效果")]
        [DefaultValue(false)]
        public bool EnableExtraAffix;

        [Header("视觉设置")]
        [Label("友方弹幕透明度")]
        [Tooltip("设置友方弹幕的透明度，0为完全透明，1为完全不透明。")]
        [Range(0f, 1f)]
        [DefaultValue(1f)]
        [SliderColor(102, 170, 193)]
        public float FriendlyProjectileAlpha { get; set; }
    }

    public enum StrengthMode
    {
        [Label("我即神明")]
        God,
        [Label("追求平凡")]
        Ordinary
    }
}
namespace SwordMastery.Content.GlobaProjectiles
{
    public class FlyingSwordGlobalProj : GlobalProjectile
    {
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (projectile.friendly && !projectile.hostile)
            {
                float alpha = ModContent.GetInstance<SwordMasteryConfig>().FriendlyProjectileAlpha;
                //projectile.alpha = (int)(255 * (1f - alpha));
                // 不再直接修改 lightColor
                projectile.hide = alpha <= 0f;
                lightColor *= alpha;
            }
            
            //if (projectile.type == ProjectileID.Smolstar)
            //{
            //    Main.NewText($"ai[0]:{projectile.ai[0]}");
            //    Main.NewText($"ai[1]:{projectile.ai[1]}");
            //    Main.NewText($"ai[2]:{projectile.ai[2]}");
            //}
            return base.PreDraw(projectile, ref lightColor);
        }
    }
}