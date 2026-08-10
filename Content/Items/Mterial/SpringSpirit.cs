using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Mterial
{
    public class SpringSpirit : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemIconPulse[Item.type] = true; // The item pulses while in the player's inventory
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes the item have no gravity
            Item.ResearchUnlockCount = 25; // Configure the amount of this item that's needed to research it in Journey mode.
        }
        public override void SetDefaults()
        {
            Item.width = 19;
            Item.height = 26;
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(silver: 68);
            Item.rare = ItemRarityID.Orange;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Type].Value;
            Vector2 position = Item.position - Main.screenPosition + new Vector2(Item.width / 2, Item.height - texture.Height * 0.5f);

            // 动态虚影缩放：0.7f 到 0.5f 之间平滑变化
            float t = (MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) + 1f) / 2f; // t在0~1之间
            float shadowScale = 1f + 0.2f * t;
            float itemScale = 1f;

            // 绘制虚影
            spriteBatch.Draw(
                texture,
                position,
                null,
                new Color(100, 255, 255),
                rotation,
                texture.Size() * 0.5f,
                shadowScale,
                SpriteEffects.None,
                0f
            );

            // 绘制本体
            spriteBatch.Draw(
                texture,
                position,
                null,
                lightColor,
                rotation,
                texture.Size() * 0.5f,
                itemScale,
                SpriteEffects.None,
                0f
            );

            return false;
        }
        public override void PostUpdate()
        {
            float intensity = 2f; // 控制光芒强度，越小越淡
            //Color(100, 255, 255)
            //•	R: 100 / 255 ≈ 0.392
            //•	G: 255 / 255 ≈ 1
            //•	B: 255 / 255 ≈ 1
            Lighting.AddLight(Item.Center, 0.392f * intensity, 1f * intensity, 1f * intensity);
        }
    }
    public class SpringSpiritPlayer : ModPlayer
    {
        public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
        {
            // 判断神圣环境
            bool isHallow = Player.ZoneHallow && !attempt.inLava && !attempt.inHoney;
            // 判断血月
            bool isBloodMoon = Main.bloodMoon;

            if (isHallow && isBloodMoon && Main.rand.NextBool(20))//200
            {
                itemDrop = ModContent.ItemType<SpringSpirit>();
            }
        }
    }
}