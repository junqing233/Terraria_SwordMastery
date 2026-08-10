using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using SwordMastery.Content.Items.Mterial;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SwordMastery.Content.Items.FlyingSword.BladeForge
{
    class BladeForgeTile : ModTile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/BladeForge/BladeForgeTile";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileID.Sets.FramesOnKillWall[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16};
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            
            DustType = DustID.Stone;
            AnimationFrameHeight = 54;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(200, 200, 200), Language.GetText(Language.ActiveCulture.Name == "zh-Hans" ? "铸剑台" : "Blade Forge"));

            // 作为熔炉和铁砧使用
            AdjTiles = new int[] { TileID.Furnaces, TileID.Anvils };

        }
        public override void NearbyEffects(int i, int j, bool closer)
        {
            //// 只在Tile的左上角格子生成粒子
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX == 0 && tile.TileFrameY == 0)
            {
                if (Main.rand.NextBool(100))
                {
                    // 以整个Tile为基准的中心点
                    Vector2 centerPos = new Vector2(i * 16 + 36, j * 16 + 24); // 4x3瓦片的中心
                    // 主要向上，X方向小幅随机，Y方向大概率为负
                    float speed = Main.rand.NextFloat(0.5f, 2f);
                    float angle = MathHelper.ToRadians(Main.rand.NextFloat(-40f, 40f)); // -40~40度，主要向上
                    Vector2 velocity = speed * new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));

                    Dust dust = Dust.NewDustPerfect(
                        centerPos,
                        DustID.Torch,
                        velocity,
                        150,
                        default,
                        Main.rand.NextFloat(1.2f, 1.8f)
                    );
                    dust.noGravity = false;
                    dust.scale = Main.rand.NextFloat(0.5f, 1f);
                    dust.fadeIn = Main.rand.NextFloat(0.5f, 1.2f);
                }
            }
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            // 发出橙色火光（类似熔炉）
            r = 0.9f;
            g = 0.55f;
            b = 0.18f;
        }
        //public override void PlaceInWorld(int i, int j, Item item)
        //{
        //    // 铸剑台的贴图是 4x3，所以需要调整坐标
        //    i -= 1;
        //    j -= 1;
        //    base.PlaceInWorld(i, j, item);
        //}
        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            frameCounter++;
            if (frameCounter > 12) // 控制动画速度，6可以调整为更快/更慢
            {
                frameCounter = 0;
                frame++;
                if (frame >= 6) // 6为总帧数
                    frame = 0;
            }
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            return true;
        }
        //public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        //{
            
        //}
    }

    class BladeForge : ModItem
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/BladeForge/BladeForge";

        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 38;
            Item.maxStack = 1;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<BladeForgeTile>();
        }

        public override void AddRecipes()
        {
            // 创建一个新的配方组
            RecipeGroup group = new RecipeGroup(() => "铁砧或铅砧",
                ItemID.IronAnvil,
                ItemID.LeadAnvil);
            // 注册配方组
            RecipeGroup.RegisterGroup("FurnaceOrLeadAnvil", group);

            CreateRecipe()
               .AddRecipeGroup("FurnaceOrLeadAnvil", 1) // 使用配方组
               .AddIngredient(ItemID.Furnace, 1)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}