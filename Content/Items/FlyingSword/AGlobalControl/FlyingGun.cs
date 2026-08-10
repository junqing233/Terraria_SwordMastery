using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Prefixes;
using System;
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
    public class FlyingGun : ModItem
    {
        // 存储选中的武器类型
        public static int MaxItems = 1;
        public Item[] items = Enumerable.Range(0, MaxItems).Select(_ => new Item()).ToArray();
        public static bool UseAttackAI = true; // 默认开启攻击AI
        public static int recoilStrength = 5;
        public static bool IgnoreTilesForTargeting = false; // 是否索敌穿墙
        public static bool IsTail = false; // 是否拖尾
        public static float AttackSpread = MathHelper.ToRadians(180f); // 默认180度
        public bool isClick = false;
        public static bool IsClick = false;
        internal static FlyingGunWeaponSlotUI weaponSlotUI;
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

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.mana = 2;
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = 20000;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingGunProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingGun>();
            Item.DamageType = DamageClass.Summon;
        }
        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.AmmoBox, 1)
                .AddIngredient(ModContent.ItemType<Quiver>(), 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        private float FinalWeaponDamage = 0;
        private float FinalWeaponKnockback = 0;
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (items[0] != null && !items[0].IsAir && items[0].damage > 0
                && items[0].DamageType.CountsAsClass(DamageClass.Ranged))
            {
                int useTime = items[0].useTime;
                int minUseTime = 10;
                int maxUseTime = 40;
                float minScale = 0.42f; // 最短时间最小倍率
                float maxScale = 0.62f; // 最长时间最大倍率
                float scale;
                // 获取武器最终伤害（已包含饰品等加成）
                float finalWeaponDamage = player.GetWeaponDamage(items[0]);
                FinalWeaponDamage = finalWeaponDamage;
                // 读取配置并调整伤害
                var config = ModContent.GetInstance<SwordMasteryConfig>();
                if (config.StrengthExperience == StrengthMode.Ordinary)
                {
                    FinalWeaponDamage = (int)(FinalWeaponDamage * 0.6f);
                }
                if (useTime <= minUseTime)
                    scale = minScale;
                else if (useTime >= maxUseTime)
                    scale = maxScale;
                else
                    scale = minScale + (maxScale - minScale) * ((float)(useTime - minUseTime) / (maxUseTime - minUseTime));
                damage *= scale * finalWeaponDamage / Item.damage;
            }
            else
            {
                damage *= 0f;
            }
        }
        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            if (items[0] != null && !items[0].IsAir && items[0].knockBack > 0
                && items[0].DamageType.CountsAsClass(DamageClass.Ranged))
            {
                FinalWeaponKnockback = player.GetWeaponKnockback(items[0]);
                // 近似一半（注意：StatModifier只能乘法，不能直接赋值）
                knockback *= 0.5f * FinalWeaponKnockback / Item.knockBack;
            }
            else
            {
                knockback *= 0f;
            }
        }
        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            // 伤害行处理
            var damageLine = tooltips.FirstOrDefault(t => t.Name == "Damage" && t.Mod == "Terraria");
            if (damageLine != null)
            {
                int showDamage = 0;
                if (items[0] != null && !items[0].IsAir && items[0].damage > 0
                    && items[0].DamageType.CountsAsClass(DamageClass.Ranged))
                {
                    int useTime = items[0].useTime;
                    int minUseTime = 10;
                    int maxUseTime = 40;
                    float minScale = 0.42f;
                    float maxScale = 0.62f;
                    float scale;
                    if (useTime <= minUseTime)
                        scale = minScale;
                    else if (useTime >= maxUseTime)
                        scale = maxScale;
                    else
                        scale = minScale + (maxScale - minScale) * ((float)(useTime - minUseTime) / (maxUseTime - minUseTime));
                    showDamage = (int)(FinalWeaponDamage * scale);
                }
                string[] split = damageLine.Text.Split(' ');
                string suffix = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "伤害";
                damageLine.Text = $"{showDamage} {suffix}";
            }

            // 击退行处理
            var knockbackLine = tooltips.FirstOrDefault(t => t.Name == "Knockback" && t.Mod == "Terraria");
            if (knockbackLine != null)
            {
                float showKnockback = 0f;
                if (items[0] != null && !items[0].IsAir && items[0].knockBack > 0
                    && items[0].DamageType.CountsAsClass(DamageClass.Ranged))
                {
                    showKnockback = FinalWeaponKnockback * 0.2f;
                }
                // 获取原始击退描述的后缀（如“击退”）
                string[] split = knockbackLine.Text.Split(' ');
                string suffix = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "击退";
                knockbackLine.Text = $"{showKnockback:0.##} {suffix}";
                // 在击退行后插入“发射攻速”
                int insertIndex = tooltips.IndexOf(knockbackLine) + 1;
                int useTime = items[0] != null && !items[0].IsAir ? items[0].useTime : 0;
                double seconds = useTime / 60.0;
                var speedLine = new TooltipLine(Mod, "ShootSpeed", Language.ActiveCulture.Name == "zh-Hans" ? $"射滞: {seconds:0.00}秒": $"Shoot Lull: {seconds:0.00} s");
                tooltips.Insert(insertIndex, speedLine);
                // 是否消耗弹药
                bool consumeAmmo = items[0] != null && !items[0].IsAir && items[0].useAmmo > 0;
                var ammoLine = new TooltipLine(Mod, "ConsumeAmmo",
                    Language.ActiveCulture.Name == "zh-Hans"
                        ? $"消耗弹药: {(consumeAmmo ? "是" : "否")}"
                        : $"Consume Ammo: {(consumeAmmo ? "Yes" : "No")}");
                tooltips.Insert(insertIndex + 1, ammoLine);
            }
            // 物品名后拼接物品槽武器名
            var nameLine = tooltips.FirstOrDefault(t => t.Name == "ItemName" && t.Mod == "Terraria");
            if (nameLine != null && items[0] != null && !items[0].IsAir)
            {
                nameLine.Text += $"({items[0].Name})";
            }else
            {
                nameLine.Text += $"(空)";
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new FlyingGunWeaponSlotUI();
            }
        }
        public override bool CanRightClick()
        {
            if (Main.mouseRight && !isClick)
            {
                if (Main.mouseRightRelease)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        IsClick = !IsClick;
                        if (IsClick)
                        {
                            ModContent.GetInstance<FlyingGunUISystem>().ShowWeaponSlotUI(this);
                            SoundEngine.PlaySound(SoundID.MenuOpen); // 播放打开音效
                        }
                        else
                        {
                            ModContent.GetInstance<FlyingGunUISystem>().HideWeaponSlotUI();
                            SoundEngine.PlaySound(SoundID.MenuClose); // 播放关闭音效
                        }
                    }
                }
            }
            isClick = Main.mouseRightRelease;
            return false;
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

        // 发射弹幕时传递武器类型
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingGun>(), 3600);
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].originalDamage = damage;
                // 传递唯一标识符（用 ai[1] 或 localAI[0]，或 ModProjectile 字段）
                Main.projectile[proj].ai[1] = items[0].type; // Guid转int
                Main.projectile[proj].localAI[1] = items[0].shoot;
                Main.projectile[proj].localAI[0] = items[0].shootSpeed;
                Main.projectile[proj].localAI[2] = items[0].useAmmo;
                Main.projectile[proj].ai[0] = items[0].useAnimation;
                Main.projectile[proj].ai[2] = items[0].maxStack;
                // 存储武器类型到玩家
                player.GetModPlayer<FlyingGunPlayer>().LastSummonWeaponType = items[0].type;
                // 在Shoot方法里
                player.GetModPlayer<FlyingGunPlayer>().LastSummonWeaponPrefix = items[0].prefix; // 传递前缀ID
                // 在 Shoot 方法里
                Main.projectile[proj].GetGlobalProjectile<FlyingSwordProjGlobal>().OnHitNPCTypeId = items[0].type;
            }
            return false;
        }
    }
    public class FlyingGunProjGlobal : GlobalProjectile
    {
        public int OnHitNPCTypeId;
        public override bool InstancePerEntity => true;
    }
    // 2. UI系统
    public class FlyingGunUISystem : ModSystem
    {
        internal static UserInterface weaponSlotInterface;
        internal static FlyingGunWeaponSlotUI weaponSlotUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new FlyingGunWeaponSlotUI();
                weaponSlotInterface = new UserInterface();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            //if (weaponSlotUI.Visible)
                weaponSlotInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "FlyingGun: WeaponSlotUI",
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

        public void ShowWeaponSlotUI(FlyingGun item)
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
    public class FlyingGunWeaponSlotUI : UIState
    {
        
        private UIItemSlotFlyingGun slot;
        private FlyingGun flyingGun;
        public bool Visible = false;
        public static float ItemID = 0;
        private UITextButtonGun switchAIBtn;
        private UITextButtonGun recoilButton;
        private UITextButtonGun ignoreTilesBtn;
        private UITextButtonGun TailButton;

        
        public override void OnInitialize()
        {
            int offsetX = 0;
            if (FlyingSwordUISystem.weaponSlotUI != null && FlyingSwordUISystem.weaponSlotUI.Visible)
                offsetX += 100;

            // 物品槽
            slot = new UIItemSlotFlyingGun(flyingGun?.items, 0);
            slot.Left.Set(600 + offsetX, 0f);
            slot.Top.Set(200, 0f);
            Append(slot);

            //攻击调整按钮
            switchAIBtn = new UITextButtonGun(Language.ActiveCulture.Name == "zh-Hans" ? "攻击调整": "Attack adjustments", () =>
            {
                FlyingGun.UseAttackAI = !FlyingGun.UseAttackAI;
            },
            () => 
            {
                if (FlyingGun.UseAttackAI)
                {
                    FlyingGun.AttackSpread -= MathHelper.ToRadians(22.5f);
                    if (FlyingGun.AttackSpread < MathHelper.ToRadians(0f))
                        FlyingGun.AttackSpread = MathHelper.ToRadians(180f);
                }
            });
            switchAIBtn.Left.Set(586 + offsetX, 0f);
            switchAIBtn.Top.Set(260, 0f);
            Append(switchAIBtn);

            //索敌按钮
            ignoreTilesBtn = new UITextButtonGun(
               "",
               () => {
                   FlyingGun.IgnoreTilesForTargeting = !FlyingGun.IgnoreTilesForTargeting;
               }
           );
            ignoreTilesBtn.Left.Set(586 + offsetX, 0f);
            ignoreTilesBtn.Top.Set(300, 0f);
            Append(ignoreTilesBtn);

            //后坐力按钮
            recoilButton = new UITextButtonGun(
                "",
                () => {
                    FlyingGun.recoilStrength += 1;
                    if (FlyingGun.recoilStrength > 10)
                        FlyingGun.recoilStrength = 10;
                },
                () => {
                    FlyingGun.recoilStrength -= 1;
                    if (FlyingGun.recoilStrength < 0)
                        FlyingGun.recoilStrength = 0;
                }
            );
            recoilButton.Left.Set(586 + offsetX, 0f);
            recoilButton.Top.Set(340, 0f);
            Append(recoilButton);

            //拖尾按钮
            TailButton = new UITextButtonGun(
               "",
               () => {
                   FlyingGun.IsTail = !FlyingGun.IsTail;
               }
           );
            TailButton.Left.Set(586 + offsetX, 0f);
            TailButton.Top.Set(380, 0f);
            Append(TailButton);
        }
       
        public void SetItem(FlyingGun flyingGun)
        {
            this.flyingGun = flyingGun;
            if (slot != null)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            var rect = switchAIBtn.GetDimensions().ToRectangle();
            if (switchAIBtn.isMouseOver)
            {
                if (FlyingGun.UseAttackAI)
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans" ? "纠缠敌人": "Entangle the enemy", rect.Center.X - 30, rect.Center.Y - 8, Color.YellowGreen, Color.Black, Vector2.Zero, 0.8f);
                else
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans" ? "环绕玩家": "Surround the player", rect.Center.X - 30, rect.Center.Y - 8, Color.IndianRed, Color.Black, Vector2.Zero, 0.8f);
                if(FlyingGun.UseAttackAI)
                {
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                       Language.ActiveCulture.Name == "zh-Hans" ? $"右键点击调整攻击覆盖度\n当前角度: {MathHelper.ToDegrees(FlyingGun.AttackSpread):0.0}°" : $"Right-click to adjust attack coveragenCurrent angle: {MathHelper.ToDegrees(FlyingGun.AttackSpread):0.0}°",
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);
                }
            }
            // 只有鼠标移入按钮时才绘制提示
            var rect_ = recoilButton.GetDimensions().ToRectangle();
            if (recoilButton.isMouseOver)
            {
                string tip = Language.ActiveCulture.Name == "zh-Hans" ? $"后坐力: {FlyingGun.recoilStrength}": $"Recoil: {FlyingGun.recoilStrength}";
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rect_.Center.X - 31, rect_.Center.Y - 8, Color.MediumPurple, Color.Black, Vector2.Zero, 0.8f);
            }else
            {
                string tip = Language.ActiveCulture.Name == "zh-Hans" ? "后坐力调整": "Recoil adjustment";
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rect_.Center.X - 38, rect_.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }

            var rectIgnore = ignoreTilesBtn.GetDimensions().ToRectangle();
            if (ignoreTilesBtn.isMouseOver)
            {
                string tip = FlyingGun.IgnoreTilesForTargeting
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Current: Wall Targeting")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Current: Normal Targeting");
                Color color_ = FlyingGun.IgnoreTilesForTargeting
                    ? Color.LightSkyBlue : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rectIgnore.Center.X - 24, rectIgnore.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            }else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "索敌穿墙" : "Wall Targeting", rectIgnore.Center.X - 30, rectIgnore.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }

            //拖尾开关
            var Rect = TailButton.GetDimensions().ToRectangle();
            if (TailButton.isMouseOver)
            {
                string tip = FlyingGun.IsTail
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Turned On")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Closed");
                Color color_ = FlyingGun.IsTail
                    ? Color.Goldenrod : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, Rect.Center.X - 24, Rect.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "光亮拖尾" : "Brightly Tailed", Rect.Center.X - 30, Rect.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            int offsetX = FlyingSwordUISystem.weaponSlotUI != null && FlyingSwordUISystem.weaponSlotUI.Visible ? 100 : 0;
            slot.Left.Set(600 + offsetX, 0f);
            switchAIBtn.Left.Set(586 + offsetX, 0f);
            recoilButton.Left.Set(586 + offsetX, 0f);
            ignoreTilesBtn.Left.Set(586 + offsetX, 0f);
            TailButton.Left.Set(586 + offsetX, 0f);

            if (flyingGun != null)
            {
                // 只允许远程武器
                if (flyingGun.items[0] != null && !flyingGun.items[0].IsAir && flyingGun.items[0].damage > 0 
                    && flyingGun.items[0].DamageType == DamageClass.Ranged)
                {
                    ItemID = flyingGun.items[0].type;
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
                ModContent.GetInstance<FlyingGunUISystem>().HideWeaponSlotUI();
                FlyingGun.IsClick = !FlyingGun.IsClick;
            }
        }
    }
    public class UITextButtonGun : UIElement
    {

        private string text;
        private Action onClick;
        private Action onRightClick;
        public bool isMouseOver = false;


        public UITextButtonGun(string text, Action onClick, Action onRightClick = null)
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

            if ((text != "攻击调整" && text != "Attack adjustments") || !isMouseOver)
            {
                int offsetX = -38;
                if (Language.ActiveCulture.Name == "zh-Hans" ? text == "攻击调整": text == "Attack adjustments")
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
            //Main.NewText("2");
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
    public class UIItemSlotFlyingGun : UIElement
    {
        private Item[] items;
        private int index;
        public bool isMouseOver = false;

        public UIItemSlotFlyingGun(Item[] items, int index)
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
            //Main.NewText("2");
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
            // 放入物品（只允许远程武器）
            else if (items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
                && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Ranged)
                && !player.ItemAnimationActive
                )
                )
            {
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 交换物品（只允许远程武器）
            else if (!items[index].IsAir && !Main.mouseItem.IsAir
                 && Main.mouseItem.damage > 0
                 && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Ranged)
                 && !player.ItemAnimationActive
                )
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
                    if(items[index].maxStack > 1)
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                       Language.ActiveCulture.Name == "zh-Hans" ? "此武器攻击需消耗背包中对应物品" : "This weapon attack consumes the corresponding item in the backpack",
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);
                }
                else
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                       Language.ActiveCulture.Name == "zh-Hans" ? "可放入远程武器": "Can be placed in ranged weapons", 
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.02f);
            }
        }
    }
    
    // 5. 弹幕类
    public class FlyingGunProj : ModProjectile
    {
        private float parentItemType = 0;
        private float parentItemShoot = 0;
        private float parentItemShootSpeed = 0;
        private float parentItemAnimation = 0;
        private float parentItemAmmo = 0;
        private float parentItemMaxStack = 0;

        private bool Isparent = false;

        public static bool appliedBloodFiend = false;

        NPC targetNPC = null;
        
        
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hide = false;
            Projectile.minion = true;
            Projectile.timeLeft = 2;
            Projectile.height = Projectile.width = 10;
            Projectile.minionSlots = 1;
            Projectile.tileCollide = false;
        }
        //public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        //{
        //}
        //public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        //{
        //}
        public override bool MinionContactDamage()
        {
            return false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.velocity = Vector2.Zero;
            base.OnSpawn(source);
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                cachedAverageColor = GetTextureAverageColor(texture_);
                averageColorCalculated = true;
            }
        }
        // 检查弹药是否充足（排除鼠标上的物品）
        private bool HasEnoughAmmo(Player player, int ammoType)
        {
            if (parentItemMaxStack > 1) // 消耗品武器
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    // 跳过鼠标上的物品
                    if (parentItemType == Main.mouseItem.type) return false;
                    if (item != null && !item.IsAir && item.type == parentItemType && item.stack > 0)
                        return true;
                }
            }
            else
            {
                if (ammoType <= 0) return true; // 不需要弹药
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    // 排除鼠标上的物品
                    if (Main.mouseItem.ammo == AmmoID.Arrow ||
                        Main.mouseItem.ammo == AmmoID.Bullet ||
                        Main.mouseItem.ammo == AmmoID.Gel) return false;
                    if (item != null && !item.IsAir && item.ammo == ammoType && item.stack > 0)
                        return true;
                }
            }
            
            return false;
        }
        
        private Item ConsumeAmmo(Player player, int ammoType)
        {
            if (parentItemMaxStack > 1) // 消耗品武器
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    if (item == Main.mouseItem) continue;
                    if (item != null && !item.IsAir && item.type == parentItemType && item.stack > 0)
                    {
                        Item consumed = item.Clone();
                        // 堆叠数量达到3996时不消耗
                        if (item.stack < 3996)
                            item.stack--;
                        if (item.stack <= 0)
                            item.TurnToAir();
                        return consumed;
                    }
                }
            }
            else
            {
                if (ammoType <= 0) return null;
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && !item.IsAir && item.ammo == ammoType && item.stack > 0 && item != Main.mouseItem
                         && item.type != ItemID.EndlessQuiver && item.type != ItemID.EndlessMusketPouch)
                    {
                        Item consumed = item.Clone();
                        // 堆叠数量达到3996时不消耗
                        if (item.stack < 3996)
                            item.stack--;
                        if (item.stack <= 0)
                            item.TurnToAir();
                        return consumed;
                    }
                }
            }
            
            return null;
        }
        void Attack()
        {
            Player player = Main.player[Projectile.owner];
            //var flyingSword = player.inventory.FirstOrDefault(i => i != null && i.type == ModContent.ItemType<Flyingsword>() && i.ModItem is Flyingsword fs && fs.items[0] != null && !fs.items[0].IsAir);

            if (targetNPC != null && targetNPC.active)
            {
                var weapon = parentItemShoot;

                if (weapon != 0 && weapon > ProjectileID.None && HasEnoughAmmo(player, (int)parentItemAmmo))
                {
                    int projType = (int)weapon;
                    Item ammoItem = ConsumeAmmo(player, (int)parentItemAmmo);

                    // 如果消耗了弹药，且弹药有 shoot 字段，则用弹药的 shoot 字段作为弹幕类型
                    if ((weapon == 1 || weapon == 10 || weapon == 14) &&
                        ammoItem != null && ammoItem.shoot > ProjectileID.None)
                    {
                        projType = ammoItem.shoot;
                    }

                    Vector2 shootDirection = (targetNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    // 线性插值
                    float shootSpeed = parentItemShootSpeed > 12f ? parentItemShootSpeed : 12f;
                    int shootDamage = Projectile.damage;
                    if (ammoItem != null && ammoItem.damage > 0)
                    {
                        shootDamage += ammoItem.damage;
                    }
                    float shootKnockback = Projectile.knockBack;

                    int newProj = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        shootDirection * shootSpeed,
                        projType,
                        shootDamage,
                        shootKnockback,
                        player.whoAmI
                    );
                    
                    if (newProj >= 0 && newProj < Main.maxProjectiles)
                    {
                        var proj = Main.projectile[newProj];
                        proj.friendly = true;
                        proj.timeLeft = 120;
                        proj.tileCollide = false;

                        if (!proj.usesLocalNPCImmunity || proj.localNPCHitCooldown > 10)
                        {
                            proj.usesLocalNPCImmunity = true;
                            proj.localNPCHitCooldown = 10; // 可调
                        }

                        if (proj.type == ProjectileID.PurificationPowder)
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed * 8f,
                                ProjectileID.Bullet,
                                shootDamage,
                                0f,
                                player.whoAmI
                            );
                        }
                        if (parentItemType == 5117)// 气喇叭
                        {
                            proj.Kill();
                            int randomValue = Main.rand.Next(24);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                projType,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                0, randomValue
                            );
                        }else if( parentItemType == 160)// 鱼叉枪
                        {
                            proj.Kill();
                            int Proj = Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                projType,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                            Main.projectile[Proj].aiStyle = ProjAIStyleID.StickProjectile;
                        }
                        else if (parentItemType == 4381)// 血雨弓
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                ProjectileID.BloodArrow,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                        }
                        else if (parentItemType == 4703)// 四管霰弹枪
                        {
                            proj.Kill();
                            for (int i = 0; i < 4; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3788)// 玛瑙爆破枪
                        {
                            proj.Kill();
                            for (int i = 0; i < 4; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ProjectileID.BlackBolt,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                        }
                        else if (parentItemType == 534)// 霰弹枪
                        {
                            proj.Kill();
                            for (int i = 0; i < 4; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3854)// 幽灵凤凰
                        {
                            proj.Kill();
                            for (int i = 0; i < 2; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    ProjectileID.FireArrow,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                            if (Main.rand.NextBool(5))
                            {
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ProjectileID.DD2PhoenixBowShot,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                    );
                            }
                        }
                        else if (parentItemType == 725)// 冰雪弓
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ProjectileID.FrostArrow,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                        }
                        else if (parentItemType == 3029)// 代达罗斯风暴弓
                        {
                            //proj.Kill();
                            float screenTopY = Main.screenPosition.Y + 32f;
                            Vector2 targetCenter = targetNPC.Center;
                            int arrowCount = Main.rand.Next(1, 2); // 1~2个弹幕
                            for (int i = 0; i < arrowCount; i++)
                            {
                                // 随机偏移方向（左或右）
                                int dir = Main.rand.NextBool() ? 1 : -1;
                                // 随机偏移距离（80~160）
                                float offsetX = dir * Main.rand.NextFloat(0f, 80f);
                                Vector2 spawnPos = new Vector2(targetCenter.X + offsetX, screenTopY);

                                // 目标点加10像素随机偏移
                                Vector2 targetOffset = targetCenter + new Vector2(Main.rand.NextFloat(-20f, 20f), Main.rand.NextFloat(-10f, 10f));
                                Vector2 dirVec = (targetOffset - spawnPos).SafeNormalize(Vector2.UnitY);

                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dirVec * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 1229)// 叶绿连弩
                        {
                            for (int i = 0; i < Main.rand.Next(0, 3); i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 679)// 战术霰弹枪
                        {
                            for (int i = 0; i < Main.rand.Next(2, 5); i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.34f, 0.34f); // 约±20°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 1156)// 食人鱼枪
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ModContent.ProjectileType<FlyingGunProjectile_190>(),
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                        }
                        else if (parentItemType == 2797)// 外星霰弹枪
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 1.6f,
                                    ModContent.ProjectileType<FlyingGunProjectile_444>(),
                                    (int)(shootDamage * Main.rand.NextFloat(1f, 3f)),
                                    shootKnockback,
                                    player.whoAmI
                                );

                            for (int i = 0; i < 3; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.1f, 0.1f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    ammoItem.shoot,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3930)// 喜庆弹射器Mk2
                        {
                            proj.Kill();
                            float X = Main.rand.NextFloat(-1f, 7f); // -10到10的整数

                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            // 四种烟花弹幕类型
                            int[] fireworks = new int[]
                            {
                                ProjectileID.Celeb2Rocket,
                                //ProjectileID.Celeb2RocketExplosive,
                                ProjectileID.Celeb2RocketLarge,
                                //ProjectileID.Celeb2RocketExplosiveLarge
                            };
                            // 随机选取一个不同的类型
                            int Prj = Main.rand.Next(fireworks.Length);
                            int selected = fireworks[Prj];

                            // 随机偏移角度（-20° ~ +20°）
                            float offsetAngle = Main.rand.NextFloat(-0.1f, 0.1f);
                            Vector2 perturbedDirection = dir.RotatedBy(offsetAngle);
                            Vector2 spawnPos = Projectile.Center + dir;
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                spawnPos,
                                perturbedDirection * shootSpeed * 0.8f,
                                selected,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                X,
                                0
                            );
                        }
                        else if (parentItemType == 3475)// 星璇机枪
                        {
                            proj.Kill();
                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            //前后排列
                            for (int i = 0; i < 3; i++)
                            {
                                // 随机偏移角度）
                                float offset_ = Main.rand.NextFloat(-0.04f, 0.04f);
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset_);
                                 
                                float offset = (i - 1) * 128f;
                                Vector2 spawnPos = Projectile.Center + dir * offset;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    perturbedDirection * shootSpeed,
                                    ammoItem.shoot,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                            if (Main.rand.NextBool(2))
                            {
                                float offset_ = Main.rand.NextFloat(-0.12f, 0.12f);
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset_);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    ProjectileID.VortexBeaterRocket,
                                    (int)(shootDamage * 1.4f),
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3546)// 喜庆弹射器
                        {
                            proj.Kill();
                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            // 四种烟花弹幕类型
                            int[] fireworks = new int[]
                            {
                                ProjectileID.RocketFireworkRed,
                                ProjectileID.RocketFireworkGreen,
                                ProjectileID.RocketFireworkBlue,
                                ProjectileID.RocketFireworkYellow
                            };
                            // 随机选取两个不同的类型
                            int first = Main.rand.Next(fireworks.Length);
                            int second;
                            do
                            {
                                second = Main.rand.Next(fireworks.Length);
                            } while (second == first);

                            int[] selected = new int[] { fireworks[first], fireworks[second] };

                            for (int i = 0; i < 2; i++)
                            {
                                // 随机偏移角度（-20° ~ +20°）
                                float offsetAngle = Main.rand.NextFloat(-0.1f, 0.1f);
                                Vector2 perturbedDirection = dir.RotatedBy(offsetAngle);
                                float offset = (i - 0.5f) * Main.rand.NextFloat(0f, 64f); // 前后随机间隔
                                Vector2 spawnPos = Projectile.Center + dir * offset;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    perturbedDirection * shootSpeed,
                                    selected[i],
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 434)// 发条式突击步枪
                        {
                            proj.Kill();
                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            //前后排列
                            for (int i = 0; i < 2; i++)
                            {
                                float offset = (i - 1) * 64f;
                                Vector2 spawnPos = Projectile.Center + dir * offset;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dir * shootSpeed * 0.8f,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if ((parentItemType == 1254 || parentItemType == 1255 || parentItemType == 1265)
                            && projType == ProjectileID.Bullet)// 狙击步枪，维纳斯万能枪，乌兹冲锋枪
                        {
                            proj.Kill();
                            int typeToShoot = (projType == ProjectileID.Bullet)
                                    ? ProjectileID.BulletHighVelocity
                                    : projType;
                            
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                typeToShoot,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                        }
                        else if (parentItemType == 2270)// 鳄鱼机关枪
                        {
                            proj.Kill();
                            // 随机偏移角度）
                            float offset = Main.rand.NextFloat(-0.12f, 0.12f);
                            Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                            float X = Main.rand.NextFloat(0.5f, 1.2f);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                perturbedDirection * shootSpeed * X,
                                projType,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                        }
                        else if (parentItemType == 1929)// 链式机枪
                        {
                            proj.Kill();
                            // 随机偏移角度）
                            float offset = Main.rand.NextFloat(-0.1f, 0.1f);
                            Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                perturbedDirection * shootSpeed,
                                projType,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                        }
                        else if (parentItemType == 3350)// 彩弹枪
                        {
                            proj.Kill();
                            float X = Main.rand.NextFloat(-2f, 3f); // -10到10的整数

                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI,
                                    0,
                                    X
                                );
                        }
                        else if (parentItemType == 120)// 熔火之怒
                        {
                            proj.Kill();
                            int typeToShoot = (projType == ProjectileID.WoodenArrowFriendly)
                                    ? ProjectileID.FireArrow
                                    : projType;
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    typeToShoot,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                        }
                        else if (parentItemType == 964)// 三发猎枪
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3859)// 空中祸害
                        {
                            proj.Kill();
                            for (int i = 0; i < 3; i++)
                            {
                                float extraOffset = MathHelper.ToRadians(i * 5f); // 顺时针每个2°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(extraOffset);
                               
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed * 2f,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 4953)// 日暮
                        {
                            proj.Kill();
                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            for (int i = 0; i < 5; i++)
                            {
                                float offset = (i - 1) * 64f;
                                Vector2 spawnPos = Projectile.Center + dir * offset;

                                float extraOffset = MathHelper.ToRadians(i * 1.2f); // 顺时针每个2°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(-extraOffset);
                                float X = Main.rand.NextFloat(-10, 11); // -10到10的整数

                                int typeToShoot = (projType == ProjectileID.WoodenArrowFriendly || i == 0)
                                    ? ProjectileID.FairyQueenRangedItemShot
                                    : projType;
                                int damage = i == 0 ? shootDamage * 3 : shootDamage;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    perturbedDirection * shootSpeed * 2f,
                                    typeToShoot,
                                    damage,
                                    shootKnockback,
                                    player.whoAmI,
                                    0,
                                    X
                                );
                            }
                        }
                        else if (parentItemType == 2624)// 海啸
                        {
                            proj.Kill();
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                                Vector2 perp = new Vector2(-dir.Y, dir.X);
                                float offset = (i - 1.5f) * 10f; // 垂直方向间隔10像素
                                Vector2 spawnPos = Projectile.Center + perp * offset;

                                // 中间两个弹幕（i==1,2）沿发射方向前移2像素
                                if (i == 1 || i == 2)
                                    spawnPos += dir * 5f;

                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dir * shootSpeed * 1.5f,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 2223)// 脉冲弓
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ProjectileID.PulseBolt,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                        }
                        else if (parentItemType == 3540)// 幻影弓
                        {
                            proj.Kill();
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                                Vector2 perp = new Vector2(-dir.Y, dir.X);

                                // 垂直方向随机间隔[-15, 15]像素
                                float perpOffset = Main.rand.NextFloat(-15f, 15f);
                                // 前后方向随机偏移[-5, 10]像素
                                float forwardOffset = Main.rand.NextFloat(-15f, 15f);
                                float X = Main.rand.NextFloat(0.5f, 2f);
                                Vector2 spawnPos = Projectile.Center + perp * perpOffset + dir * forwardOffset;
                                int Proj = Main.rand.NextBool(2) ? ammoItem.shoot : ProjectileID.PhantasmArrow;

                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dir * shootSpeed * X,
                                    Proj,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI,
                                    targetNPC.whoAmI,
                                    0
                                );
                            }
                        }
                    }
                    int recoilStrength = FlyingGun.recoilStrength * 2; // 后坐力强度，可调整
                    Projectile.Center -= shootDirection * recoilStrength;
                }
            }
        }
        private int ShootTimer = 0;
        private int ProjCount = 0;
        private readonly int MaxDis = 1200;
        
        public delegate bool SpecialCondition(NPC possibleTarget);
        public static bool ClosestNPC(ref NPC target, float maxDistance, Vector2 position, bool ignoreTiles = false, int overrideTarget = -1, SpecialCondition specialCondition = null)
        {
            if (specialCondition == null)
                specialCondition = _ => true;

            bool foundTarget = false;

            // 优先目标
            if (overrideTarget != -1)
            {
                NPC npc = Main.npc[overrideTarget];
                if ((npc.Center - position).Length() < maxDistance
                    && !npc.immortal
                    && (Collision.CanHit(position, 0, 0, npc.Center, 0, 0) || ignoreTiles)
                    && specialCondition(npc))
                {
                    target = npc;
                    return true;
                }
            }

            // 遍历所有 NPC
            for (int k = 0; k < Main.npc.Length; k++)
            {
                NPC possibleTarget = Main.npc[k];
                float distance = (possibleTarget.Center - position).Length();
                if (distance < maxDistance
                    && possibleTarget.active
                    && possibleTarget.chaseable
                    && !possibleTarget.dontTakeDamage
                    && !possibleTarget.friendly
                    && possibleTarget.lifeMax > 5
                    && !possibleTarget.immortal
                    && (Collision.CanHit(position, 0, 0, possibleTarget.Center, 0, 0) || ignoreTiles)
                    && specialCondition(possibleTarget))
                {
                    target = possibleTarget;
                    foundTarget = true;
                    maxDistance = distance;
                }
            }
            return foundTarget;
        }
        public override bool PreAI()
        {
            // 获取玩家
            var player = Main.player[Projectile.owner];
            if (!Isparent)
            {
                parentItemAnimation = Projectile.ai[0];
                parentItemType = (int)Projectile.ai[1];
                parentItemMaxStack = Projectile.ai[2];
                parentItemShootSpeed = Projectile.localAI[0];
                parentItemShoot = Projectile.localAI[1];
                parentItemAmmo = Projectile.localAI[2];
                
                Isparent = true;
            }
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingGun>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsFlyingGun>())) Projectile.Kill();
            targetNPC = null;
            if (HasEnoughAmmo(player, (int)parentItemAmmo) &&
                ClosestNPC(ref targetNPC, MaxDis, player.Center, FlyingGun.IgnoreTilesForTargeting, player.MinionAttackTargetNPC, npc => npc.active))
            {
                Projectile.rotation = (targetNPC.Center - Projectile.Center).ToRotation();
            }
            else
            {
                Projectile.rotation = 0;
            }
            
            if (targetNPC != null && targetNPC.active)
            {
                ShootTimer++;
                if(ShootTimer > parentItemAnimation)
                {
                    Attack();
                    ShootTimer = 0;
                }
            }
            if (FlyingGun.UseAttackAI && targetNPC != null && targetNPC.active && HasEnoughAmmo(player, (int)parentItemAmmo))
            {
                // 计算玩家到敌人的距离
                float playerToTarget = Vector2.Distance(player.Center, targetNPC.Center);

                // 设定弹幕环绕距离为：玩家到敌人距离的80%~120%，并限制最小/最大值
                float minRadius = 60f;
                float maxRadius = 320f;
                float orbitRadius = MathHelper.Clamp(playerToTarget * 0.8f, minRadius, maxRadius) + ProjCount * 4;

                AttackOrbitAI(Projectile, targetNPC, orbitRadius, 0.15f);
            }
            else
            {
                StandbyOrbitAI(Projectile, 80f + ProjCount * 4, 0.02f, 0.15f, player.MountedCenter);
            }
            return base.PreAI();
        }
        void AttackOrbitAI(Projectile projectile, NPC target, float orbitRadius, float lerpFactor)
        {
            // 统计同类弹幕数量和编号
            int index = 0, total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == projectile.owner && proj.type == projectile.type)
                {
                    if (i < projectile.whoAmI) index++;
                    total++;
                }
            }
            // 计算分布角度（180度扇形，正对玩家）
            Player player = Main.player[projectile.owner];
            Vector2 dir = (player.Center - target.Center).SafeNormalize(Vector2.UnitX);
            float baseAngle = dir.ToRotation();
            float spread = FlyingGun.AttackSpread; // 180度
            float angle = baseAngle - spread / 2 + spread * (index + 0.5f) / Math.Max(1, total);
            Vector2 targetPos = target.Center + orbitRadius * angle.ToRotationVector2();
            projectile.Center = Vector2.Lerp(projectile.Center, targetPos, lerpFactor);
        }
        void StandbyOrbitAI(Projectile projectile, float orbitRadius, float orbitSpeed, float lerpFactor, Vector2 center)
        {
            // 统计同类弹幕数量和编号
            int index = 0, total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == projectile.owner && proj.type == projectile.type)
                {
                    if (i < projectile.whoAmI) index++;
                    total++;
                    ProjCount = total;
                }
            }
            // 计算目标角度
            float targetAngle = MathHelper.TwoPi * index / Math.Max(1, total) + Main.GameUpdateCount * orbitSpeed;
            // 计算目标位置
            Vector2 targetPos = center + orbitRadius * targetAngle.ToRotationVector2();
            // 平滑插值移动
            projectile.Center = Vector2.Lerp(projectile.Center, targetPos, lerpFactor);
        }
        public override void AI()
        {
            //前缀血煞吸血效果添加
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                // 获取父武器实例
                Item weapon = new Item();
                weapon.SetDefaults(weaponType);
                Player player = Main.player[Projectile.owner];
                int prefix = player.GetModPlayer<FlyingGunPlayer>().LastSummonWeaponPrefix;
                // 判断是否为BloodFiend前缀
                if (prefix == ModContent.PrefixType<BloodFiend>())
                {
                    appliedBloodFiend = true;
                }
            }
        }

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            appliedBloodFiend = false;

            parentItemAnimation = 0;
            parentItemType = 0;
            parentItemMaxStack = 0;
            parentItemShootSpeed = 0;
            parentItemShoot = 0;
            parentItemAmmo = 0;
            Player player = Main.player[Projectile.owner];
            player.GetModPlayer<FlyingGunPlayer>().LastSummonWeaponPrefix = 0;

            base.Kill(timeLeft);
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
        public override bool PreDraw(ref Color lightColor)
        {
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                int frameCount = Main.itemAnimations[weaponType]?.FrameCount ?? 1;
                int frameHeight = texture_.Height / frameCount;
                int frameY = 0;
                // 弹幕帧与物品帧同步（可自定义）
                if (Main.itemAnimations[weaponType] != null)
                    frameY = Main.itemAnimations[weaponType].Frame * frameHeight;
                Rectangle rectangle = new Rectangle(0, frameY, texture_.Width, frameHeight);

                SpriteEffects effects = SpriteEffects.None; // 贴图效果
                float rotationOffset = 0;
                var player = Main.player[Projectile.owner];
                if(targetNPC != null && HasEnoughAmmo(player, (int)parentItemAmmo))
                {
                    if(targetNPC.Center.X < Projectile.Center.X)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                        rotationOffset = 135;
                    }else
                    {
                        effects = SpriteEffects.None;
                        rotationOffset = 0;
                    }
                }else
                {
                    if(player.direction == -1)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                    }
                }

                Color LightsColor = cachedAverageColor;
                var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
                var v3 = Main.rgbToHsl(LightsColor);
                v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
                var c = Main.hslToRgb(v3);
                c.A = 0;

                if(FlyingGun.IsTail)
                {
                    Color MyColor = c * (0.4f / 3f);
                    MyColor.A = 0;
                    int maxStep = ProjectileID.Sets.TrailCacheLength[Type] - 7;
                    if (Projectile.ai[0] != 0) maxStep += 7;
                    for (int i = 1; i < maxStep - 2; i++)
                    {
                        for (float j = 0; j < 1; j += 0.3f)
                        {
                            float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                            Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                            var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j);
                            Main.EntitySpriteDraw(texture_,
                                                  oldcenter,
                                                  rectangle,
                                                  MyColor * factor/* * Projectile.alpha*/,
                                                  oldRo + rotationOffset,
                                                  new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                                  Projectile.scale * 1f * factor,
                                                  effects,
                                                  0);
                        }
                    }
                }

                Main.EntitySpriteDraw(
                    texture_,
                    Projectile.Center - Main.screenPosition,
                    rectangle,
                    lightColor /** Projectile.alpha*/,
                    Projectile.rotation + rotationOffset,
                    new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                    Projectile.scale * 1f,
                    effects,
                    0
                    );
                #region 以下：渐变高光
                if(FlyingGun.IsTail)
                for (int i = 0; i < 3; i++)
                {
                    Main.EntitySpriteDraw(texture_,
                                          Projectile.Center - Main.screenPosition,
                                          rectangle,
                                          c * value * 0.6f /** Projectile.alpha*/,
                                          Projectile.rotation + rotationOffset,
                                          new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                          Projectile.scale * 1f,
                                          effects,
                                          0);
                }
                #endregion
                return false;
            }
            return false; // 阻止默认绘制
        }
    }

    // 6. Buff类
    class BuffsFlyingGun : ModBuff
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/AGlobalControl/BuffsFlyingControl";
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingGunProj>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
        private Texture2D weaponTex = null;
        public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
        {
            base.PostDraw(spriteBatch, buffIndex, drawParams);
            Player player = Main.LocalPlayer;
            int weaponType = player.GetModPlayer<FlyingGunPlayer>().LastSummonWeaponType;
            if (weaponType > 0 && weaponType < TextureAssets.Item.Length && TextureAssets.Item[weaponType].IsLoaded)
            {
                weaponTex = TextureAssets.Item[weaponType].Value;
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
    public class FlyingGunPlayer : ModPlayer
    {
        public int LastSummonWeaponType = 0;
        public int LastSummonWeaponPrefix = 0;
    }
}