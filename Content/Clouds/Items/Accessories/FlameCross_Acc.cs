using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SwordMastery.Content.Items.Weapons.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static SwordMastery.Content.Items.Accessories.FlameCrossDebuffNPC;

namespace SwordMastery.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Waist)]
    public class FlameCross_Acc : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30; // 饰品宽度
            Item.height = 40; // 饰品高度
            Item.value = Item.sellPrice(0, 0, 2, 0); // 商店售卖价格
            Item.rare = ItemRarityID.Orange; // 稀有度
            Item.accessory = true; // 设为装备
            Item.defense = 2; // 防御力加成
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                tooltips.Add(new TooltipLine(Mod, "", $"按下 {FlameCross_ACC_System.FlameCrossKeybind.GetAssignedKeys().FirstOrDefault() ?? "未绑定"} 进行裁决！"));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "", $"Press the {FlameCross_ACC_System.FlameCrossKeybind.GetAssignedKeys().FirstOrDefault() ?? "unbound"} key to make a judgment"));
            }
            var cooldown = Main.LocalPlayer.GetModPlayer<FlameCross_AccPlayer>().flameCrossCooldown;
            if (cooldown > 0)
            {
                int seconds = cooldown / 60;
                int min = seconds / 60;
                int sec = seconds % 60;
                string text = $"冷却中: {min:D2}:{sec:D2}";
                tooltips.Add(new TooltipLine(Mod, "FlameCrossCooldown", text) { OverrideColor = Color.OrangeRed });
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "FlameCrossReady", "圣焰裁决已就绪") { OverrideColor = Color.Orange });
            }
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<FlameCross_AccPlayer>().hasFlameCrossAcc = true;
        }
    }
    public class FlameCross_AccPlayer : ModPlayer
    {
        public bool hasFlameCrossAcc = false;
        public int flameCrossCooldown = 0; // 单位：帧

        public override void SaveData(TagCompound tag)
        {
            tag["flameCrossCooldown"] = flameCrossCooldown;
        }

        public override void LoadData(TagCompound tag)
        {
            flameCrossCooldown = tag.GetInt("flameCrossCooldown");
        }

        public override void ResetEffects()
        {
            hasFlameCrossAcc = false;
        }

        public override void PostUpdate()
        {
            int lastCooldown = flameCrossCooldown;
            if (flameCrossCooldown > 0)
                flameCrossCooldown--;

            // 冷却刚好归零时播放音效
            if (lastCooldown > 0 && flameCrossCooldown == 0 && Main.myPlayer == Player.whoAmI)
            {
                SoundEngine.PlaySound(SoundID.MaxMana);
                Player.GetModPlayer<FlameCrossPlayer>().flameBlessingTimer = 45;
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // 检查是否装备饰品且按下V键
            if (hasFlameCrossAcc
                && FlameCross_ACC_System.FlameCrossKeybind.Current
                )
            {
                if (!Player.GetModPlayer<FlameCross_AccPlayer>().lastVPressed)
                {
                    // 冷却中不能使用
                    if (flameCrossCooldown <= 0)
                    {
                        Projectile.NewProjectile(
                            Player.GetSource_Misc("FlameCrossAcc_Acc"),
                            Player.Center,
                            new Vector2(0, -8),
                            ModContent.ProjectileType<FlameCross_Proj>(),
                            1,
                            0f,
                            Player.whoAmI
                        );
                        flameCrossCooldown = 60 * 60 * 10; // 10分钟冷却（60帧*60秒*10）
                    }
                }
                Player.GetModPlayer<FlameCross_AccPlayer>().lastVPressed = true;
            }
            else
            {
                Player.GetModPlayer<FlameCross_AccPlayer>().lastVPressed = false;
            }
        }
        private bool lastVPressed = false;
    }
    public class FlameCross_ACC_System : ModSystem
    {
        public static ModKeybind FlameCrossKeybind; // 添加自定义按键绑定

        public override void Load()
        {
            FlameCrossKeybind = KeybindLoader.RegisterKeybind(Mod, Language.ActiveCulture.Name == "zh-Hans" ? "圣焰裁决" : "Holy Flame Judgment", "V"); // 注册自定义按键绑定，默认是 V 键
        }

        public override void Unload()
        {
            FlameCrossKeybind = null;
        }
    }
    public class FlameCross_Proj : ModProjectile
    {
        private const int GatherTime = 120; // 2秒
        private const float BallSize = 30f;
        private bool launched = false;
        public override string Texture => "SwordMastery/Content/Items/Accessories/FlameCross_Acc";
        private readonly Player player = Main.player[Main.myPlayer];
        public override void SetStaticDefaults()
        {
            // 可选：显示名称
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }
        public override bool? CanDamage()
        {
            return launched;// 只在发射后才能对NPC造成伤害
        }
        public override void AI()
        {
            Vector2 playerCenter = player.Center;
            if (player.dead) Projectile.Kill();
            // 1. 前2秒：粒子向中心汇聚
            if (Projectile.ai[0] < GatherTime)
            {
                Projectile.Center = playerCenter;
                if (Main.myPlayer == Projectile.owner)
                {
                    int reduce = (int)(player.statLifeMax2 * 0.006f);
                    if (reduce < 1) reduce = 1;
                    player.statLife -= reduce;

                    // 如果生命值为0或更低，直接杀死玩家
                    if (player.statLife <= 0)
                        player.KillMe(PlayerDeathReason.ByCustomReason($"{player.name}被圣焰吞噬了！"), 9999, 0);

                    int swirlLayers = 8;
                    int baseDustCount = 18;
                    float maxRadius = 800f;
                    float minRadius = 400f;
                    float t_ = MathHelper.Clamp(Projectile.ai[0] / GatherTime, 0f, 1f);

                    for (int layer = 0; layer < swirlLayers; layer++)
                    {
                        float swirlSpeed = 2.5f + layer * 0.5f;
                        float swirlTightness = 0.6f + layer * 0.12f;

                        float layerStartRadius = MathHelper.Lerp(maxRadius, minRadius, layer / (float)(swirlLayers - 1));
                        float baseRadius = MathHelper.Lerp(layerStartRadius, 0f, t_);

                        // 让粒子数量与半径的1.5次方成正比，外圈更密集
                        int dustCount = Math.Max((int)(baseDustCount * Math.Pow(baseRadius / maxRadius, 1.5)), 2);

                        for (int i = 0; i < dustCount; i++)
                        {
                            float baseAngle = MathHelper.TwoPi * i / dustCount;
                            float swirlAngle = baseAngle + swirlSpeed * Projectile.ai[0] * 0.08f + layer * 0.3f;
                            float dist = baseRadius + Main.rand.NextFloat(-4f, 4f);

                            Vector2 spawnPos = player.Center + swirlAngle.ToRotationVector2() * dist;
                            Vector2 toCenter = (player.Center - spawnPos).SafeNormalize(Vector2.Zero);
                            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);
                            float tangentFactor = swirlTightness * (1f - t_) * Main.rand.NextFloat(1.1f, 2.0f);
                            float toCenterFactor = MathHelper.Lerp(2.5f, 5.5f, t_);
                            Vector2 velocity = toCenter * toCenterFactor + tangent * tangentFactor;

                            Color color = Color.Lerp(Color.Orange, Color.White, layer / (float)swirlLayers);
                            float scale = 1.0f + layer * 0.08f;

                            Dust dust = Dust.NewDustPerfect(spawnPos, DustID.Torch, velocity, 100, color, scale);
                            dust.noGravity = true;
                            dust.alpha = 100 + layer * 18;
                        }
                    }
                }
                Projectile.ai[0]++;
                return;
            }

            // 2. 2秒后：小球出现并移动到圆周上
            float t = Utils.GetLerpValue(GatherTime, GatherTime + 30, Projectile.ai[0], true); // 平滑出现
            Vector2 mouseWorld = Main.MouseWorld;
            Vector2 dirToMouse = (mouseWorld - playerCenter).SafeNormalize(Vector2.UnitY);
            Vector2 targetPos = playerCenter + dirToMouse * 40;

            // 3. 粒子小球效果
            if (Main.myPlayer == Projectile.owner)
            {
                int dustCount = 30;
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(BallSize / 2, BallSize / 2);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch, Vector2.Zero, 100, Color.Orange, 1.1f);
                    dust.noGravity = true;
                }
            }

            // 4. 旋转小球朝向
            Projectile.rotation = dirToMouse.ToRotation();

            // 5. 检测鼠标右键发射
            if (!launched && Main.myPlayer == Projectile.owner && Main.mouseRight && Main.mouseRightRelease)
            {
                SoundEngine.PlaySound(SoundID.Item100);
                launched = true;
                Vector2 shootDir = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitY);
                float speed = 16f;
                Projectile.velocity = shootDir * speed;
                Projectile.tileCollide = true;
                Projectile.netUpdate = true;
            }

            // 6. 发射后行为
            if (launched)
            {
                // 粒子拖尾
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.2f, 100, Color.Orange, 1.1f);
                    dust.noGravity = true;
                }
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Vector2.Lerp(playerCenter, targetPos, t);
            }

            Projectile.ai[0]++;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            Main.instance.CameraModifiers.Add(new FlameCrossCameraModifier(target.Center, 140, FullName));
            var global = target.GetGlobalNPC<FlameCrossDebuffNPC>();
            global.flameMarkTime = 120; // 持续2秒
            global.target = target;
        }

        [System.Obsolete]
        public override void Kill(int timeLeft)
        {
            base.Kill(timeLeft);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class FlameCrossDebuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        // 自定义标记字段
        public bool flameMarked = false;
        public int flameMarkTime = 0;
        public NPC target = null;

        public override void ResetEffects(NPC npc)
        {
            if (flameMarkTime <= 0)
            {
                flameMarked = false;
                target = null;
            }
        }
        public override void AI(NPC npc)
        {
            if (target != null && flameMarkTime > 0)
            {
                flameMarked = true;
                flameMarkTime--;
                if (target.velocity.Length() == 0)
                    target.position = npc.oldPosition;
                else target.velocity = Vector2.Zero;
                target.frameCounter = 0; // 重置动画帧计数器
                target.timeLeft = 0; // 重置动画持续时间
                target.netUpdate = true; // 通知客户端更新

                // 粒子特效
                int dustCount = 10;
                for (int i = 0; i < dustCount; i++)
                {
                    int dustIndex = Dust.NewDust(target.position, target.width, target.height, DustID.Torch);
                    Main.dust[dustIndex].velocity *= 0.2f;
                    Main.dust[dustIndex].scale *= 1.5f;
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity.Y -= 5f;
                    Main.dust[dustIndex].color = Color.Red;
                }

                // 玩家无敌，不能受到伤害
                // 这里假设只让最近的玩家无敌（你也可以指定特定玩家）
                Player player = Main.player[Player.FindClosest(npc.Center, npc.width, npc.height)];
                if (player.active && !player.dead)
                {
                    player.immune = true;
                    player.immuneTime = 30; // 每帧刷新，保证持续无敌
                }
            }
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            var global = npc.GetGlobalNPC<FlameCrossDebuffNPC>();
            if (global.flameMarked)
            {
                int lose = (int)(npc.lifeMax * 0.02f);
                if (lose < 1) lose = 1;
                npc.lifeRegen = -lose * 60;
                damage = lose;
            }
        }
        public class FlameCrossCameraModifier : ICameraModifier
        {
            private readonly int framesToLast;
            private int framesElapsed;
            public Vector2 targetPosition;

            // 这确保了相同身份的其他修饰器不会同时运行
            public string UniqueIdentity { get; private set; }
            public bool Finished { get; private set; }

            public FlameCrossCameraModifier(Vector2 position, int frames, string uniqueIdentity = null)
            {
                targetPosition = position - new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
                framesToLast = frames;
                UniqueIdentity = uniqueIdentity;
            }

            public void Update(ref CameraInfo cameraInfo)
            {
                // 使相机平滑地从起始位置移动到目标位置，然后再移回起始位置：
                // 我们将使用进度来确定相机应该根据已经过去的时间位于何处。
                float progress = Utils.GetLerpValue(0, framesToLast, framesElapsed);
                // 等价于 "(float)framesElapsed / framesToLast"
                // 有许多方法可以在两个值之间进行插值，例如使用以下任何一种方法：
                // MathF.Sin, MathHelper.Lerp, MathHelper.SmoothStep, Utils.MultiLerp, Utils.Turn01ToCyclic010
                // 每种方法都会导致不同的运动行为，例如加速度不同。
                // 在这个示例中，我们将使用 Remap 方法和 switch 表达式来实现分段线性插值
                // 在动画时间的前50%内，值将从0增加到1，
                // 然后在接下来的30%时间内保持为1，
                // 最后在动画时间的最后20%内快速返回到0
                // 更快到达目标和更快返回：前20%到达，后20%返回
                float lerpAmount = progress switch
                {
                    < 0.2f => Utils.Remap(progress, 0f, 0.2f, 0f, 1f),
                    > 0.8f => Utils.Remap(progress, 0.8f, 1f, 1f, 0f),
                    _ => 1f, // 中间60%停留
                };
                //float lerpAmount = progress switch
                //{
                //    < 0.5f => Utils.Remap(progress, 0, 0.5f, 0, 1),
                //    > 0.8f => Utils.Remap(progress, 0.8f, 1f, 1, 0),
                //    _ => 1, // progress 在0.5到0.8之间
                //};
                cameraInfo.CameraPosition = Vector2.Lerp(cameraInfo.CameraPosition, targetPosition, lerpAmount);

                // 如果游戏最小化或暂停，则暂停效果
                if (!Main.gameInactive && !Main.gamePaused)
                {
                    framesElapsed++;
                }
                if (framesElapsed >= framesToLast)
                {
                    Finished = true;
                }
            }
        }
    }
}
