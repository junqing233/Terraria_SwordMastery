using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.Weapons.Sword;
using SwordMastery.Content.Prefixes;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    // 1. 物品类
    public class Flyingsword : ModItem
    {
        // 存储选中的武器类型
        public static int MaxItems = 1;
        public Item[] items = Enumerable.Range(0, MaxItems).Select(_ => new Item()).ToArray();
        public static bool EnableProjectile = true; // 是否启用弹幕
        public static bool IsTail = false; // 是否拖尾
        public static float ExtraDrawRotation = 0f; // 额外绘制旋转角度（单位：弧度）
        public static bool UseThousandBladesMode = false; // 千刀万刮模式开关
        public bool isClick = false;
        public static bool IsClick = false;
        internal static FlyingSwordWeaponSlotUI weaponSlotUI;
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
            Item.width = 36;
            Item.height = 36;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = 20000;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingswordProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingsword>();
            Item.DamageType = DamageClass.Summon;
        }
        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override void AddRecipes()
        {
            // 创建一个新的配方组
            RecipeGroup group = new RecipeGroup(() => Language.ActiveCulture.Name == "zh-Hans" ? "铁锭或铅锭": "IronBarOrLeadBar",
                ItemID.IronBar,
                ItemID.LeadBar);
            // 注册配方组
            RecipeGroup.RegisterGroup("FlyingSword:IronBarOrLeadBar", group);

            CreateRecipe()
                .AddIngredient(ItemID.Ruby, 1)// 鲁比
                .AddIngredient(ItemID.Sapphire, 1)// 蓝宝石
                .AddIngredient(ItemID.Emerald, 1)// 绿宝石
                .AddIngredient(ItemID.Leather, 12)// 皮革
                .AddRecipeGroup("FlyingSword:IronBarOrLeadBar", 12) // 使用配方组
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        private float FinalWeaponDamage = 0;
        private float FinalWeaponKnockback = 0;
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (items[0] != null && !items[0].IsAir && items[0].damage > 0
                && (items[0].DamageType.CountsAsClass(DamageClass.Melee)
                || items[0].DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
                )
                )
            {
                int useTime = items[0].useTime;
                int minUseTime = 10;
                int maxUseTime = 40;
                float minScale = 0.52f;
                float maxScale = 0.32f;

                // 获取武器最终伤害（已包含饰品等加成）
                float finalWeaponDamage = player.GetWeaponDamage(items[0]);
                FinalWeaponDamage = finalWeaponDamage;
                // 读取配置并调整伤害
                var config = ModContent.GetInstance<SwordMasteryConfig>();
                if (config.StrengthExperience == StrengthMode.Ordinary)
                {
                    FinalWeaponDamage = (int)(FinalWeaponDamage * 0.6f);
                }
                // 区间外处理
                if (useTime <= minUseTime)
                    damage *= minScale * finalWeaponDamage / Item.damage;
                else if (useTime >= maxUseTime)
                    damage *= maxScale * finalWeaponDamage / Item.damage;
                else
                {
                    float t = (float)(useTime - minUseTime) / (maxUseTime - minUseTime);
                    float scale = minScale + (maxScale - minScale) * t;
                    damage *= scale * finalWeaponDamage / Item.damage;
                }
            }
            else
            {
                damage *= 0f;
            }
        }
        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            if (items[0] != null && !items[0].IsAir && items[0].knockBack > 0
                && (items[0].DamageType.CountsAsClass(DamageClass.Melee)
                || items[0].DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
                )
                )
            {
                FinalWeaponKnockback = player.GetWeaponKnockback(items[0]);
                // 近似一半（注意：StatModifier只能乘法，不能直接赋值）
                knockback *= 0.5f * player.GetWeaponKnockback(items[0]) / Item.knockBack;
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
                    && (items[0].DamageType.CountsAsClass(DamageClass.Melee)
                    || items[0].DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
                    )
                    )
                {
                    int useTime = items[0].useTime;
                    int minUseTime = 10;
                    int maxUseTime = 40;
                    float minScale = 0.52f; // 最短时间最大倍率
                    float maxScale = 0.32f; // 最长时间最小倍率
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
                    && (items[0].DamageType.CountsAsClass(DamageClass.Melee)
                    || items[0].DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
                    )
                    )
                {
                    showKnockback = FinalWeaponKnockback / 2f;
                }
                // 获取原始击退描述的后缀（如“击退”）
                string[] split = knockbackLine.Text.Split(' ');
                string suffix = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "击退";
                knockbackLine.Text = $"{showKnockback:0.##} {suffix}";
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
                weaponSlotUI = new FlyingSwordWeaponSlotUI();
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
                            ModContent.GetInstance<FlyingSwordUISystem>().ShowWeaponSlotUI(this);
                            SoundEngine.PlaySound(SoundID.MenuOpen); // 播放打开音效
                        }
                        else
                        {
                            ModContent.GetInstance<FlyingSwordUISystem>().HideWeaponSlotUI();
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
            //return FlyingSwordWeaponSlotUI.ItemID > 0;
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
            player.AddBuff(ModContent.BuffType<BuffsFlyingsword>(), 3600);
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].originalDamage = damage;
                // 传递唯一标识符（用 ai[1] 或 localAI[0]，或 ModProjectile 字段）
                Main.projectile[proj].ai[1] = items[0].type; // Guid转int
                Main.projectile[proj].localAI[1] = items[0].shoot;
                Main.projectile[proj].localAI[0] = items[0].shootSpeed;
                // 存储武器类型到玩家
                player.GetModPlayer<FlyingSwordPlayer>().LastSummonWeaponType = items[0].type;
                // 在Shoot方法里
                Main.projectile[proj].localAI[2] = items[0].prefix; // 传递前缀ID
                // 在 Shoot 方法里
                Main.projectile[proj].GetGlobalProjectile<FlyingSwordProjGlobal>().OnHitNPCTypeId = items[0].type;
            }
            return false;
        }
    }
    public class FlyingSwordProjGlobal : GlobalProjectile
    {
        public int OnHitNPCTypeId;
        public override bool InstancePerEntity => true;
    }
    // 2. UI系统
    public class FlyingSwordUISystem : ModSystem
    {
        internal static UserInterface weaponSlotInterface;
        internal static FlyingSwordWeaponSlotUI weaponSlotUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new FlyingSwordWeaponSlotUI();
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
                    "FlyingSword: WeaponSlotUI",
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

        public void ShowWeaponSlotUI(Flyingsword item)
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
    public class FlyingSwordWeaponSlotUI : UIState
    {
        private UIItemSlotFlyingSword slot;
        private Flyingsword flyingsword;
        public bool Visible = false;
        public static float ItemID = 0;
        private UITextButton enableProjButton;
        private UITextButton rotateButton;
        private UITextButton modeSwitchButton;
        private UITextButtonGun TailButton;


        public override void OnInitialize()
        {
            // 物品槽
            slot = new UIItemSlotFlyingSword(flyingsword?.items, 0);
            slot.Left.Set(600, 0f);
            slot.Top.Set(200, 0f);
            Append(slot);

            // 千刀万刮模式按钮
            modeSwitchButton = new UITextButton(
                "",
                () => {
                    Flyingsword.UseThousandBladesMode = !Flyingsword.UseThousandBladesMode;
                }
            );
            modeSwitchButton.Left.Set(586, 0f);
            modeSwitchButton.Top.Set(260, 0f);
            Append(modeSwitchButton);

            // 启用弹幕按钮
            enableProjButton = new UITextButton(Language.ActiveCulture.Name == "zh-Hans" ? "弹幕已启用": "Barrage is enabled", () =>
            {
                Flyingsword.EnableProjectile = !Flyingsword.EnableProjectile;

            });
            enableProjButton.Left.Set(586, 0f);
            enableProjButton.Top.Set(300, 0f);
            Append(enableProjButton);

            // 旋转按钮
            rotateButton = new UITextButton(
                Language.ActiveCulture.Name == "zh-Hans" ? "旋转武器": "Rotate the weapon",
                () => {
                    Flyingsword.ExtraDrawRotation += MathHelper.ToRadians(45f);
                    if (Flyingsword.ExtraDrawRotation > MathHelper.TwoPi)
                        Flyingsword.ExtraDrawRotation -= MathHelper.TwoPi;
                },
                () => {
                    Flyingsword.ExtraDrawRotation -= MathHelper.ToRadians(45f);
                    if (Flyingsword.ExtraDrawRotation < -MathHelper.TwoPi)
                        Flyingsword.ExtraDrawRotation += MathHelper.TwoPi;
                }
            );
            rotateButton.Left.Set(586, 0f);
            rotateButton.Top.Set(340, 0f);
            Append(rotateButton);

            //拖尾按钮
            TailButton = new UITextButtonGun(
               "",
               () => {
                   Flyingsword.IsTail = !Flyingsword.IsTail;
               }
           );
            TailButton.Left.Set(586, 0f);
            TailButton.Top.Set(380, 0f);
            Append(TailButton);
        }
       
        public void SetItem(Flyingsword flyingsword)
        {
            //this.flyingsword = flyingsword;
            //RemoveAllChildren();
            //OnInitialize();
            this.flyingsword = flyingsword;
            // 不要重复 RemoveAllChildren 和 OnInitialize
            if (slot != null)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            // 基础切换
            var rect_m = modeSwitchButton.GetDimensions().ToRectangle();
            if (modeSwitchButton.isMouseOver)
            {
                if (Flyingsword.UseThousandBladesMode)
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans" ? "千刀万剐" : "Thousand Blades", rect_m.Center.X - 30, rect_m.Center.Y - 8, Color.LightPink, Color.Black, Vector2.Zero, 0.8f);
                else
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                        Language.ActiveCulture.Name == "zh-Hans" ? "刃之女皇" : "Empress Blade", rect_m.Center.X - 30, rect_m.Center.Y - 8, Color.SkyBlue, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "基础逻辑" : "Basic Logic", rect_m.Center.X - 30, rect_m.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
            // 按钮提示层
            if (!Flyingsword.EnableProjectile)
            {
                var rect = enableProjButton.GetDimensions().ToRectangle();
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color.Red * 0.3f);
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "弹幕已禁用": "Barrage is disabled", rect.Center.X - 48, rect.Center.Y-10, Color.Red, Color.Black, Vector2.Zero, 1f);
            }
            // 只有鼠标移入旋转按钮时才绘制提示
            if (rotateButton.isMouseOver)
            {
                var rect_ = rotateButton.GetDimensions().ToRectangle();
                string tip = Language.ActiveCulture.Name == "zh-Hans" ? $"旋转:{MathHelper.ToDegrees(Flyingsword.ExtraDrawRotation):0}°": $"Revolve:{MathHelper.ToDegrees(Flyingsword.ExtraDrawRotation):0}°";
                // 右移10像素
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rect_.Center.X - 31, rect_.Center.Y - 8, Color.MediumPurple, Color.Black, Vector2.Zero, 0.8f);
            }
            //拖尾开关
            var Rect = TailButton.GetDimensions().ToRectangle();
            if (TailButton.isMouseOver)
            {
                string tip = Flyingsword.IsTail
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Turned On")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Closed");
                Color color_ = Flyingsword.IsTail
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
            if (flyingsword != null)
            {
                // 只允许近战武器
                if (flyingsword.items[0] != null && !flyingsword.items[0].IsAir && flyingsword.items[0].damage > 0 
                    && (flyingsword.items[0].DamageType == DamageClass.Melee
                    || flyingsword.items[0].DamageType == DamageClass.MeleeNoSpeed
                    )
                    )
                {
                    //flyingsword.selectedWeaponType = flyingsword.items[0].type;
                    ItemID = flyingsword.items[0].type;
                }
                else
                {
                    //flyingsword.selectedWeaponType = 0;
                    ItemID = 0;
                }
            }
            if (slot.isMouseOver)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            if (!Main.playerInventory)
            {
                ModContent.GetInstance<FlyingSwordUISystem>().HideWeaponSlotUI();
                Flyingsword.IsClick = !Flyingsword.IsClick;
            }
        }
    }
    public class UITextButton : UIElement
    {
        private string text;
        private Action onClick;
        private Action onRightClick;
        public bool isMouseOver = false;


        public UITextButton(string text, Action onClick, Action onRightClick = null)
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
            if (text != "旋转武器" && text != "Rotate the weapon" || !isMouseOver) 
            {
                int offsetX = -38;
                if (Language.ActiveCulture.Name == "zh-Hans" ? text == "旋转武器": text == "Rotate the weapon")
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
            if(isMouseOver)
                Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
            base.Update(gameTime);
        }
    }
    // UIElement
    public class UIItemSlotFlyingSword : UIElement
    {
        private Item[] items;
        private int index;
        public bool isMouseOver = false;

        public UIItemSlotFlyingSword(Item[] items, int index)
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
            // 放入物品（只允许近战武器）
            else if (items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
                && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Melee)
                || Main.mouseItem.DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
                )
                && !player.ItemAnimationActive
                )
            {
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 交换物品（只允许近战武器）
            else if (!items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
                 && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Melee)
                || Main.mouseItem.DamageType.CountsAsClass(DamageClass.MeleeNoSpeed)
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
                       Language.ActiveCulture.Name == "zh-Hans" ? "可放入近战武器": "Can be placed in melee weapons", 
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.02f);
            }
        }
    }
    // 5. 弹幕类
    public class FlyingswordProj : ModProjectile
    {
        //====================原飞剑====================
        private float parentItemType = 0;
        private float parentItemShoot = 0;
        private float parnetItemShootSpeed = 0;
        private bool Isparent = false;
        NPC targetNPC = null;
        //====================臭虫剑====================
        enum SwordAIState
        {
            Stab,       // 刺击
            Circle,     // 盘旋
            Slash       // 斩击（椭圆）
        }
        private SwordAIState aiState = SwordAIState.Stab;
        private int aiTimer = 0;
        private Vector2 ellipseCenter;
        private float ellipseAngle;
        private float ellipseA, ellipseB;
        private bool stabPassedTarget = false;
        private Vector2 stabDir;
        public static bool appliedBloodFiend = false;
        //==============================================

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
            Projectile.CloneDefaults(ProjectileID.EmpressBlade);
            AIType = ProjectileID.EmpressBlade;
            Projectile.hide = false;
            Projectile.minion = true;
            Projectile.timeLeft = 2;
            Projectile.height = Projectile.width = 10;
            Projectile.minionSlots = 1;
        }
        public override bool CanHitPlayer(Player target)
        {
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Flyingsword.UseThousandBladesMode)
                modifiers.FinalDamage -= 0.4f;
            //=====================臭虫剑======================无视防御
            if (parentItemType == 5129 && target.friendly)
            {
                // 让本弹幕无视防御：加回被防御抵消的伤害
                if (target.type == NPCID.Nurse)//护士翻倍
                {
                    modifiers.ArmorPenetration += target.defense;
                    modifiers.FinalDamage += 1;
                }
                else
                    modifiers.ArmorPenetration += target.defense;
            }
            if (parentItemType == 4788 || parentItemType == 4789 || parentItemType == 4790)
            {
                modifiers.FinalDamage += Main.player[Projectile.owner].velocity.Length()/10;
            }
            if(parentItemType == 671)
            {
                Projectile.damage = Projectile.damage*(((1 - (target.life / target.lifeMax))*10/9)+1);
                ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.Keybrand,
                    new ParticleOrchestraSettings { PositionInWorld = Main.rand.NextVector2FromRectangle(target.Hitbox) },
                    Projectile.owner);
            }
        }
        private int clown = 0;// 攻击冷却
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Flyingsword.EnableProjectile) return;
            // 冷却判定
            if (clown > 0)
                return;
            var global = Projectile.GetGlobalProjectile<FlyingSwordProjGlobal>();
            if (global.OnHitNPCTypeId > 0)
            {
                if (Flyingsword.UseThousandBladesMode)
                    clown = 15;
                else
                    clown = 30;
                //===================养蜂人===================击中
                if (global.OnHitNPCTypeId == ItemID.BeeKeeper)
                {
                    FlyingswordEffect.BeeKeeperEffect(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================魔光剑===================击中
                if (global.OnHitNPCTypeId == ItemID.LightsBane)
                {
                    FlyingswordEffect.LightsBaneEffect(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================臭虫剑===================击中
                if(global.OnHitNPCTypeId == 5129)
                {
                    target.AddBuff(BuffID.Stinky, 300);
                    SoundEngine.PlaySound(SoundID.Item16, target.Center);
                    return;
                }
                //===================血腥屠刀==================击中
                if(global.OnHitNPCTypeId == 795)
                {
                    FlyingswordEffect.BloodButchererEffect(Main.player[Projectile.owner], target, damageDone);
                    return;
                }
                //=====================村正====================击中
                if (global.OnHitNPCTypeId == 155)
                {
                    FlyingswordEffect.MuramasaEffect(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //====================蝙蝠棍===================击中
                if (global.OnHitNPCTypeId == 5097)
                {
                    if (Main.rand.Next(4) > 1)
                        return;
                    var player = Main.player[Projectile.owner];
                    int re = Main.rand.NextBool(10) ? 5 : 1;
                    if (player.statLife != player.statLifeMax)
                        player.statLife += re;
                    player.HealEffect(re, true);
                    return;
                }
                //=====================火山====================击中
                if (global.OnHitNPCTypeId == 121)
                {
                    FlyingswordEffect.VolcanoEffect(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================瞌睡章鱼===================击中
                if (global.OnHitNPCTypeId == ItemID.MonkStaffT1)
                {
                    FlyingswordEffect.MonkStaffT1ExplosionEffct(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================草剑===================击中
                if (global.OnHitNPCTypeId == ItemID.BladeofGrass)
                {
                    FlyingswordEffect.BladeOfGrassEffct(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================舌锋剑===================击中
                if (global.OnHitNPCTypeId == 3211)
                {
                    FlyingswordEffect.IchorSplashEffct(Main.player[Projectile.owner], target, damageDone, Projectile);
                    return;
                }
                //===================地狱烙印===================击中
                if (global.OnHitNPCTypeId == 3823)
                {
                    if(Main.rand.NextBool(4))
                        target.AddBuff(BuffID.OnFire3, 300);
                    if(Main.rand.NextBool(100))
                        Main.player[Projectile.owner].AddBuff(BuffID.ParryDamageBuff, 300);
                    return;
                }
                //===================真永夜刃===================击中
                if (global.OnHitNPCTypeId == 675)
                {
                    FlyingswordEffect.TrueNightsEdgeEffct(target, Projectile, 0.8f);
                    //return;
                }
                //===================永夜刃===================击中
                if (global.OnHitNPCTypeId == 273)
                {
                    FlyingswordEffect.TrueNightsEdgeEffct(target, Projectile, 1f);
                    return;
                }
                //=====================断钢剑==================击中
                if (global.OnHitNPCTypeId == 368)
                {
                    FlyingswordEffect.ExcaliburEffct(target, Projectile);
                    return;
                }
                //====================真断钢剑==================击中
                if (global.OnHitNPCTypeId == 674)
                {
                    FlyingswordEffect.TrueExcaliburEffct(target, Projectile);
                    return;
                }
                //====================泰拉刃==================击中
                if (global.OnHitNPCTypeId == 757)
                {
                    FlyingswordEffect.TerraBladeEffct(target, Projectile);
                    //return;
                }
                //=============================================
                ModItem modItem = ModContent.GetModItem(global.OnHitNPCTypeId);
                if (modItem != null)
                {
                    var method = modItem.GetType().GetMethod("OnHitNPC", new[] { typeof(Player), typeof(NPC), typeof(NPC.HitInfo), typeof(int) });
                    if (method != null)
                    {
                        method.Invoke(modItem, new object[] { Main.player[Projectile.owner], target, hit, damageDone });
                    }
                }
            }
            targetNPC = target;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                cachedAverageColor = GetTextureAverageColor(texture_);
                averageColorCalculated = true;
            }
        }
        private readonly int MaxDis = 800;
        public override bool MinionContactDamage()
        {
            if (FindNPC(MaxDis) > 0 && FindNPC(MaxDis) < Main.npc.Length || Projectile.ai[0] != 0
                || Projectile.hostile
                )
                return true;
            return base.MinionContactDamage();
        }
        private int FindNPC(int dis)
        {
            return Projectile.FindTargetWithLineOfSight(dis);
        }
        public interface IFlyingSwordAdaptable
        {
            void OnFlyingSwordAI(Projectile proj, Flyingsword swordItem);
        }
        //===============悠悠球===============发射限制
        bool Yoyo(int projType, Player player)
        {
            int yoyoAiStyle = 99;
            bool isYoyo = false;
            if (projType >= 0 && projType < ProjectileID.Sets.YoyosLifeTimeMultiplier.Length && ProjectileID.Sets.YoyosLifeTimeMultiplier[projType] > 0f)
            {
                isYoyo = true;
            }
            else
            {
                var tempProj = new Projectile();
                tempProj.SetDefaults(projType);
                if (tempProj.aiStyle == ProjAIStyleID.Yoyo)
                    isYoyo = true;
            }
            // 如果是溜溜球且玩家手持物品就是发射该弹幕的物品，则销毁所有同类弹幕并返回false
            if (isYoyo && player.HeldItem != null && player.HeldItem.shoot == projType)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    var p = Main.projectile[i];
                    if (p.active && p.owner == player.whoAmI && p.type == projType && p.aiStyle == yoyoAiStyle)
                    {
                        p.Kill();
                    }
                }
                return false;
            }
            // 限制同类溜溜球弹幕数量
            if (isYoyo)
            {
                int count = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    var p = Main.projectile[i];
                    if (p.active && p.owner == player.whoAmI && p.type == projType && p.aiStyle == yoyoAiStyle)
                    {
                        count++;
                    }
                }
                if (count > 0)
                    return false; // 已有同类溜溜球弹幕，禁止再发射
            }
            return true; // 允许发射
        }

        // 在 Attack 或 OnHitNPC 里发射弹幕后调用
        void Attack()
        {
            Player player = Main.player[Projectile.owner];
            
            if (targetNPC != null && targetNPC.active)
            {
                var weapon = parentItemShoot;
               
                if (weapon != 0 && weapon > ProjectileID.None)
                {
                    int projType = (int)weapon;

                    // 只在允许时才发射
                    if (!Yoyo(projType, player))
                        return;

                    Vector2 shootDirection = (targetNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    float shootSpeed = parnetItemShootSpeed != 0 ? parnetItemShootSpeed : 8;
                    int shootDamage = Projectile.damage / 2;
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
                        proj.timeLeft = 240;
                        if (!proj.usesLocalNPCImmunity || proj.localNPCHitCooldown > 10)
                        {
                            proj.usesLocalNPCImmunity = true;
                            proj.localNPCHitCooldown = 10; // 可调
                        }
                        
                        if (proj.type == ProjectileID.VampireKnife)//吸血鬼刀
                        {
                            int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    targetNPC.Center,
                                    Vector2.Zero,
                                    ProjectileID.VampireHeal,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI,
                                    0,
                                    shootDamage / (4 * Main.rand.NextFloat(1f, 2f))
                                );
                        }
                        if(proj.type == ProjectileID.SporeCloud || proj.type == ProjectileID.ChlorophyteOrb)// 叶绿气团，叶绿球珠
                        {
                            proj.usesLocalNPCImmunity = false;
                        }

                        if (proj.aiStyle == ProjAIStyleID.SleepyOctopod)//天龙之怒
                        {
                            if (proj.type == ProjectileID.MonkStaffT3)
                                proj.Kill();
                            Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 8f,
                                    ProjectileID.MonkStaffT3_AltShot,
                                    shootDamage,
                                    0f,
                                    player.whoAmI
                                );
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.Flairon)//猪鲨链锤
                        {
                            if (proj.type == ProjectileID.Flairon)
                            {
                                proj.Kill();
                                for (int i = 0; i < 6; i++)
                                    Projectile.NewProjectile(
                                        Projectile.GetSource_FromThis(),
                                        targetNPC.Center,
                                        Vector2.Zero,
                                        ProjectileID.FlaironBubble,
                                        shootDamage,
                                        0f,
                                        player.whoAmI,
                                        0,
                                        Main.rand.Next(8, 10),
                                        0
                                    );
                            }
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.SuperStarBeam)//草剑
                        {
                            if (proj.type == ProjectileID.BladeOfGrass)
                                proj.Kill();
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.TrueNightsEdge)
                        {
                            if (proj.type == ProjectileID.TrueNightsEdge)// 真永夜刃
                            {
                                proj.Kill();
                                FlyingswordEffect.TrueNightsEdge(targetNPC, Projectile);
                            }
                            else
                            if (proj.type == ProjectileID.TerraBlade2Shot)
                            {
                                proj.Kill();
                                FlyingswordEffect.TerraBlade(targetNPC, Projectile);
                            }
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.SleepyOctopod)//瞌睡章鱼
                        {
                            if (parentItemType == 3835)
                                proj.Kill();
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.LightsBane)// 魔光剑
                        {
                            if (parentItemType == ItemID.LightsBane)
                                proj.Kill();
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.Harpoon)//链刀
                        {
                            proj.aiStyle = ProjAIStyleID.Reaping;
                            proj.hide = false;
                            proj.penetrate = 2;
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.ShortSword)//标尺
                        {
                            proj.aiStyle = ProjAIStyleID.Powder;
                            proj.hide = false;
                            proj.rotation = proj.velocity.ToRotation() + MathHelper.ToRadians(90);
                            proj.velocity *= Main.rand.NextFloat(5, 10);
                            proj.penetrate = 2;
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.Flail)//链锤
                        {
                            proj.aiStyle = ProjAIStyleID.Boomerang;
                            if (Main.rand.Next(4) <= 1)
                            {
                                if (player.statLife != player.statLifeMax)
                                    player.statLife += proj.damage / 5 > 0 ? proj.damage / 5 : 1;
                                player.HealEffect(proj.damage / 5 > 0 ? proj.damage / 5 : 1, true);
                            }
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.Yoyo)//溜溜球
                        {
                            proj.aiStyle = -1;
                        }

                        else
                        if (proj.aiStyle == ProjAIStyleID.Spear)//矛
                        {
                            proj.aiStyle = ProjAIStyleID.Powder;
                            proj.hide = false;
                            proj.rotation = proj.velocity.ToRotation() + MathHelper.ToRadians(135);
                            proj.velocity *= Main.rand.NextFloat(5, 10);
                            proj.penetrate = 4;
                            proj.timeLeft = 60;

                            if (parentItemType == 4788 || parentItemType == 4789 || parentItemType == 4790)//骑枪
                            {
                                proj.Kill();
                            }
                            else
                            if (parentItemType == 756)//蘑菇长矛
                            {
                                proj.Kill();
                                int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 12f,
                                    ProjectileID.Mushroom,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                                Main.projectile[newProj_].penetrate = 4;
                            }
                            else
                            if (parentItemType == 1228)//叶绿镋
                            {
                                proj.Kill();
                                int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 4f,
                                    ProjectileID.SporeCloud,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                            else
                            if (parentItemType == 4061)//风暴长矛
                            {
                                proj.Kill();
                                int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 8f,
                                    ProjectileID.ThunderSpearShot,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                                Main.projectile[newProj_].penetrate = 4;
                            }
                            else
                            if (parentItemType == 1947)//北极
                            {
                                proj.Kill();
                                int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 2.6f,
                                    ProjectileID.NorthPoleSpear,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                                Main.projectile[newProj_].usesLocalNPCImmunity = true;
                                Main.projectile[newProj_].localNPCHitCooldown = 10;
                            }
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.ForwardStab)//恐怖关刀
                        {
                            proj.aiStyle = -1;
                        }
                        else
                        if (proj.aiStyle == ProjAIStyleID.HeldProjectile)//星光
                        {
                            proj.aiStyle = ProjAIStyleID.Reaping; // 禁用原版AI
                            proj.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2 * 2;
                            proj.timeLeft = 30;
                        }
                        //天晶剑
                        else TrySpawnAllHeadProjectiles(player, (int)(Projectile.damage * 0.8f), Projectile.knockBack, player.whoAmI, proj.type, proj);
                    }
                }
            }
        }
        // 定义所有需要映射的弹幕对（本体, 头部）
        private static readonly (int body, int head)[] SwordHeadPairs = new[]
        {
            (ModContent.ProjectileType<TianjingSwordProj>(), ModContent.ProjectileType<TianjingSwordProj_Head>()),
            (ModContent.ProjectileType<TianjingSword_0Proj>(), ModContent.ProjectileType<TianjingSword_0Proj_Head>()),
            (ModContent.ProjectileType<TianjingSword_1Proj>(), ModContent.ProjectileType<TianjingSword_1Proj_Head>()),
            (ModContent.ProjectileType<TianjingSword_2Proj>(), ModContent.ProjectileType<TianjingSword_2Proj_Head>()),
            (ModContent.ProjectileType<TianjingSword_3Proj>(), ModContent.ProjectileType<TianjingSword_3Proj_Head>()),
        };
        private static void TrySpawnAllHeadProjectiles(Player player, int damage, float knockback, int owner, int killedType, Projectile proj)
        {
            foreach (var (body, head) in SwordHeadPairs)
            {
                if (killedType == body)
                {
                    proj.Kill();
                    int MaxCount = 0, headCount = 0;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.owner == owner)
                        {
                            if (p.type == ModContent.ProjectileType<FlyingswordProj>()) MaxCount++;
                            else if (p.type == head) headCount++;
                        }
                    }
                    if (headCount < (MaxCount / 2f))
                    {
                        Projectile.NewProjectile(
                            player.GetSource_ItemUse(player.HeldItem),
                            player.Center,
                            Vector2.Zero,
                            head,
                            damage,
                            knockback,
                            owner
                        );
                    }
                    break; // 只处理一次
                }
            }
        }
        private bool tb_isCharging = false; // 是否正在冲击
        private int tb_chargeTimer = 0; // 冲击计时
        private float tb_angleOffset = 0f; // 攻击保底
        private int tb_cooldown = 0; // 冷却计时
        private bool IsVector2Zero = false;

        private Vector2 tb_targetPos = Vector2.Zero; // 当前冲刺目标点

        public override bool PreAI()
        {
            // 冷却递减
            if (clown > 0)
                clown--;
            // 获取玩家
            var player = Main.player[Projectile.owner];
            if (!Isparent)
            {
                parentItemType = (int)Projectile.ai[1];
                parentItemShoot = Projectile.localAI[1];
                parnetItemShootSpeed = Projectile.localAI[0];
                Isparent = true;
                return base.PreAI();
            }
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingsword>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsFlyingsword>())) Projectile.Kill();
            ////修改此参数以确定攻击范围
            var n = FindNPC(MaxDis);
            if (Flyingsword.UseThousandBladesMode)
            {
                if(!IsVector2Zero)
                {
                    Projectile.velocity = Vector2.Zero;
                    IsVector2Zero = true;
                }
                Projectile.localNPCHitCooldown = 20;
                //=====================千刀万刮=================
                NPC target_m = null;
                if (FlyingGunProj.ClosestNPC(ref target_m, 1000f, player.Center, false, player.MinionAttackTargetNPC, npc => npc.active))
                {
                    Projectile.height = Projectile.width = 30;
                    if (tb_cooldown > 0)
                    {
                        tb_cooldown--;
                        return false;
                    }

                    if (!tb_isCharging)
                    {
                        Vector2 toTarget = (target_m.Center - Projectile.Center);
                        if (toTarget.Length() < 1f) toTarget = Vector2.UnitX;
                        toTarget = toTarget.SafeNormalize(Vector2.UnitX);
                        float randomAngle = Main.rand.NextFloat(-0.35f, 0.35f);
                        Vector2 dir = toTarget.RotatedBy(randomAngle);

                        float chargeDistance = 120f; // 冲刺距离（可调）
                        tb_targetPos = target_m.Center + dir * chargeDistance; // 冲过敌人一段距离
                        tb_isCharging = true;
                        tb_chargeTimer = 0;
                    }

                    // 平滑移动到目标点
                    float moveSpeed = 0.2f+Main.rand.NextFloat(-0.1f, 0.1f); // 越大越快，建议0.2~0.4
                    Projectile.Center = Vector2.Lerp(Projectile.Center, tb_targetPos, moveSpeed);
                    tb_chargeTimer++;

                    // 判断是否到达目标点
                    if ((Projectile.Center - tb_targetPos).Length() < 8f || tb_chargeTimer > 20)
                    {
                        tb_isCharging = false;
                        tb_cooldown = 4;
                        tb_angleOffset++;

                        if(Main.rand.NextBool(5) || tb_angleOffset > 5)
                        {
                            Attack();
                            tb_angleOffset = 0;
                        }
                    }

                    Projectile.rotation = (tb_targetPos - Projectile.Center).ToRotation() + MathHelper.PiOver2;
                    return false;
                }
                else
                {
                    if (parentItemType == 5129 && FindNPC(MaxDis) == -1)
                    {
                        if (HandleBugSwordAI(player)) return false;
                    }
                    HandleIdleDistribution(player);
                }
            }
            else //======================御剑===============================
            {
                if (IsVector2Zero)
                    IsVector2Zero = false;
                Projectile.height = Projectile.width = 10;
                if (n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0)
                {
                    if (Flyingsword.EnableProjectile &&
                        (Projectile.ai[0] == 1 || Projectile.ai[0] == 68))
                        Attack();
                    return base.PreAI();

                }
                else if (parentItemType == 5129 && FindNPC(MaxDis) == -1)
                {
                    if (HandleBugSwordAI(player)) return false;
                }

                Projectile.ai[2] = 0;
                foreach (var p in Main.projectile)
                {
                    if (p != null && p.active && SwordProjectileGroup.AllTypes.Contains(p.type))
                        if (p.whoAmI < Projectile.whoAmI) Projectile.ai[2]++;
                }

                #region 以下：魔改原版的代码
                int num3 = (int)Projectile.ai[2] + 1;
                var idleRotation3 = num3 * ((float)Math.PI * 2f) * (1f / 60f) * player.direction + (float)Math.PI / 2f;
                idleRotation3 = MathHelper.WrapAngle(idleRotation3);
                int num4 = (int)(num3 % idleRotation3);
                Vector2 vector = new Vector2(0f, 0.5f).RotatedBy((player.miscCounterNormalized * (2f + num4) + num4 * 0.5f + player.direction * 1.3f) * ((float)Math.PI * 2f)) * 4f;
                var idleSpot = Projectile.rotation.ToRotationVector2() * 10f + player.MountedCenter + new Vector2(player.direction * (num3 * -6 - 16), player.gravDir * -15f);
                idleSpot += vector + new Vector2(8, 15);
                idleRotation3 += (float)Math.PI / 2f;

                Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation3, 0.45f);
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, idleSpot, 0.45f);
                for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                {
                    Projectile.localNPCImmunity[i] = 0;
                }
                #endregion
            }
            return false;
        }
        private void HandleIdleDistribution(Player player)
        {
            tb_targetPos = player.Center;
            tb_chargeTimer = 0;
            tb_cooldown = 0;
            Projectile.height = Projectile.width = 10;
            int total = 0, index = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                var p = Main.projectile[i];
                if (p != null && p.active && p.owner == Projectile.owner && p.type == Type)
                {
                    if (p.whoAmI < Projectile.whoAmI) index++;
                    total++;
                }
            }
            Projectile.rotation = 0;
            if (total == 1)
            {
                Vector2 targetPos = player.Center + new Vector2(0, -60f);
                Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.15f);
            }
            else
            {
                Vector2 ellipseCenter = player.Center + new Vector2(0, -60f);
                float longAxis = 1f + total * 5;
                float shortAxis = 1f + total * 0.8f;
                float baseSpeed = 0.03f;
                float speedFactor = MathHelper.Clamp(total / 1000f, 0.001f, 0.029f);
                float baseAngle = Main.GameUpdateCount * (baseSpeed - speedFactor);
                float angleStep = MathHelper.TwoPi / total;
                float angle = baseAngle + index * angleStep;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * longAxis, (float)Math.Sin(angle) * shortAxis);
                Vector2 targetPos = ellipseCenter + offset;
                Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.15f);
            }
            for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                Projectile.localNPCImmunity[i] = 0;
        }
        private bool HandleBugSwordAI(Player player)
        {
            NPC target = null;
            float closestDistance = 400;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.lifeMax > 5 && !npc.dontTakeDamage && npc.friendly)
                {
                    float distance = Vector2.Distance(player.Center, npc.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        target = npc;
                    }
                }
            }
            if (target != null)
            {
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 10;
                Projectile.hostile = true;
                aiTimer++;
                switch (aiState)
                {
                    case SwordAIState.Stab:
                        if (!stabPassedTarget)
                        {
                            stabDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                            float speed = 10f;
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, stabDir * speed, 0.12f);
                            float dist = Vector2.Distance(Projectile.Center, target.Center);
                            if (dist < 20f)
                            {
                                stabPassedTarget = true;
                                aiTimer = 0;
                            }
                        }
                        else
                        {
                            float speed = 6f;
                            Projectile.velocity = stabDir * speed;
                            aiTimer++;
                            if (aiTimer > 30)
                            {
                                aiState = SwordAIState.Circle;
                                aiTimer = 0;
                                stabPassedTarget = false;
                            }
                        }
                        break;
                    case SwordAIState.Circle:
                        if (aiTimer == 0)
                        {
                            ellipseCenter = target.Center;
                            ellipseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        }
                        float radius = 80f + 20f * (float)Math.Sin(aiTimer * 0.1f);
                        ellipseAngle += 0.045f;
                        Vector2 offset = new Vector2((float)Math.Cos(ellipseAngle), (float)Math.Sin(ellipseAngle)) * radius;
                        Vector2 dest = ellipseCenter + offset;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, (dest - Projectile.Center).SafeNormalize(Vector2.UnitY) * 7f, 0.10f);
                        if (aiTimer > 50)
                        {
                            aiState = Main.rand.NextBool() ? SwordAIState.Stab : SwordAIState.Slash;
                            aiTimer = 0;
                            if (aiState == SwordAIState.Slash)
                            {
                                ellipseA = 120f;
                                ellipseB = 40f;
                                ellipseCenter = target.Center;
                                ellipseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                            }
                        }
                        break;
                    case SwordAIState.Slash:
                        ellipseAngle += 0.10f;
                        Vector2 ellipseOffset = new Vector2((float)Math.Cos(ellipseAngle) * ellipseA, (float)Math.Sin(ellipseAngle) * ellipseB);
                        //Vector2 dest = ;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, (ellipseCenter + ellipseOffset - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10f, 0.12f);
                        if (ellipseAngle > MathHelper.TwoPi)
                        {
                            aiState = Main.rand.NextBool() ? SwordAIState.Stab : SwordAIState.Circle;
                            aiTimer = 0;
                        }
                        break;
                }
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
                return true;
            }
            else
            {
                Projectile.usesLocalNPCImmunity = false;
                Projectile.hostile = false;
                Projectile.velocity = Vector2.Zero;
            }
            return false;
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

                int prefix = (int)Projectile.localAI[2];

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

            parentItemType = 0;
            parnetItemShootSpeed = 0;
            parentItemShoot = 0;

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
        private bool flag = false;
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

                SpriteEffects effects; // 贴图效果
                float rotationOffset;
                var player = Main.player[Projectile.owner];
                var n = FindNPC(MaxDis);
                if (Flyingsword.UseThousandBladesMode)
                {
                    if (player.direction == 1)
                    {
                        rotationOffset = 0f - Flyingsword.ExtraDrawRotation;
                        effects = SpriteEffects.None; // 贴图不翻转
                    }
                    else
                    {
                        rotationOffset = MathHelper.ToRadians(90f) + Flyingsword.ExtraDrawRotation; // 旋转偏移135度
                        effects = SpriteEffects.FlipHorizontally; // 翻转贴图
                    }
                }
                else
                {
                    if (!(n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0))
                    {
                        if (player.direction == -1)
                        {
                            rotationOffset = 0f - Flyingsword.ExtraDrawRotation;
                            effects = SpriteEffects.None; // 贴图不翻转
                        }
                        else
                        {
                            rotationOffset = MathHelper.ToRadians(90f) + Flyingsword.ExtraDrawRotation; // 旋转偏移135度
                            effects = SpriteEffects.FlipHorizontally; // 翻转贴图
                        }
                    }
                    else
                    {
                        if (Projectile.ai[0] > 0 && Projectile.ai[0] < 40 && n >= 0 && n < Main.npc.Length)
                        {
                            if (Main.npc[n] != null && Projectile.Center.X <= Main.npc[n].Center.X)
                            {
                                flag = true;
                            }
                            if (Main.npc[n] != null && Projectile.Center.X >= Main.npc[n].Center.X)
                            {
                                flag = false;
                            }
                        }
                        if (flag)
                        {
                            rotationOffset = 0f - Flyingsword.ExtraDrawRotation;
                            effects = SpriteEffects.None; // 贴图不翻转
                        }
                        else
                        {
                            rotationOffset = MathHelper.ToRadians(90f) + Flyingsword.ExtraDrawRotation; // 旋转偏移135度
                            effects = SpriteEffects.FlipHorizontally; // 翻转贴图
                        }
                    }
                }
                Color LightsColor = cachedAverageColor;
                var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
                var v3 = Main.rgbToHsl(LightsColor);
                v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
                var c = Main.hslToRgb(v3);
                c.A = 0;
                if(Flyingsword.IsTail)
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
                            var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j) - MathHelper.PiOver2 + MathHelper.PiOver4;
                            Main.EntitySpriteDraw(texture_,
                                                  oldcenter,
                                                  rectangle,
                                                  MyColor * factor/* * Projectile.alpha*/,
                                                  oldRo + rotationOffset,
                                                  new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                                  Projectile.scale * 1.5f * factor,
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
                    Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4 + rotationOffset,
                    new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                    Projectile.scale * 1.5f,
                    effects,
                    0
                    );
                #region 以下：渐变高光
                if(Flyingsword.IsTail)
                for (int i = 0; i < 3; i++)
                {
                    Main.EntitySpriteDraw(texture_,
                                          Projectile.Center - Main.screenPosition,
                                          rectangle,
                                          c * value * 0.6f /** Projectile.alpha*/,
                                          Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4 + rotationOffset,
                                          new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                          Projectile.scale * 1.5f,
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
    class BuffsFlyingsword : ModBuff
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/AGlobalControl/BuffsFlyingControl";
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingswordProj>()] > 0)
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
            int weaponType = player.GetModPlayer<FlyingSwordPlayer>().LastSummonWeaponType;
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
    public class FlyingSwordPlayer : ModPlayer
    {
        public int LastSummonWeaponType = 0;
    }
}