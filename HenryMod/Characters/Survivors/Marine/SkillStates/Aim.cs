using EntityStates;
using MarineMod.Characters.Survivors.Marine.SkillStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HenryMod.Survivors.Henry.SkillStates
{
    public class Aim : BaseSkillState
    {
        public static float baseDuration = 1.2f;
        public static float timeBetweenStocks = 1.2f;
        private float duration;
        private float timer;

        public SkillDef primaryOverride;
        public GenericSkill overriddenSkill;


        public SkillDef utilityOverride;
        public GenericSkill overriddenUtilitySkill;

        private NetworkStateMachine nsm;
        private EntityStateMachine weaponEsm;


        private bool isPressingCameraSwap;
        private bool useAltCamera = false;

        public static Vector3 chargeCameraPos = new Vector3(1.2f, -0.65f, -6.1f);
        public static Vector3 altCameraPos = new Vector3(-1.2f, -0.65f, -6.1f);


        public CameraTargetParams.CameraParamsOverrideHandle camOverrideHandle;
        private CharacterCameraParamsData chargeCameraParams = new CharacterCameraParamsData
        {
            maxPitch = 85f,
            minPitch = -85f,
            pivotVerticalOffset = 1f,
            idealLocalCameraPos = chargeCameraPos,
            wallCushion = 0.1f,
        };

        private CharacterCameraParamsData altCameraParams = new CharacterCameraParamsData
        {
            maxPitch = 85f,
            minPitch = -85f,
            pivotVerticalOffset = 1f,
            idealLocalCameraPos = altCameraPos,
            wallCushion = 0.1f,
        };


        public override void OnEnter()
        {
            base.OnEnter();

            // gets weapon state to make sure you aren't currently firing your gun
            nsm = GetComponent<NetworkStateMachine>();
            if (nsm != null)
                weaponEsm = nsm.stateMachines[1];

            if (inputBank.skill4.down)
            {
                outer.SetNextStateToMain();
                return;
            }

            duration = (baseDuration * skillLocator.secondary.cooldownScale);

            characterBody.SetAimTimer(2f);

            CameraSwap();

            GenericSkill primarySkill = skillLocator.primary;

            primaryOverride = SkillStore.rifleSkill;

            if (primarySkill && primaryOverride)
            {
                if (!overriddenSkill)
                {
                    overriddenSkill = primarySkill;
                    overriddenSkill.SetSkillOverride(primarySkill, primaryOverride, GenericSkill.SkillOverridePriority.Replacement);
                }
            }

            GenericSkill utilitySkill = skillLocator.utility;

            utilityOverride = SkillStore.bashSkill;

            if (utilitySkill && utilityOverride)
            {
                if (!overriddenUtilitySkill)
                {
                    overriddenUtilitySkill = utilitySkill;
                    overriddenUtilitySkill.SetSkillOverride(utilitySkill, utilityOverride, GenericSkill.SkillOverridePriority.Replacement);
                }
            }

            // TOOD: Remember to slow Marine down while aiming
            if (NetworkServer.active)
            {
                characterBody.AddBuff(HenryBuffs.armorBuff);
            }
        }

        private void CameraSwap()
        {
            if (useAltCamera)
            {
                cameraTargetParams.RemoveParamsOverride(camOverrideHandle, 0.2f);

                CameraTargetParams.CameraParamsOverrideRequest request = new CameraTargetParams.CameraParamsOverrideRequest
                {
                    cameraParamsData = altCameraParams,
                    priority = 0f
                };

                camOverrideHandle = cameraTargetParams.AddParamsOverride(request, 0.2f);
            }
            else
            {
                cameraTargetParams.RemoveParamsOverride(camOverrideHandle, 0.2f);

                CameraTargetParams.CameraParamsOverrideRequest request = new CameraTargetParams.CameraParamsOverrideRequest
                {
                    cameraParamsData = chargeCameraParams,
                    priority = 0f
                };

                camOverrideHandle = cameraTargetParams.AddParamsOverride(request, 0.2f);
            }
        }

        public override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.V) && isAuthority)
            {
                useAltCamera = !useAltCamera;
                CameraSwap();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            characterBody.isSprinting = false;

            if (isAuthority)
            {
                if (!inputBank.skill2.down || inputBank.skill3.down || inputBank.skill4.down)
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }

            if (weaponEsm.IsInMainState())
                timer += Time.fixedDeltaTime;
            else
                timer = 0f;

            if (timer >= (timeBetweenStocks * skillLocator.secondary.cooldownScale) / attackSpeedStat && characterBody.skillLocator.secondary.stock < characterBody.skillLocator.secondary.maxStock)
            {
                timer = 0f;

                skillLocator.secondary.AddOneStock();
                characterBody.SetAimTimer(timeBetweenStocks);
            }
        }


        public override void OnExit()
        {
            //PlayCrossfade("Gesture, Override", "BufferEmpty", "Secondary.playbackRate", duration, 0.3f);
            if (cameraTargetParams)
            {
                cameraTargetParams.RemoveParamsOverride(camOverrideHandle, 0.7f);
            }

            if (overriddenSkill)
            {
                overriddenSkill.UnsetSkillOverride(skillLocator.primary, primaryOverride, GenericSkill.SkillOverridePriority.Replacement);
            }

            if (overriddenUtilitySkill)
            {
                overriddenUtilitySkill.UnsetSkillOverride(skillLocator.utility, utilityOverride, GenericSkill.SkillOverridePriority.Replacement);
            }

            if (NetworkServer.active)
            {
                characterBody.RemoveBuff(HenryBuffs.armorBuff);
            }

            base.OnExit();
        }
    }
}
