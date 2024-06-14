using Comfort.Common;
using EFT.Airdrop;
using EFT.Interactive;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Fika.Core.Coop.Airdrops
{
    /// <summary>
    /// Created by: SPT team
    /// Link: https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.Custom/Airdrops/AirdropBox.cs
    /// </summary>
    public class FikaAirdropBox : MonoBehaviour
    {
        private const string CRATE_PATH = "assets/content/location_objects/lootable/prefab/scontainer_crate.bundle";
        private const string AIRDROP_SOUNDS_PATH = "assets/content/audio/prefabs/airdrop/airdropsounds.bundle";
        private readonly int CROSSFADE = Shader.PropertyToID("_Crossfade");
        private readonly int COLLISION = Animator.StringToHash("collision");

        public LootableContainer Container { get; set; }
        private float fallSpeed;
        private AirdropSynchronizableObject boxSync;
        private AirdropLogicClass boxLogic;
        private Material paraMaterial;
        private Animator paraAnimator;
        private AirdropSurfaceSet surfaceSet;
        private Dictionary<BaseBallistic.ESurfaceSound, AirdropSurfaceSet> soundsDictionary;
        private BetterSource audioSource;

        private BetterSource AudioSource
        {
            get
            {
                if (audioSource != null) return audioSource;

                audioSource = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Environment, false);
                audioSource.transform.parent = transform;
                audioSource.transform.localPosition = Vector3.up;

                return audioSource;
            }
        }

        public static async Task<FikaAirdropBox> Init(float crateFallSpeed)
        {
            FikaAirdropBox instance = (await LoadCrate()).AddComponent<FikaAirdropBox>();
            instance.soundsDictionary = await LoadSounds();

            instance.Container = instance.GetComponentInChildren<LootableContainer>();
            instance.Container.Id = "FikaAirdropContainer";

            instance.boxSync = instance.GetComponent<AirdropSynchronizableObject>();
            instance.boxLogic = new AirdropLogicClass();
            instance.boxSync.SetLogic(instance.boxLogic);

            instance.paraAnimator = instance.boxSync.Parachute.GetComponent<Animator>();
            instance.paraMaterial = instance.boxSync.Parachute.GetComponentInChildren<Renderer>().material;
            instance.fallSpeed = crateFallSpeed;
            return instance;
        }

        private static async Task<GameObject> LoadCrate()
        {
            IEasyAssets easyAssets = Singleton<PoolManager>.Instance.EasyAssets;
            await easyAssets.Retain(CRATE_PATH, null, null).LoadingJob;

            GameObject crate = Instantiate(easyAssets.GetAsset<GameObject>(CRATE_PATH));
            crate.SetActive(false);
            return crate;
        }

        private static async Task<Dictionary<BaseBallistic.ESurfaceSound, AirdropSurfaceSet>> LoadSounds()
        {
            IEasyAssets easyAssets = Singleton<PoolManager>.Instance.EasyAssets;
            await easyAssets.Retain(AIRDROP_SOUNDS_PATH, null, null).LoadingJob;

            Dictionary<BaseBallistic.ESurfaceSound, AirdropSurfaceSet> soundsDictionary = new Dictionary<BaseBallistic.ESurfaceSound, AirdropSurfaceSet>();
            AirdropSurfaceSet[] sets = easyAssets.GetAsset<AirdropSounds>(AIRDROP_SOUNDS_PATH).Sets;
            foreach (AirdropSurfaceSet set in sets)
            {
                if (!soundsDictionary.ContainsKey(set.Surface))
                {
                    soundsDictionary.Add(set.Surface, set);
                }
                else
                {
                    Debug.LogError(set.Surface + " surface sounds are duplicated");
                }
            }

            return soundsDictionary;
        }

        public IEnumerator DropCrate(Vector3 position)
        {
            RaycastBoxDistance(LayerMaskClass.TerrainLowPoly, out RaycastHit hitInfo, position);
            SetLandingSound();
            boxSync.Init(1, position, Vector3.zero);
            PlayAudioClip(boxSync.SqueakClip, true);

            if (hitInfo.distance < 155f)
            {
                for (float i = 0; i < 1; i += Time.deltaTime / 6f)
                {
                    transform.position = Vector3.Lerp(position, hitInfo.point, i * i);
                    yield return null;
                }

                transform.position = hitInfo.point;
            }
            else
            {
                Vector3 parachuteOpenPos = position + new Vector3(0f, -148.2f, 0f);
                for (float i = 0; i < 1; i += Time.deltaTime / 5.5f)
                {
                    transform.position = Vector3.Lerp(position, parachuteOpenPos, i * i);
                    yield return null;
                }
                OpenParachute();
                while (RaycastBoxDistance(LayerMaskClass.TerrainLowPoly, out _))
                {
                    transform.Translate(Vector3.down * (Time.deltaTime * fallSpeed));
                    transform.Rotate(Vector3.up, Time.deltaTime * 6f);
                    yield return null;
                }
                transform.position = hitInfo.point;
                CloseParachute();
            }

            OnBoxLand(out float clipLength);
            yield return new WaitForSecondsRealtime(clipLength + 0.5f);
            ReleaseAudioSource();
        }

        private void OnBoxLand(out float clipLength)
        {
            AudioClip landingClip = surfaceSet.LandingSoundBank.PickSingleClip(surfaceSet.LandingSoundBank.GetRandomClipIndex(2));
            clipLength = landingClip.length;
            boxSync.AirdropDust.SetActive(true);
            boxSync.AirdropDust.GetComponent<ParticleSystem>().Play();
            AudioSource.source1.Stop();
            PlayAudioClip(new TaggedClip
            {
                Clip = landingClip,
                Falloff = (int)surfaceSet.LandingSoundBank.Rolloff,
                Volume = surfaceSet.LandingSoundBank.BaseVolume
            });

            if (FikaBackendUtils.IsServer)
            {
                AddNavMeshObstacle();
            }
        }

        private void AddNavMeshObstacle()
        {
            NavMeshObstacle navMeshObstacle = this.GetOrAddComponent<NavMeshObstacle>();
            navMeshObstacle.size = boxSync.CollisionCollider.bounds.size;
            navMeshObstacle.carving = true;
        }

        private bool RaycastBoxDistance(LayerMask layerMask, out RaycastHit hitInfo)
        {
            return RaycastBoxDistance(layerMask, out hitInfo, transform.position);
        }

        private bool RaycastBoxDistance(LayerMask layerMask, out RaycastHit hitInfo, Vector3 origin)
        {
            Ray ray = new Ray(origin, Vector3.down);

            bool raycast = Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask);
            if (!raycast) return false;

            return hitInfo.distance > 0.05f;
        }

        private void SetLandingSound()
        {
            if (!RaycastBoxDistance(LayerMaskClass.AudioControllerStepLayerMask, out RaycastHit raycast))
            {
                Debug.LogError("Raycast to ground returns no hit. Choose Concrete sound landing set");
                surfaceSet = soundsDictionary[BaseBallistic.ESurfaceSound.Concrete];
            }
            else
            {
                if (raycast.collider.TryGetComponent(out BaseBallistic component))
                {
                    BaseBallistic.ESurfaceSound surfaceSound = component.GetSurfaceSound(raycast.point);
                    if (soundsDictionary.ContainsKey(surfaceSound))
                    {
                        surfaceSet = soundsDictionary[surfaceSound];
                        return;
                    }
                }

                surfaceSet = soundsDictionary[BaseBallistic.ESurfaceSound.Concrete];
            }
        }

        private void PlayAudioClip(TaggedClip clip, bool looped = false)
        {
            float volume = clip.Volume;
            UnityEngine.Audio.AudioMixerGroup occlusionGroupSimple = Singleton<BetterAudio>.Instance.GetOcclusionGroupSimple(transform.position, ref volume);
            AudioSource.gameObject.SetActive(true);
            AudioSource.source1.outputAudioMixerGroup = occlusionGroupSimple;
            AudioSource.source1.spatialBlend = 1f;
            AudioSource.SetRolloff(clip.Falloff);
            AudioSource.source1.volume = volume;

            if (AudioSource.source1.isPlaying) return;

            AudioSource.source1.clip = clip.Clip;
            AudioSource.source1.loop = looped;
            AudioSource.source1.Play();
        }

        private void OpenParachute()
        {
            boxSync.Parachute.SetActive(true);
            paraAnimator.SetBool(COLLISION, false);
            StartCoroutine(CrossFadeAnimation(1f));
        }

        private void CloseParachute()
        {
            paraAnimator.SetBool(COLLISION, true);
            StartCoroutine(CrossFadeAnimation(0f));
        }

        private IEnumerator CrossFadeAnimation(float targetFadeValue)
        {
            float curFadeValue = paraMaterial.GetFloat(CROSSFADE);
            for (float i = 0; i < 1; i += Time.deltaTime / 2f)
            {
                paraMaterial.SetFloat(CROSSFADE, Mathf.Lerp(curFadeValue, targetFadeValue, i * i));
                yield return null;
            }
            paraMaterial.SetFloat(CROSSFADE, targetFadeValue);

            if (targetFadeValue == 0f)
            {
                boxSync.Parachute.SetActive(false);
            }
        }

        private void ReleaseAudioSource()
        {
            if (audioSource == null) return;

            audioSource.transform.parent = null;
            audioSource.Release();
            audioSource = null;
        }
    }
}