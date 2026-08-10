using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    [AutoloadEquip(EquipType.Back)]
    public class Quiver : ModItem
    {
        public override void SetDefaults()
        {
            int realBackSlot = Item.backSlot;
            Item.CloneDefaults(ItemID.StalkersQuiver);
            Item.value = 20000;
            Item.rare = ItemRarityID.White;
            // CloneDefaults 会清除自动加载的背部槽位，所以需要这样保存
            Item.backSlot = realBackSlot;
            Item.width = Item.height = 32;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.WoodenArrow, 999)
                .AddIngredient(ItemID.Silk, 6)
                .AddIngredient(ItemID.Leather, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            //player.maxMinions += 100;
            player.GetModPlayer<QuiverPlayer>().hasQuiver = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return true;
        }
    }
    public class QuiverPlayer : ModPlayer
    {
        public bool hasQuiver;

        public override void ResetEffects()
        {
            hasQuiver = false;
        }
        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 判断是否装备了箭袋，且武器为弓，且弹幕为箭
            if (hasQuiver && item.DamageType == DamageClass.Ranged && item.useAmmo == AmmoID.Arrow)
            {
                // 发射一个木箭弹幕
                Projectile.NewProjectile(
                    source,
                    position + new Vector2(0,10),
                    velocity,
                    ProjectileID.WoodenArrowFriendly,
                    damage,
                    knockback,
                    Player.whoAmI
                );
            }
            return base.Shoot(item, source, position, velocity, type, damage, knockback);
        }
    }
}