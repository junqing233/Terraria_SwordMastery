using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace SwordMastery.Content.Items.Weapons.Sword
{
    public class MasterSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.crit = 9;
            Item.DamageType = DamageClass.Melee;
            Item.width = 38;
            Item.height = 38;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(0,0,0,64);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;// 自动使用
            //Item.useTurn = true;// 自动转向
            Item.noUseGraphic = false;// 取消使用图标//false为显示使用图标
            //Item.shoot = ModContent.ProjectileType<TenthSwordProj1>();
            //Item.shootSpeed = 12f;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
           
        }
        public override bool? UseItem(Player player)
        {
            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.Center,
                player.DirectionTo(Main.MouseWorld),
                ProjectileID.LastPrism,
                //ProjectileID.WoodenArrowFriendly,
                Item.damage,
                Item.knockBack,
                player.whoAmI,
                0,
                2
                );

            return true;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            //target.defense -= 6; // 减少目标的防御力
            //if (target.defense < 0)
            //{
            //    target.defense = 0; // 确保防御力不会变为负数
            //}
        }
    }
}
