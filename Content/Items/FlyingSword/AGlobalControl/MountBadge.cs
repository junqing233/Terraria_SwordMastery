using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    // 1. 物品类
    public class MountBadge : ModItem
    {
        // 存储选中的武器类型
        public static int MaxItems = 1;
        public Item[] items = Enumerable.Range(0, MaxItems).Select(_ => new Item()).ToArray();
        public bool isClick = false;
        public static float ExtraDrawRotation = 0f; // 额外绘制旋转角度（单位：弧度）
        public static bool Dynamicsteer = false;// 动态转向
        public static bool Stablemode = false;// 稳定模式
        public static float adjustmentcoefficient = 0.02f;
        public static bool IsLight = false;
        public static bool IsClick = false;
        internal static MountBadgeWeaponSlotUI weaponSlotUI;
        public Guid InstanceId = Guid.NewGuid(); // 每个物品唯一

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
        }

        public override void LoadData(TagCompound tag)
        {
            if (items == null || items.Length != MaxItems)
                items = new Item[MaxItems];
            for (int i = 0; i < MaxItems; i++)
                items[i] = new Item();

            var loadedItems = tag.GetList<TagCompound>("items");
            var loadedTextures = tag.GetList<int>("textures");

            for (int i = 0; i < loadedItems.Count && i < items.Length; i++)
            {
                items[i] = ItemIO.Load(loadedItems[i]);
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

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.mountType = ModContent.MountType<CustomWeaponMount>();
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.value = 20000;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item100;
            Item.consumable = false; // 设置为不可消耗物品
        }

        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Geode, 1)
                .AddTile(TileID.Furnaces)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.Amethyst, 1)
                .AddIngredient(ItemID.Topaz, 1)
                .AddIngredient(ItemID.Sapphire, 1)
                .AddIngredient(ItemID.Emerald, 1)
                .AddIngredient(ItemID.Ruby, 1)
                .AddIngredient(ItemID.Diamond, 1)
                .AddIngredient(ItemID.StoneBlock, 6)
                .AddTile(TileID.Hellforge)
                .Register();
        }
        public static float FinalWeaponDamage = 0;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 只在有武器时显示
            if (items[0] != null && !items[0].IsAir)
            {
                var player = Main.myPlayer;
                int damage = Main.player[player].GetWeaponDamage(items[0]);
                FinalWeaponDamage = damage;
                int useTime = items[0].useTime;
                float knockback = items[0].knockBack;
                int crit = items[0].crit;

                // 计算速度和加速度（与坐骑一致）
                float baseSpeed = 6f;
                float baseAccel = 0.01f;
                float speed = baseSpeed;
                if (damage > 0)
                    speed += damage * 0.03f;
                speed += knockback * 0.08f;
                speed += crit * 0.04f;
                speed = Math.Clamp(speed, baseSpeed, 24f);

                // 计算加速度（使用时间越短加速度越大，区分更明显）
                float accel = baseAccel;
                if (useTime > 0)
                    accel += 3f / (useTime + 2f);
                accel = Math.Clamp(accel, baseAccel, 0.4f);
                if (Stablemode)
                {
                    speed /= 2;
                    accel = 1;
                }
                // 中文/英文支持
                string speedText = Language.ActiveCulture.Name == "zh-Hans"
                    ? $"Max速度：{speed:0.00}"
                    : $"Mount Speed: {speed:0.00}";
                string accelText = Language.ActiveCulture.Name == "zh-Hans"
                    ? $"加速度：{accel:0.00}"
                    : $"Mount Acceleration: {accel:0.00}";

                tooltips.Add(new TooltipLine(Mod, "MountSpeed", speedText));
                tooltips.Add(new TooltipLine(Mod, "MountAccel", accelText));
            }
            if ((!Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) &&
                !Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift)))
            {
                string speedText = Language.ActiveCulture.Name == "zh-Hans"
                    ? "按住Shift键然后……"
                    : "Hold down the Shift key and ......";
                tooltips.Add(new TooltipLine(Mod, "MountSpeed", speedText));
            }
            else
            {
                string speedText = Language.ActiveCulture.Name == "zh-Hans"
                    ? "左键点击打开徽章"
                    : "Left-click to open the badge";
                tooltips.Add(new TooltipLine(Mod, "MountSpeed", speedText));
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new MountBadgeWeaponSlotUI();
            }
        }
        //public override void UpdateAccessory(Player player, bool hideVisual)
        //{
        //    base.UpdateAccessory(player, hideVisual);
        //}
        public override void UpdateInventory(Player player)
        {
            base.UpdateInventory(player);
            if (Main.playerInventory &&
                Main.mouseLeft &&
                Main.mouseLeftRelease &&
                Main.HoverItem.type == Item.type &&
                Main.netMode != NetmodeID.Server &&
                (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                )
            {
                // 阻止物品被拿起
                Main.mouseLeftRelease = false;
                if (Main.netMode != NetmodeID.Server)
                {
                    IsClick = !IsClick;
                    if (IsClick)
                    {
                        ModContent.GetInstance<MountBadgeUISystem>().ShowWeaponSlotUI(this);
                        SoundEngine.PlaySound(SoundID.MenuOpen); // 播放打开音效
                    }
                    else
                    {
                        ModContent.GetInstance<MountBadgeUISystem>().HideWeaponSlotUI();
                        SoundEngine.PlaySound(SoundID.MenuClose); // 播放关闭音效
                    }
                }
            }
        }

        // 没有武器时无法使用
        public override bool CanUseItem(Player player)
        {
            return items[0] != null && !items[0].IsAir;
        }
        private Texture2D tex = null;
        private Texture2D weaponTex = null;
        // 物品图标右下角绘制
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            tex = TextureAssets.Item[Type].Value;
            spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

            if (items[0] != null && !items[0].IsAir)
            {
                weaponTex = TextureAssets.Item[items[0].type].Value;
                Rectangle weaponFrame = Main.itemAnimations[items[0].type]?.GetFrame(weaponTex) ?? weaponTex.Frame();
                Vector2 offset = new Vector2(10, 10) * scale;
                spriteBatch.Draw(weaponTex, position + offset, weaponFrame, Color.White, 0f, origin, scale * 0.5f, SpriteEffects.None, 0f);
            }
            return false;
        }
       
        public override bool? UseItem(Player player)
        {
            if (items[0] != null && !items[0].IsAir)
            {
                // 可将武器参数传递给坐骑
                player.GetModPlayer<MountBadgePlayer>().SetMountParams(items[0]);
                // 存储武器类型到玩家
                player.GetModPlayer<MountBadgePlayer>().LastSummonWeaponType = items[0].type;
                return true;
            }
            return false;
        }
    }
    // 坐骑参数同步
    public class MountBadgePlayer : ModPlayer
    {
        public int LastSummonWeaponType = 0;
        public int weaponDamage;
        public int weaponUseTime;
        public float weaponKnockback;
        public int weaponCrit;
       
        public void SetMountParams(Item weapon)
        {
            weaponDamage = (int)MountBadge.FinalWeaponDamage;
            weaponUseTime = weapon.useTime;
            weaponKnockback = weapon.knockBack;
            weaponCrit = weapon.crit;
        }
    }
    // 自定义坐骑
    public class CustomWeaponMount : ModMount
    {
        private List<Vector2> tailPositions = new List<Vector2>();
        private const int TailLength = 12; // 拖尾长度，可调
        private float rotationOff = 0f;
        private int delay = 0;
       
        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<BuffsMountBadge>();
            MountData.acceleration = 0.01f; // 坐骑加速的速率。
            MountData.heightBoost = 0; // 坐骑与地面之间的高度。
            MountData.runSpeed = 6f; // 坐骑速度。
            MountData.dashSpeed = 6f; // 坐骑在冲刺状态下的移动速度。
            MountData.flightTimeMax = 0; // 坐骑能在飞行状态下持续的时间（帧数）。

            // 其他
            MountData.fatigueMax = 0;// 坐骑疲劳值上限。
            // 特效
            MountData.spawnDust = DustID.WhiteTorch; // 骑上或下坐骑时产生的尘埃ID。

            // 帧数据和玩家偏移
            MountData.totalFrames = 1; // 坐骑的动画帧总数
            MountData.playerYOffsets = Enumerable.Repeat(20, MountData.totalFrames).ToArray();// 玩家的Y轴偏移
            MountData.xOffset = 0;// 坐骑的X轴偏移
            MountData.yOffset = -1;// 坐骑的Y轴偏移
           
            // 站立
            MountData.standingFrameCount = 0;
            MountData.standingFrameDelay = 0;
            MountData.standingFrameStart = 0;
            // 跑动
            MountData.runningFrameCount = 0;
            MountData.runningFrameDelay = 0;
            MountData.runningFrameStart = 0;
            // 飞行
            MountData.flyingFrameCount = 0;
            MountData.flyingFrameDelay = 0;
            MountData.flyingFrameStart = 0;
            // 空中
            MountData.inAirFrameCount = 0;
            MountData.inAirFrameDelay = 0;
            MountData.inAirFrameStart = 0;
            // 静止
            MountData.idleFrameCount = 0;
            MountData.idleFrameDelay = 0;
            MountData.idleFrameStart = 0;
            MountData.idleFrameLoop = true;
            // 游泳
            MountData.swimFrameCount = 0;
            MountData.swimFrameDelay = 0;
            MountData.swimFrameStart = 0;

            if (!Main.dedServ)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }

        public override void UpdateEffects(Player player)
        {
            // 获取武器属性
            var badgePlayer = player.GetModPlayer<MountBadgePlayer>();
            int damage = badgePlayer.weaponDamage;
            int useTime = badgePlayer.weaponUseTime;
            float knockback = badgePlayer.weaponKnockback;
            int crit = badgePlayer.weaponCrit;
           
            // 基础值
            float baseSpeed = 6f;
            float baseAccel = 0.01f;

            // 计算移速（伤害、击退、暴击加成）
            float speed = baseSpeed;
            if (damage > 0)
                speed += damage * 0.03f; // 伤害加成（可调）
            speed += knockback * 0.08f; // 击退加成（可调）
            speed += crit * 0.04f;      // 暴击加成（可调）
            speed = Math.Clamp(speed, baseSpeed, 24f); // 限制最大速度

            // 计算加速度（使用时间越短加速度越大）
            float accel = baseAccel;
            if (useTime > 0)
                accel += 3f / (useTime + 2f);
            accel = Math.Clamp(accel, baseAccel, 0.4f);
                  
            // 应用到 MountData
            MountData.runSpeed = speed;
            MountData.dashSpeed = speed;
            MountData.acceleration = accel;

            // 稳定模式处理
            if (MountBadge.Stablemode)
            {
                MountData.runSpeed /= 2f;      // 最大速度减半
                if (player.controlRight)
                    player.velocity.X = MountData.runSpeed;
                else if (player.controlLeft)
                    player.velocity.X = -MountData.runSpeed;
                else
                    player.velocity.X = 0;
            }

            float 下落最高速度 = MountData.runSpeed;
            float 向下加速度 = MountData.acceleration * 2.5f;
            float 上升最高速度 = MountData.runSpeed;
            float 向上加速度 = MountData.acceleration * 2.5f;
            player.gravity = 0;
            player.maxFallSpeed = 下落最高速度;

            if (player.controlDown && player.velocity.Y <= 下落最高速度)
            {
                if (MountBadge.Stablemode)
                {
                    // 只有在速度较低时才赋值，且用适合穿平台的速度
                    if (player.velocity.Y > 0.0001f || (!(player.controlUp || player.controlJump) && player.velocity.Y < 0))
                        player.velocity.Y = 下落最高速度; // 6f是平台穿透的推荐速度
                    else
                        player.velocity.Y += 向下加速度;
                }
                else
                {
                    player.velocity.Y += 向下加速度;
                }
            }
            else if ((player.controlUp || player.controlJump) && player.velocity.Y >= -上升最高速度)
            {
                if (MountBadge.Stablemode)
                    player.velocity.Y = -上升最高速度;
                else
                    player.velocity.Y -= 向上加速度;
            }
            else if (player.velocity.Y > 1.5) player.velocity.Y--;
            else if (player.velocity.Y < -1.5) player.velocity.Y++;
            else
            {
                player.velocity.Y = -0.0001f;
            }
            player.fallStart = (int)(player.position.Y / 16f);

            // 让玩家朝前进方向旋转，并且平滑过渡
            float targetRotation = 0f;
            float maxSpeed = MountData.runSpeed; // 坐骑最大速度
            float offsetAngle = MathHelper.ToRadians(10f); // 微微旋转的角度（可调整）
            
            if (player.velocity.Length() > 0.1f)
            {
                if(MountBadge.Dynamicsteer)
                {
                    targetRotation = (float)Math.Atan2(player.velocity.Y, player.velocity.X);
                    if (player.velocity.X < 0 || (player.direction == -1 && player.velocity.X == 0))
                        targetRotation += MathF.PI;
                }
                
                // 判断水平速度达到最大速度的3/5
                if (Math.Abs(player.velocity.X) >= maxSpeed * 0.6f)
                {
                    if (player.velocity.X > 0)
                        targetRotation += offsetAngle; // 向右顺时针
                    else if (player.velocity.X < 0)
                        targetRotation -= offsetAngle; // 向左逆时针
                }
            }

            player.fullRotation = LerpAngle(player.fullRotation, targetRotation, MountBadge.adjustmentcoefficient);
            player.fullRotationOrigin = player.Size;
            rotationOff = player.fullRotation;

            // 记录轨迹点
            tailPositions.Add(player.Center);
            if (tailPositions.Count > TailLength)
                tailPositions.RemoveAt(0);
            
            var weaponType = badgePlayer.LastSummonWeaponType;
            if ((weaponType <= 0 || weaponType >= TextureAssets.Item.Length || !TextureAssets.Item[weaponType].IsLoaded)
                && delay == 0)
            {
                delay = 1;
                string[] zhTips = new string[]
                    {
                    "御气！我就知道能行！",
                    "嘿！朋友，放点什么进去吧……",
                    "空空如也，快来点装备！",
                    "没有武器，怎么御剑？",
                    "你是不是忘了什么？"
                    };
                string[] enTips = new string[]
                {
                    "Qi control! I knew it would work!",
                    "Hey! Friend, put something in...",
                    "Empty! Try adding some gear!",
                    "No weapon, no flying sword!",
                    "Did you forget something?"
                };

                string tip;
                if (Language.ActiveCulture.Name == "zh-Hans")
                    tip = zhTips[Main.rand.Next(zhTips.Length)];
                else
                    tip = enTips[Main.rand.Next(enTips.Length)];

                CombatText.NewText(
                    new Rectangle((int)player.position.X, (int)player.position.Y - 20, player.width, player.height),
                    new Color(200, 250, 250),
                    tip,
                    true,
                    false
                );
            }
        }
        public override void SetMount(Player player, ref bool skipDust)
        {
            base.SetMount(player, ref skipDust);
            int weaponType = (int)player.GetModPlayer<MountBadgePlayer>().LastSummonWeaponType; ;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                cachedAverageColor = GetTextureAverageColor(texture_);
                averageColorCalculated = true;
            }
        }
        public override void Dismount(Player player, ref bool skipDust)
        {
            base.Dismount(player, ref skipDust);
            tailPositions.Clear();
            delay = 0;
        }
        public static float LerpAngle(float from, float to, float t)
        {
            float difference = ((to - from + MathF.PI) % (MathF.PI * 2)) - MathF.PI;
            return from + difference * t;
        }
        public override bool Draw(
                List<DrawData> playerDrawData,
                int drawType,
                Player drawPlayer,
                ref Texture2D texture,
                ref Texture2D glowTexture,
                ref Vector2 drawPosition,
                ref Rectangle frame,
                ref Color drawColor,
                ref Color glowColor,
                ref float rotation,
                ref SpriteEffects spriteEffects,
                ref Vector2 drawOrigin,
                ref float drawScale,
                float shadow)
        {
            int weaponType = drawPlayer.GetModPlayer<MountBadgePlayer>().LastSummonWeaponType;

            if (weaponType > 0 && weaponType < TextureAssets.Item.Length && TextureAssets.Item[weaponType].IsLoaded)
            {
                texture = TextureAssets.Item[weaponType].Value;
                frame = Main.itemAnimations[weaponType]?.GetFrame(texture) ?? texture.Frame();
                drawOrigin = frame.Size() / 2f;
                drawScale = 1f; // 可根据需要调整缩放
                rotation = drawPlayer.direction == -1 ? MathHelper.ToRadians(-50f) - MountBadge.ExtraDrawRotation : MathHelper.ToRadians(50f) + MountBadge.ExtraDrawRotation;
                spriteEffects = drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                glowTexture = null;
                glowColor = Color.Transparent;
                
                drawColor = MountBadge.IsLight ? Color.White : drawColor;

                if (MountBadge.IsLight)
                    // 拖尾绘制
                    for (int i = 1; i < tailPositions.Count; i++)
                    {
                        Color LightsColor = cachedAverageColor;
                        var value = (float)Math.Max((Math.Cos(Main.timeForVisualEffects * 0.04 + 2) * 0.3f + 0.4f), 0.4f);
                        var v3 = Main.rgbToHsl(LightsColor);
                        v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + 2) * 0.5f + 1f) * 0.1f;
                        var c = Main.hslToRgb(v3);
                        c.A = 0;
                        float factor = (float)i / tailPositions.Count;
                        Color tailColor = c * value * 0.4f * factor;
                        Vector2 pos = tailPositions[i] - Main.screenPosition;
                        Main.EntitySpriteDraw(
                            texture,
                            pos,
                            frame,
                            tailColor,
                            rotation + rotationOff,           // 适配玩家旋转
                            drawOrigin,     // 适配玩家原点
                            drawScale * (0.8f + 0.2f * factor),
                            spriteEffects,
                            0
                        );
                    }
            }
            // 返回 true 让 tModLoader继续绘制（此时已替换为武器贴图）
            return true;
        }
        private Color cachedAverageColor = Color.White;
        private bool averageColorCalculated = false;

        private Color GetTextureAverageColor(Texture2D texture)
        {
            if (averageColorCalculated) return cachedAverageColor;
            if (texture == null) return Color.White;
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            int r = 0, g = 0, b = 0, count = 0;
            foreach (var c in data)
            {
                if (c.A > 32)
                {
                    r += c.R; g += c.G; b += c.B; count++;
                }
            }
            if (count == 0) return Color.White;
            cachedAverageColor = new Color(r / count, g / count, b / count);
            averageColorCalculated = true;
            return cachedAverageColor;
        }
        //private Color GetTextureAverageColor(Texture2D texture)
        //{
        //    if (texture == null) return Color.White;
        //    Color[] data = new Color[texture.Width * texture.Height];
        //    texture.GetData(data);
        //    int r = 0, g = 0, b = 0, count = 0;
        //    foreach (var c in data)
        //    {
        //        if (c.A > 32) // 忽略透明像素
        //        {
        //            r += c.R; g += c.G; b += c.B; count++;
        //        }
        //    }
        //    if (count == 0) return Color.White;
        //    return new Color(r / count, g / count, b / count);
        //}
    }
    public class MountBadgeProjGlobal : GlobalProjectile
    {
        public int OnHitNPCTypeId;
        public override bool InstancePerEntity => true;
    }
    // 2. UI系统
    public class MountBadgeUISystem : ModSystem
    {
        internal static UserInterface weaponSlotInterface;
        internal static MountBadgeWeaponSlotUI weaponSlotUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new MountBadgeWeaponSlotUI();
                weaponSlotInterface = new UserInterface();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            //if (weaponSlotUI.Visible)
                weaponSlotInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "MountBadge: WeaponSlotUI",
                    () =>
                    {
                        if (/*weaponSlotUI.Visible && */weaponSlotInterface?.CurrentState != null)
                            weaponSlotInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public void ShowWeaponSlotUI(MountBadge item)
        {
            weaponSlotUI.SetItem(item);
            weaponSlotInterface.SetState(weaponSlotUI);
            weaponSlotUI.Visible = true;
        }
        public void HideWeaponSlotUI()
        {
            weaponSlotInterface.SetState(null);
            weaponSlotUI.Visible = false;
        }
    }

    // 3. UIState实现
    public class MountBadgeWeaponSlotUI : UIState
    {
        
        private UIItemSlotMountBadge slot;
        private MountBadge mountBadge;
        public bool Visible = false;
        public static float ItemID = 0;
        private UITextButtonMount rotateButton;
        private UITextButtonMount steerButton;
        private UITextButtonMount StableButton;
        private UITextButtonMount LightButton;

        public override void OnInitialize()
        {
            // 物品槽
            slot = new UIItemSlotMountBadge(mountBadge?.items, 0);
            slot.Left.Set(445, 0f);
            slot.Top.Set(320, 0f);
            Append(slot);


            // 旋转按钮
            rotateButton = new UITextButtonMount(
                Language.ActiveCulture.Name == "zh-Hans" ? "旋转坐骑" : "Rotate the mount",
                () => {
                    MountBadge.ExtraDrawRotation += MathHelper.ToRadians(45f);
                    if (MountBadge.ExtraDrawRotation > MathHelper.TwoPi)
                        MountBadge.ExtraDrawRotation -= MathHelper.TwoPi;
                },
                () => {
                    MountBadge.ExtraDrawRotation -= MathHelper.ToRadians(45f);
                    if (MountBadge.ExtraDrawRotation < -MathHelper.TwoPi)
                        MountBadge.ExtraDrawRotation += MathHelper.TwoPi;
                }
            );
            rotateButton.Left.Set(431, 0f);
            rotateButton.Top.Set(380, 0f);
            Append(rotateButton);


            //动态转向按钮
            steerButton = new UITextButtonMount(
               "",
               () => {
                   MountBadge.Dynamicsteer = !MountBadge.Dynamicsteer;
               },
               () => {
                   MountBadge.adjustmentcoefficient += 0.01f;
                   if (MountBadge.adjustmentcoefficient >= 0.06)
                       MountBadge.adjustmentcoefficient = 0.01f;
               }
           );
            steerButton.Left.Set(431, 0f);
            steerButton.Top.Set(420, 0f);
            Append(steerButton);

            //稳定模式按钮
            StableButton = new UITextButtonMount(
               "",
               () => {
                   MountBadge.Stablemode = !MountBadge.Stablemode;
               }
           );
            StableButton.Left.Set(431, 0f);
            StableButton.Top.Set(460, 0f);
            Append(StableButton);

            //发光按钮
            LightButton = new UITextButtonMount(
               "",
               () => {
                   MountBadge.IsLight = !MountBadge.IsLight;
               }
           );
            LightButton.Left.Set(431, 0f);
            LightButton.Top.Set(500, 0f);
            Append(LightButton);
        }
       
        public void SetItem(MountBadge mountBadge)
        {
            this.mountBadge = mountBadge;
            if(slot != null)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            // 只有鼠标移入旋转按钮时才绘制提示
            if (rotateButton.isMouseOver)
            {
                var rect_ = rotateButton.GetDimensions().ToRectangle();
                string tip = Language.ActiveCulture.Name == "zh-Hans" ? $"旋转:{MathHelper.ToDegrees(MountBadge.ExtraDrawRotation):0}°" : $"Revolve:{MathHelper.ToDegrees(MountBadge.ExtraDrawRotation):0}°";
                // 右移10像素
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rect_.Center.X - 31, rect_.Center.Y - 8, Color.MediumPurple, Color.Black, Vector2.Zero, 0.8f);
            }
            //动态转向按钮
            var rectIgnore = steerButton.GetDimensions().ToRectangle();
            if (steerButton.isMouseOver)
            {
                string tip = MountBadge.Dynamicsteer
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Enabled")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Disabled");
                Color color_ = MountBadge.Dynamicsteer
                    ? Color.LightSkyBlue : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rectIgnore.Center.X - 24, rectIgnore.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            
                if(MountBadge.Dynamicsteer)
                {
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                           Language.ActiveCulture.Name == "zh-Hans" ? "右键点击调节转向系数" : "Right-click on the adjustment factor",
                           Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);

                    Utils.DrawBorderStringFourWay(
                        spriteBatch,
                        FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans"
                            ? $"当前系数：{(MountBadge.adjustmentcoefficient * 10):0.0}"
                            : $"Current coefficient: {(MountBadge.adjustmentcoefficient * 10):0.0}",
                        Main.MouseScreen.X + 26,
                        Main.MouseScreen.Y + 16,
                        Color.White,
                        Color.Black,
                        Vector2.Zero,
                        0.8f
                    );
                }
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "动态转向" : "Dynamic steering", rectIgnore.Center.X - 30, rectIgnore.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
            //稳定模式按钮
            var rectIgnore_ = StableButton.GetDimensions().ToRectangle();
            if (StableButton.isMouseOver)
            {
                string tip = MountBadge.Stablemode
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Enabled")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Disabled");
                Color color_ = MountBadge.Stablemode
                    ? Color.LightSkyBlue : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rectIgnore_.Center.X - 24, rectIgnore_.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
                
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                           Language.ActiveCulture.Name == "zh-Hans" ? "牺牲速度带来稳定" : "Sacrifice speed for stability",
                           Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "稳定模式" : "Stable mode", rectIgnore_.Center.X - 30, rectIgnore_.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
            //发光按钮
            var rectIgnore__ = LightButton.GetDimensions().ToRectangle();
            if (LightButton.isMouseOver)
            {
                string tip = MountBadge.IsLight
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Enabled")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Disabled");
                Color color_ = MountBadge.IsLight
                    ? Color.LightSkyBlue : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rectIgnore__.Center.X - 24, rectIgnore__.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "光亮和拖尾" : "Bright and tailed", rectIgnore__.Center.X - 39, rectIgnore__.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mountBadge != null)
            {
                // 只允许远程武器
                if (mountBadge.items[0] != null && !mountBadge.items[0].IsAir && mountBadge.items[0].damage > 0
                   && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Ranged) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Melee) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MeleeNoSpeed) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Magic) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MagicSummonHybrid) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Throwing)))
                {
                    ItemID = mountBadge.items[0].type;
                }
                else
                {
                    ItemID = 0;
                }
            }
            if (slot.isMouseOver)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            if (!Main.playerInventory)
            {
                ModContent.GetInstance<MountBadgeUISystem>().HideWeaponSlotUI();
                MountBadge.IsClick = !MountBadge.IsClick;
            }
        }
    }
    public class UITextButtonMount : UIElement
    {

        private string text;
        private Action onClick;
        private Action onRightClick;
        public bool isMouseOver = false;


        public UITextButtonMount(string text, Action onClick, Action onRightClick = null)
        {
            this.text = text;
            this.onClick = onClick;
            this.onRightClick = onRightClick;
            Width.Set(80, 0f);
            Height.Set(32, 0f);
        }
       
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var rect = GetDimensions().ToRectangle();
            spriteBatch.Draw(TextureAssets.InventoryBack9.Value, rect, Color.White * 0.8f);

            // 仅当不是“旋转武器”按钮，或鼠标未移入时才绘制按钮文字
            if (text != "旋转坐骑" && text != "Rotate the mount" || !isMouseOver)
            {
                int offsetX = -38;
                if (Language.ActiveCulture.Name == "zh-Hans" ? text == "旋转坐骑" : text == "Rotate the mount")
                    offsetX += 7;

                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    text,
                    rect.Center.X + offsetX,
                    rect.Center.Y - 8,
                    Color.White,
                    Color.Black,
                    Vector2.Zero,
                    0.8f
                );
            }
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
            onClick?.Invoke();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            onRightClick?.Invoke();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void Update(GameTime gameTime)
        {
            if (isMouseOver)
                Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
            base.Update(gameTime);
        }
    }
    // UIElement
    public class UIItemSlotMountBadge : UIElement
    {
        private Item[] items;
        private int index;
        public bool isMouseOver = false;

        public UIItemSlotMountBadge(Item[] items, int index)
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
            else if (items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
               && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Ranged) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Melee) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MeleeNoSpeed) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Magic) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MagicSummonHybrid) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Throwing)
                )
                && !player.ItemAnimationActive
                
                )
            {
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 交换物品
            else if (!items[index].IsAir && !Main.mouseItem.IsAir
                 && Main.mouseItem.damage > 0
                && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Ranged) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Melee) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MeleeNoSpeed) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Magic) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.MagicSummonHybrid) ||
                Main.mouseItem.DamageType.CountsAsClass(DamageClass.Throwing)
                )
                 && !player.ItemAnimationActive
                )
            {
                Item temp = items[index].Clone();
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem = temp;
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureAssets.InventoryBack9.Value, GetDimensions().ToRectangle(), Color.White * 0.72f);
            if (items[index] != null && !items[index].IsAir)
            {
                Texture2D tex = TextureAssets.Item[items[index].type].Value;
                Rectangle frame = Main.itemAnimations[items[index].type]?.GetFrame(tex) ?? tex.Frame();
                float scale = Math.Min(1f, 30f / (frame.Width + frame.Height) * 2);
                var drawPosition = GetDimensions().Position() + new Vector2(25f) - frame.Size() * 0.5f * scale;
                spriteBatch.Draw(tex, drawPosition, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                
                if (items[index].maxStack > 1 && items[index].stack > 1)
                {
                    Utils.DrawBorderStringFourWay(
                        spriteBatch,
                        FontAssets.ItemStack.Value,
                        items[index].stack.ToString(),
                        drawPosition.X - 5, // 位置可微调
                        drawPosition.Y + 15,
                        Color.White,
                        Color.Black,
                        Vector2.Zero,
                        0.8f
                    );
                }
            }
            if (isMouseOver) // 如果鼠标移入物品槽，绘制一个半透明的覆盖层来防止点击
            {
                if (!items[index].IsAir)
                {
                    Main.hoverItemName = items[index].Name;
                    Main.HoverItem = items[index].Clone();
                }
                else
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                       Language.ActiveCulture.Name == "zh-Hans" ? "可放入近战类、远程类、魔法类武器": "Melee, Ranged, Magic weapons accepted", 
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.02f);
            }
        }
    }
   
    // 6. Buff类
    class BuffsMountBadge : ModBuff
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/AGlobalControl/BuffsFlyingControl";
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.mount != null && player.mount.Type != ModContent.MountType<CustomWeaponMount>())// 如果玩家已经坐上了坐骑，并且不是FirstMount
            {
                // 确保玩家的坐骑是FirstMount
                player.mount.SetMount(ModContent.MountType<CustomWeaponMount>(), player);
            }

            player.buffTime[buffIndex] = 30; // 设定缓冲时间，可根据需要调整
        }
        public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
        {
            base.PostDraw(spriteBatch, buffIndex, drawParams);
            Player player = Main.LocalPlayer;
            int weaponType = player.GetModPlayer<MountBadgePlayer>().LastSummonWeaponType;
            if (weaponType > 0 && weaponType < TextureAssets.Item.Length && TextureAssets.Item[weaponType].IsLoaded)
            {
                Texture2D weaponTex = TextureAssets.Item[weaponType].Value;
                Rectangle weaponFrame = Main.itemAnimations[weaponType]?.GetFrame(weaponTex) ?? weaponTex.Frame();
                float maxSize = 20f;
                float scale = 1f;
                if (weaponFrame.Width > maxSize || weaponFrame.Height > maxSize)
                    scale = maxSize / Math.Max(weaponFrame.Width, weaponFrame.Height);

                Vector2 center = drawParams.Position + new Vector2(16, 16);

                Rectangle buffRect = new Rectangle(
                    (int)drawParams.Position.X,
                    (int)drawParams.Position.Y,
                    32, 32
                );
                bool mouseInBuff = buffRect.Contains(Main.mouseX, Main.mouseY);

                Color drawColor = mouseInBuff ? Color.White * 1f : Color.White * 0.5f;

                spriteBatch.Draw(
                    weaponTex,
                    center,
                    weaponFrame,
                    drawColor,
                    0f,
                    weaponFrame.Size() / 2,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}