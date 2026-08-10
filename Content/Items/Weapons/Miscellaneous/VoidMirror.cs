using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using SwordMastery.Content.Items.FlyingSword.AGlobalControl;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI;
using static Terraria.GameContent.Animations.Actions.Sprites;


namespace SwordMastery.Content.Items.Weapons.Miscellaneous
{
    class VoidMirror : ModTile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Miscellaneous/VoidMirror";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileID.Sets.FramesOnKillWall[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileObjectData.newTile.Width = 11;
            TileObjectData.newTile.Height = 14;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16};
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;

            //AnimationFrameHeight = 54;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(0, 255, 249), Language.GetText(Language.ActiveCulture.Name == "zh-Hans" ? "虚无魔镜" : "Void Mirror"));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // 获取瓦片贴图
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // 计算瓦片左上角的屏幕坐标
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16, j * 16) - Main.screenPosition + zero;

            // 计算当前瓦片在大贴图中的偏移
            Tile tile = Main.tile[i, j];
            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;

            // 每一格16x16
            Rectangle sourceRect = new Rectangle(frameX, frameY, 16, 16);
            if(VoidMirrorUI.isClone)
            // 叠加高亮色
            spriteBatch.Draw(
                texture,
                drawPos,
                sourceRect,
                Color.White * 0.6f, // 可自定义颜色和透明度
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0f
            );
        }
        public override void NearbyEffects(int i, int j, bool closer)
        {
            Tile tile = Main.tile[i, j];
            if (VoidMirrorUI.isClone && tile.TileFrameX == 0 && tile.TileFrameY == 0)
            {
                if (VoidMirrorItem.GlobalDayCount != ModContent.GetInstance<VoidMirrorItem>().lastCloneDay)
                {
                    Vector2 center = new Vector2(i * 16 + 88, j * 16 + 112);

                    int circleCount = 8;
                    float r = 20f;
                    int pointsPerArc = 16;

                    // 动态旋转角度（单位：弧度），每帧递增，逆时针
                    float rotation = (float)(Main.GameUpdateCount * 0.01f);

                    Vector2[] circleCenters = new Vector2[circleCount];
                    for (int c = 0; c < circleCount; c++)
                    {
                        float angle = MathHelper.TwoPi * c / circleCount - rotation;
                        circleCenters[c] = center + r * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    }

                    for (int c = 0; c < circleCount; c++)
                    {
                        Vector2 O = circleCenters[c];
                        for (int k = 0; k < circleCount; k++)
                        {
                            if (k == c) continue;
                            Vector2 Q = circleCenters[k];

                            Vector2 OC = center - O;
                            float d = OC.Length();
                            if (d > 2 * r - 0.01f || d < 0.01f) continue;

                            float baseAngle = MathF.Atan2(OC.Y, OC.X);
                            float alpha = MathF.Acos(d / (2 * r));

                            float angle1 = baseAngle + alpha;
                            float angle2 = baseAngle - alpha;

                            float startAngle = angle2;
                            float endAngle = angle1;
                            if (endAngle < startAngle)
                                endAngle += MathHelper.TwoPi;

                            float midAngle = (startAngle + endAngle) / 2f;

                            for (int p = 0; p < pointsPerArc / 2; p++)
                            {
                                float t = (float)p / (pointsPerArc / 2 - 1);
                                float angle = MathHelper.Lerp(midAngle, endAngle, t);
                                Vector2 pos = O + r * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                                if (Main.rand.NextBool(20))
                                {
                                    Dust dust = Dust.NewDustPerfect(pos, DustID.BlueCrystalShard, Vector2.Zero, 200, Color.White, 1.1f);
                                    dust.noGravity = true;
                                    dust.fadeIn = 1.05f;
                                    dust.noLight = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemID.None;
            player.mouseInterface = true;

            //Player player = Main.LocalPlayer; // 获取本地玩家
            player.noThrow = 2; // 禁止投掷
            player.cursorItemIconEnabled = true;// 显示物品图标
            player.cursorItemIconID = ItemID.None; // 物品图标ID
            player.mouseInterface = true; // 鼠标接口开启

            // 我们可以通过获取方块样式并查找对应的物品掉落来确定光标上显示的物品。
            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
        }

        public override bool RightClick(int i, int j)
        {
            Vector2 tilePosition = new Vector2(i * 16, j * 16);
            ModContent.GetInstance<VoidMirrorUISystem>().ToggleUI(tilePosition);
            return true;
        }
    }

    public class VoidMirrorItem : ModItem
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Miscellaneous/VoidMirrorItem";
        public static int MaxItems = 1;
        public Item[] items = Enumerable.Range(0, MaxItems).Select(_ => new Item()).ToArray();
        //public static bool EnableProjectile = true; // 是否启用弹幕
        //public static bool IsTail = false; // 是否拖尾
        //public static float ExtraDrawRotation = 0f; // 额外绘制旋转角度（单位：弧度）
        //public static bool UseThousandBladesMode = false; // 千刀万刮模式开关
        public bool isClick = false;
        public static bool IsClick = false;
        internal static VoidMirrorUI voidMirrorUI;
        public Guid InstanceId = Guid.NewGuid(); // 每个物品唯一
        public static int GlobalDayCount = 0; // 全局天数计数
        public int lastCloneDay = -1; // 上次生成的天数

        // 存档支持
        // 物品创建时初始化物品数组
        public override void OnCreated(ItemCreationContext context)
        {
            InstanceId = Guid.NewGuid(); // 保证每次新建都唯一
            InitializeItems();// 初始化物品数组
        }
        public override void SaveData(TagCompound tag)
        {
            var savedItems = items.Where(item => item != null).Select(item => ItemIO.Save(item)).ToList();
            var savedTextures = items.Where(item => item != null).Select(item => item.type).ToList();

            tag["items"] = savedItems;
            tag["textures"] = savedTextures;
            tag["lastCloneDay"] = lastCloneDay;
        }

        public override void LoadData(TagCompound tag)
        {
            lastCloneDay = tag.GetInt("lastCloneDay");
            if (items == null || items.Length != MaxItems)
                items = new Item[MaxItems];
            for (int i = 0; i < MaxItems; i++)
                items[i] = new Item();

            var loadedItems = tag.GetList<TagCompound>("items");
            var loadedTextures = tag.GetList<int>("textures");

            for (int i = 0; i < loadedItems.Count && i < items.Length; i++)
            {
                items[i] = ItemIO.Load(loadedItems[i]);
                //FlyingSwordWeaponSlotUI.ItemID = items[i].type;
                // 确保物品纹理已加载
                if (items[i] != null && !items[i].IsAir)
                {
                    if (i < loadedTextures.Count)
                    {
                        int itemType = loadedTextures[i];
                        if (itemType >= 0 && itemType < TextureAssets.Item.Length)
                        {
                            if (!TextureAssets.Item[itemType].IsLoaded)
                            {
                                TextureAssets.Item[itemType] = ModContent.Request<Texture2D>($"Terraria/Images/Item_{itemType}");
                            }
                        }
                    }
                }
            }
        }

        // 初始化物品数组
        private void InitializeItems()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new Item();
                items[i].SetDefaults(ItemID.None);
            }
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<VoidMirror>();
        }
        public override void AddRecipes()
        {
            // 创建一个新的配方组
            RecipeGroup group = new RecipeGroup(() => "任意魔镜",
                ItemID.MagicMirror,
                ItemID.IceMirror
                );

            // 注册配方组
            RecipeGroup.RegisterGroup("SwordMastery:VoidMirrorItemGroup", group);

            CreateRecipe()
               .AddRecipeGroup("SwordMastery:VoidMirrorItemGroup", 1) // 使用配方组
               .AddIngredient(ItemID.Frog, 1) // 青蛙
               .AddTile(TileID.WorkBenches) // 工作台
               .Register();
        }
    }
    public class UIItemSlotVoidMirror : UIElement
    {
        private Item[] items;
        private int index;
        public bool isMouseOver = false;

        public UIItemSlotVoidMirror(Item[] items, int index)
        {
            this.items = items;
            this.index = index;
            Width.Set(52f, 0f);
            Height.Set(52f, 0f);
        }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            isMouseOver = true;
            Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            isMouseOver = false;
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            var player = Main.LocalPlayer;
            // 取出物品
            if (!items[index].IsAir && Main.mouseItem.IsAir && !player.ItemAnimationActive)
            {
                Main.mouseItem = items[index].Clone();
                items[index].TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 放入物品
            else if (items[index].IsAir && !Main.mouseItem.IsAir && !player.ItemAnimationActive)
            {
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 交换物品
            else if (!items[index].IsAir && !Main.mouseItem.IsAir && !player.ItemAnimationActive)
            {
                Item temp = items[index].Clone();
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem = temp;
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // 绘制物品槽背景
            spriteBatch.Draw(TextureAssets.InventoryBack9.Value, GetDimensions().ToRectangle(), Color.White * 0.8f);

            // 绘制物品
            if (items[index] != null && !items[index].IsAir)
            {
                Texture2D tex = TextureAssets.Item[items[index].type].Value;
                Rectangle frame = Main.itemAnimations[items[index].type]?.GetFrame(tex) ?? tex.Frame();
                float scale = Math.Min(1f, 30f / (frame.Width + frame.Height) * 2);
                Vector2 pos = GetDimensions().Center() - frame.Size() * 0.5f * scale;
                spriteBatch.Draw(tex, pos, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // 堆叠数量
                if (items[index].stack > 1)
                {
                    Utils.DrawBorderStringFourWay(
                        spriteBatch,
                        FontAssets.ItemStack.Value,
                        items[index].stack.ToString(),
                        pos.X - 2, pos.Y + 20,
                        Color.White, Color.Black, Vector2.Zero, 0.8f
                    );
                }
            }

            // 鼠标悬停提示
            if (isMouseOver)
            {
                if (!items[index].IsAir)
                {
                    Main.hoverItemName = items[index].Name;
                    Main.HoverItem = items[index].Clone();
                }
                else
                {
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans" ? "请放入需要复制的物品" : "Place an item",
                        Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);
                }
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.02f);
            }
        }
    }
    public class VoidMirrorUI : UIState
    {
        private UIItemSlotVoidMirror slot;
        private UITextPanel<string> cloneButton;
        //private Vector2 TilePosition;
        private string hoverText;
        private Item[] items;
        //private VoidMirrorItem mirrorItem => items[0]?.modItem as VoidMirrorItem ?? null;
        private VoidMirrorItem mirrorItem;
        private bool isMouseOver = false;
        public static bool isClone = true;
        //public VoidMirrorUI(Item[] items)
        //{
        //    this.items = items;
        //}
        public VoidMirrorUI(VoidMirrorItem mirrorItem)
        {
            this.mirrorItem = mirrorItem;
            this.items = mirrorItem.items;
        }
        public override void OnInitialize()
        {
            // 初始化时不设置具体位置
            slot = new UIItemSlotVoidMirror(items, 0);
            slot.Width.Set(52f, 0f);
            slot.Height.Set(52f, 0f);
            Append(slot);

            cloneButton = new UITextPanel<string>(Language.ActiveCulture.Name == "zh-Hans" ? "复制" : "Clone", 1f);
            cloneButton.Width.Set(80f, 0f);
            cloneButton.Height.Set(32f, 0f);
            cloneButton.OnLeftClick += CloneButton_OnClick;
            cloneButton.OnMouseOver += CloneButton_OnMouseOver;
            cloneButton.OnMouseOut += CloneButton_OnMouseOut;
            Append(cloneButton);
        }

        public void SetTilePosition(Vector2 _)
        {
            // 获取玩家中心的屏幕坐标
            Player player = Main.LocalPlayer;
            Vector2 playerScreenPos = player.Center - Main.screenPosition;
            // 物品槽和按钮在玩家下方
            slot.Left.Set(playerScreenPos.X - slot.Width.Pixels / 2 - 152, 0f);
            slot.Top.Set(playerScreenPos.Y - 60, 0f);
            cloneButton.Left.Set(playerScreenPos.X - cloneButton.Width.Pixels / 2 - 152, 0f);
            cloneButton.Top.Set(playerScreenPos.Y, 0f);

            slot.Recalculate();
            cloneButton.Recalculate();
        }
        public bool CanClone()
        {
            if (mirrorItem == null) return false;
            return mirrorItem.lastCloneDay != VoidMirrorItem.GlobalDayCount;
        }
        private void CloneButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            if (items[0] != null && !items[0].IsAir && CanClone())
            {
                int itemType = items[0].type;
                if(itemType == ModContent.ItemType<TianjingSword>()
                    || itemType == ModContent.ItemType<TianjingSword_0>()
                    || itemType == ModContent.ItemType<TianjingSword_1>()
                    || itemType == ModContent.ItemType<TianjingSword_2>()
                    || itemType == ModContent.ItemType<TianjingSword_3>()
                    || itemType == ModContent.ItemType<TianjingSword_4>()
                    || itemType == ModContent.ItemType<TianjingSword_5>()
                    )
                {
                    itemType = ModContent.ItemType<TianjingSword_4>();
                }
                int stack = items[0].stack > 0 ? items[0].stack : 1;
                if (itemType == ItemID.TigerSkin && player.HasBuff(BuffID.StormTiger))
                {
                    itemType = ModContent.ItemType<TigerVigor>();
                    stack = 1;
                }
                Vector2 spawnPosition = VoidMirrorUISystem.tilePosition;
                int newItemIndex = Item.NewItem(
                    player.GetSource_Misc("VoidMirrorClone"),
                    spawnPosition + new Vector2(32, -120f),
                    items[0].width > 0 ? items[0].width : 32,
                    items[0].height > 0 ? items[0].height : 32,
                    itemType,
                    stack
                );
                if (newItemIndex >= 0 && newItemIndex < Main.maxItems)
                {
                    Main.item[newItemIndex].Prefix(items[0].prefix);
                }
                mirrorItem.lastCloneDay = VoidMirrorItem.GlobalDayCount;
                SoundEngine.PlaySound(SoundID.DD2_KoboldFlyerChargeScream);
                isClone = false;
            }
            else
            {
                // 冷却中提示
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        private void CloneButton_OnMouseOver(UIMouseEvent evt, UIElement listeningElement)
        {
            cloneButton.BorderColor = Color.Yellow * 0.8f;
            isMouseOver = true;
        }

        private void CloneButton_OnMouseOut(UIMouseEvent evt, UIElement listeningElement)
        {
            cloneButton.BorderColor = Color.Black;
            hoverText = null;
            isMouseOver = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (slot.isMouseOver || isMouseOver)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (mirrorItem != null && mirrorItem.lastCloneDay == VoidMirrorItem.GlobalDayCount)
            {
                hoverText = Language.ActiveCulture.Name == "zh-Hans"
                    ? "请明早4：30分再来……"
                    : "On cooldown, available next day";
            }
            else
            {
                hoverText = Language.ActiveCulture.Name == "zh-Hans"
                    ? ""
                    : "";
            }
            if (isMouseOver && !string.IsNullOrEmpty(hoverText))
            {
                //Main.LocalPlayer.mouseInterface = true;
                Vector2 mousePosition = Main.MouseScreen;
                Vector2 drawPosition = mousePosition + new Vector2(-8f, 32f);
                spriteBatch.DrawString(FontAssets.MouseText.Value, hoverText, drawPosition, Color.White);
            }
        }
    }

    public class VoidMirrorUISystem : ModSystem
    {
        private UserInterface voidMirrorInterface;
        internal VoidMirrorUI voidMirrorUI;
        internal static Vector2 tilePosition;
        private const float MaxDistance = 100f;
        private double lastTime = 0;
        public override void PostUpdateWorld()
        {
            // 检查是否新的一天（早上4:30，Main.time == 0 && !Main.dayTime）
            if (Main.dayTime && Main.time == 0 && lastTime != Main.time)
            {
                VoidMirrorItem.GlobalDayCount++;
                VoidMirrorUI.isClone = true;
            }
            lastTime = Main.time;
        }
        public override void Load()
        {
            if (!Main.dedServ)
            {
                var voidMirrorItem = new VoidMirrorItem();
                voidMirrorUI = new VoidMirrorUI(voidMirrorItem);
                voidMirrorInterface = new UserInterface();
                tilePosition = Vector2.Zero;
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (voidMirrorInterface?.CurrentState != null)
            {
                voidMirrorInterface.Update(gameTime);

                // 检查玩家与瓦片的距离
                Player player = Main.LocalPlayer;
                if (tilePosition != Vector2.Zero) // 确保 tilePosition 已被正确设置
                {
                    float distance = Vector2.Distance(player.Center, tilePosition);
                    if (distance > MaxDistance)
                    {
                        ToggleUI(); // 关闭UI面板
                    }
                }
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "SwordMastery: Void Mirror UI",
                    delegate
                    {
                        if (voidMirrorInterface?.CurrentState != null)
                        {
                            voidMirrorInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public void ToggleUI(Vector2? position = null)
        {
            // 打开或关闭UI面板
            if (voidMirrorInterface.CurrentState == null)
            {
                // 打开UI面板
                voidMirrorInterface.SetState(voidMirrorUI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen); // 播放打开音效
                // 设置瓦片位置
                if (position.HasValue)
                {
                    // 传入位置参数
                    tilePosition = position.Value;
                    // 设置UI面板位置
                    voidMirrorUI.SetTilePosition(tilePosition);
                }
            }
            else
            {
                // 关闭UI面板
                voidMirrorInterface.SetState(null);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose); // 播放关闭音效
            }
        }

        public bool IsUIVisible()
        {
            return voidMirrorInterface?.CurrentState != null;
        }
    }
}
