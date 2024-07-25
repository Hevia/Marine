using RoR2.Projectile;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using HenryMod.Modules;
using RoR2;
using EntityStates;

namespace HenryMod.Survivors.Henry.SkillStates
{
    public class ShoulderBash : BaseSkillState
    {
        public static float baseDuration = 0.65f;
        public static float chargeDamageCoefficient = 4.5f;
        public static float knockbackDamageCoefficient = 7f;
        public static float massThresholdForKnockback = 150;
        public static float knockbackForce = 24f;
        public static float smallHopVelocity = 12f;

        public static float initialSpeedCoefficient = 6f;
        public static float finalSpeedCoefficient = 0.1f;

        private float dashSpeed;
        private Vector3 forwardDirection;
        private Vector3 previousPosition;

        private bool shieldCancel;
        private float duration;
        private float hitPauseTimer;
        private OverlapAttack attack;
        private bool inHitPause;
        private List<HurtBox> victimsStruck = new List<HurtBox>();

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = ShoulderBash.baseDuration;

            base.characterBody.isSprinting = true;


            // if (!base.HasBuff(EnforcerPlugin.Modules.Buffs.skateboardBuff))
            base.PlayAnimation("FullBody, Override", "ShoulderBash");//, "ShoulderBash.playbackRate", this.duration

            if (base.isAuthority && base.inputBank && base.characterDirection)
            {
               this.forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
                
            }

            this.RecalculateSpeed();

            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity.y *= 0.2f;
                base.characterMotor.velocity = this.forwardDirection * this.dashSpeed;
            }

            Vector3 b = base.characterMotor ? base.characterMotor.velocity : Vector3.zero;
            this.previousPosition = base.transform.position - b;

            HitBoxGroup hitBoxGroup = null;
            Transform modelTransform = base.GetModelTransform();

            if (modelTransform)
            {
                hitBoxGroup = Array.Find<HitBoxGroup>(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "SwordGroup");
            }

            this.attack = new OverlapAttack();
            this.attack.attacker = base.gameObject;
            this.attack.inflictor = base.gameObject;
            this.attack.teamIndex = base.GetTeam();
            this.attack.damage = ShoulderBash.chargeDamageCoefficient * this.damageStat;
            //this.attack.hitEffectPrefab = Loader.SwingChargedFist.overchargeImpactEffectPrefab;
            this.attack.forceVector = Vector3.up * 5f; //Toolbot.ToolbotDash.upwardForceMagnitude;
            this.attack.pushAwayForce = 10f; //Toolbot.ToolbotDash.awayForceMagnitude;
            this.attack.hitBoxGroup = hitBoxGroup;
            this.attack.isCrit = base.RollCrit();
        }

        private void RecalculateSpeed()
        {
            this.dashSpeed = (4 + (0.25f * this.moveSpeedStat)) * Mathf.Lerp(ShoulderBash.initialSpeedCoefficient, ShoulderBash.finalSpeedCoefficient, base.fixedAge / this.duration);
        }

        public override void OnExit()
        {
            if (base.characterBody)
            {
                if (this.shieldCancel) base.characterBody.isSprinting = false;
                else base.characterBody.isSprinting = true;
            }

            if (base.characterMotor) base.characterMotor.disableAirControlUntilCollision = false;// this should be a thing on all movement skills tbh

            //if (base.skillLocator) base.skillLocator.secondary.skillDef.activationStateMachineName = "Weapon";

            if (base.cameraTargetParams)
            {
                base.cameraTargetParams.fovOverride = -1f;
            }

            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.characterBody.isSprinting = true;

            if (base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
                return;
            }

            this.RecalculateSpeed();

            if (base.cameraTargetParams)
            {
               // base.cameraTargetParams.fovOverride = Mathf.Lerp(Commando.DodgeState.dodgeFOV, 60f, base.fixedAge / this.duration);
            }

            if (base.isAuthority)
            {
                if (base.skillLocator && base.inputBank)
                {
                    if (base.inputBank.skill4.down && base.fixedAge >= 0.4f * this.duration)
                    {
                        this.shieldCancel = true;
                        base.characterBody.isSprinting = false;
                        base.skillLocator.special.ExecuteIfReady();
                    }
                }

                if (!this.inHitPause)
                {
                    Vector3 normalized = (base.transform.position - this.previousPosition).normalized;

                    if (base.characterDirection)
                    {
                        if (normalized != Vector3.zero)
                        {
                            Vector3 vector = normalized * this.dashSpeed;
                            float d = Mathf.Max(Vector3.Dot(vector, this.forwardDirection), 0f);
                            vector = this.forwardDirection * d;
                            vector.y = base.characterMotor.velocity.y;
                            base.characterMotor.velocity = vector;
                        }

                        base.characterDirection.forward = this.forwardDirection;
                    }

                    this.previousPosition = base.transform.position;

                    this.attack.damage = this.damageStat * ShoulderBash.chargeDamageCoefficient;

                    if (this.attack.Fire(this.victimsStruck))
                    {
                        //Util.PlaySound(Sounds.ShoulderBashHit, EnforcerPlugin.VRAPICompat.IsLocalVRPlayer(characterBody) ? EnforcerPlugin.VRAPICompat.GetShieldMuzzleObject() : gameObject);
                        this.inHitPause = true;
                        this.hitPauseTimer = 1f;// Toolbot.ToolbotDash.hitPauseDuration;
                        //base.AddRecoil(-0.5f * Toolbot.ToolbotDash.recoilAmplitude, -0.5f * Toolbot.ToolbotDash.recoilAmplitude, -0.5f * Toolbot.ToolbotDash.recoilAmplitude, 0.5f * Toolbot.ToolbotDash.recoilAmplitude);

                        for (int i = 0; i < this.victimsStruck.Count; i++)
                        {
                            float mass = 0f;
                            HealthComponent healthComponent = this.victimsStruck[i].healthComponent;
                            CharacterMotor characterMotor = healthComponent.GetComponent<CharacterMotor>();
                            if (characterMotor)
                            {
                                mass = characterMotor.mass;
                            }
                            else
                            {
                                Rigidbody rigidbody = healthComponent.GetComponent<Rigidbody>();
                                if (rigidbody)
                                {
                                    mass = rigidbody.mass;
                                }
                            }
                            if (mass >= ShoulderBash.massThresholdForKnockback)
                            {
                                this.outer.SetNextState(new ShoulderBashImpact
                                {
                                    victimHealthComponent = healthComponent,
                                    idealDirection = this.forwardDirection,
                                    isCrit = this.attack.isCrit
                                });
                                return;
                            }
                        }
                        return;
                    }
                }
                else
                {
                    base.characterMotor.velocity = Vector3.zero;
                    this.hitPauseTimer -= Time.fixedDeltaTime;
                    if (this.hitPauseTimer < 0f)
                    {
                        this.inHitPause = false;
                    }
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (this.shieldCancel) return InterruptPriority.Any;
            else return InterruptPriority.Frozen;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.forwardDirection);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.forwardDirection = reader.ReadVector3();
        }
    }

    public class ShoulderBashImpact : BaseState
    {
        public HealthComponent victimHealthComponent;
        public Vector3 idealDirection;
        public bool isCrit;

        public static float baseDuration = 0.35f;
        public static float recoilAmplitude = 4.5f;

        private float duration;
        private bool shieldCancel;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = ShoulderBashImpact.baseDuration / this.attackSpeedStat;
            base.SmallHop(base.characterMotor, ShoulderBash.smallHopVelocity);

            //Util.PlayAttackSpeedSound(Sounds.ShoulderBashHit, EnforcerPlugin.VRAPICompat.IsLocalVRPlayer(characterBody) ? EnforcerPlugin.VRAPICompat.GetShieldMuzzleObject() : gameObject, 0.5f);

            if (NetworkServer.active)
            {
                if (this.victimHealthComponent)
                {
                    DamageInfo damageInfo = new DamageInfo
                    {
                        attacker = base.gameObject,
                        damage = this.damageStat * ShoulderBash.knockbackDamageCoefficient,
                        crit = this.isCrit,
                        procCoefficient = 1f,
                        damageColorIndex = DamageColorIndex.Item,
                        damageType = DamageType.Stun1s,
                        position = base.characterBody.corePosition
                    };

                    this.victimHealthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, this.victimHealthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, this.victimHealthComponent.gameObject);
                }
            }

            if (base.characterMotor) base.characterMotor.velocity = this.idealDirection * -ShoulderBash.knockbackForce;

            if (base.isAuthority)
            {
                base.AddRecoil(-0.5f * ShoulderBashImpact.recoilAmplitude * 3f, -0.5f * ShoulderBashImpact.recoilAmplitude * 3f, -0.5f * ShoulderBashImpact.recoilAmplitude * 8f, 0.5f * ShoulderBashImpact.recoilAmplitude * 3f);
               // EffectManager.SimpleImpactEffect(Loader.SwingZapFist.overchargeImpactEffectPrefab, this.victimHealthComponent.transform.position, base.characterDirection.forward, true);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.inputBank && base.isAuthority)
            {
                if (base.skillLocator && base.inputBank)
                {
                    if (base.inputBank.skill4.down && base.fixedAge >= 0.4f * this.duration)
                    {
                        this.shieldCancel = true;
                        base.characterBody.isSprinting = false;
                        base.skillLocator.special.ExecuteIfReady();
                    }
                }
            }

            if (base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.victimHealthComponent ? this.victimHealthComponent.gameObject : null);
            writer.Write(this.idealDirection);
            writer.Write(this.isCrit);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            GameObject gameObject = reader.ReadGameObject();
            this.victimHealthComponent = (gameObject ? gameObject.GetComponent<HealthComponent>() : null);
            this.idealDirection = reader.ReadVector3();
            this.isCrit = reader.ReadBoolean();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (this.shieldCancel) return InterruptPriority.Any;
            else return InterruptPriority.Frozen;
        }
    }
}