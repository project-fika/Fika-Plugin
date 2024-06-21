using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.FreeCamera
{
    /// <summary>
    /// A simple free camera to be added to a Unity game object. <br/><br/>
    /// 
    /// Full credit to Ashley Davis on GitHub for the inital code:<br/>
    /// https://gist.github.com/ashleydavis/f025c03a9221bc840a2b<br/><br/>
    /// 
    /// This is HEAVILY based on Terkoiz's work found here. Thanks for your work Terkoiz! <br/>
    /// https://dev.sp-tarkov.com/Terkoiz/Freecam/raw/branch/master/project/Terkoiz.Freecam/FreecamController.cs
    /// </summary>
    public class FreeCamera : MonoBehaviour
    {
        public bool IsActive = false;
        private CoopPlayer CurrentPlayer;
        private bool isFollowing = false;
        private bool leftMode = false;
        private bool disableInput = false;
        private bool showOverlay;
        private NightVision nightVision;
        private ThermalVision thermalVision;
        private FreeCameraController freeCameraController;

        private KeyCode forwardKey = KeyCode.W;
        private KeyCode backKey = KeyCode.S;
        private KeyCode leftKey = KeyCode.A;
        private KeyCode rightKey = KeyCode.D;
        private KeyCode relUpKey = KeyCode.E;
        private KeyCode relDownKey = KeyCode.Q;
        private readonly KeyCode upKey = KeyCode.R;
        private readonly KeyCode downKey = KeyCode.F;

        protected void Start()
        {
            if (FikaPlugin.AZERTYMode.Value)
            {
                forwardKey = KeyCode.Z;
                backKey = KeyCode.S;
                leftKey = KeyCode.Q;
                rightKey = KeyCode.D;

                relUpKey = KeyCode.E;
                relDownKey = KeyCode.A;
            }

            showOverlay = FikaPlugin.KeybindOverlay.Value;
            FikaPlugin.KeybindOverlay.SettingChanged += KeybindOverlay_SettingChanged;

            nightVision = CameraClass.Instance.NightVision;
            thermalVision = CameraClass.Instance.ThermalVision;

            freeCameraController = Singleton<GameWorld>.Instance.gameObject.GetComponent<FreeCameraController>();
        }

        private void KeybindOverlay_SettingChanged(object sender, EventArgs e)
        {
            showOverlay = FikaPlugin.KeybindOverlay.Value;
        }

        protected void OnGUI()
        {
            if (IsActive && showOverlay)
            {
                string visionText = "Enable nightvision";

                if (nightVision != null && nightVision.On)
                {
                    visionText = "Enable thermals";
                }

                if (thermalVision != null && thermalVision.On)
                {
                    visionText = "Disable thermals";
                }

                GUILayout.BeginArea(new Rect(5, 5, 800, 800));
                GUILayout.BeginVertical();

                GUILayout.Label($"Left/Right Mouse Button: Jump between players");
                GUILayout.Label($"CTRL + Left/Right Mouse Button: Jump and spectate in 3rd person");
                GUILayout.Label($"Spacebar + Left/Right Mouse Button: Jump and spectate in head cam");
                GUILayout.Label($"T: Teleport to cam position");
                GUILayout.Label($"N: {visionText}");
                GUILayout.Label($"M: Disable culling");
                GUILayout.Label($"HOME: {(disableInput ? "Enable Input" : "Disable Input")}");
                GUILayout.Label($"Shift + Ctrl: Turbo Speed");

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        protected void Update()
        {
            if (!IsActive)
            {
                return;
            }

            // Toggle input
            if (Input.GetKeyDown(KeyCode.Home))
            {
                disableInput = !disableInput;
                NotificationManagerClass.DisplayMessageNotification($"Free cam input is now {(disableInput ? "disabled" : "enabled")}.");
            }

            if (disableInput)
            {
                return;
            }

            // Spectate next player
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                CoopHandler coopHandler = CoopHandler.GetCoopHandler();
                if (coopHandler == null)
                {
                    return;
                }

                List<CoopPlayer> players = [.. coopHandler.Players.Values.Where(x => !x.IsYourPlayer && x.gameObject.name.StartsWith("Player_") && x.HealthController.IsAlive)];

                if (players.Count > 0)
                {
                    bool shouldHeadCam = Input.GetKey(KeyCode.Space);
                    bool should3rdPerson = Input.GetKey(KeyCode.LeftControl);
                    foreach (CoopPlayer player in players)
                    {
                        if (CurrentPlayer == null && players[0] != null)
                        {
                            CurrentPlayer = players[0];
                            if (shouldHeadCam)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }

                        int nextPlayer = players.IndexOf(CurrentPlayer) + 1;

                        if (players.Count - 1 >= nextPlayer)
                        {
                            CurrentPlayer = players[nextPlayer];
                            if (shouldHeadCam)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }
                        else
                        {
                            CurrentPlayer = players[0];
                            if (shouldHeadCam)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }
                    }
                }
                else
                {
                    if (CurrentPlayer != null)
                    {
                        CurrentPlayer = null;
                    }
                    if (isFollowing)
                    {
                        isFollowing = false;
                        transform.parent = null;
                    }
                }
            }

            // Spectate previous player
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                CoopHandler coopHandler = CoopHandler.GetCoopHandler();
                if (coopHandler == null)
                {
                    return;
                }

                List<CoopPlayer> players = [.. coopHandler.Players.Values.Where(x => !x.IsYourPlayer && x.gameObject.name.StartsWith("Player_") && x.HealthController.IsAlive)];

                if (players.Count > 0)
                {
                    bool shouldFollow = Input.GetKey(KeyCode.Space);
                    bool should3rdPerson = Input.GetKey(KeyCode.LeftControl);
                    foreach (CoopPlayer player in players)
                    {
                        if (CurrentPlayer == null && players[0] != null)
                        {
                            CurrentPlayer = players[0];
                            if (shouldFollow)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }

                        int previousPlayer = players.IndexOf(CurrentPlayer) - 1;

                        if (previousPlayer >= 0)
                        {
                            CurrentPlayer = players[previousPlayer];
                            if (shouldFollow)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }
                        else
                        {
                            CurrentPlayer = players[players.Count - 1];
                            if (shouldFollow)
                            {
                                AttachToPlayer();
                            }
                            else if (should3rdPerson)
                            {
                                Attach3rdPerson();
                            }
                            else
                            {
                                JumpToPlayer();
                            }
                            break;
                        }
                    }
                }
                else
                {
                    if (CurrentPlayer != null)
                    {
                        CurrentPlayer = null;
                    }
                    if (isFollowing)
                    {
                        isFollowing = false;
                        transform.parent = null;
                    }
                }
            }

            // Toggle vision
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleVision();
            }

            // Disable culling
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (freeCameraController != null)
                {
                    freeCameraController.DisableAllCullingObjects(); 
                }
            }

            if (isFollowing)
            {
                if (CurrentPlayer != null)
                {
                    if (CurrentPlayer.MovementContext.LeftStanceEnabled && !leftMode)
                    {
                        SetLeftShoulderMode(true);
                    }
                    else if (!CurrentPlayer.MovementContext.LeftStanceEnabled && leftMode)
                    {
                        SetLeftShoulderMode(false);
                    }
                }
                return;
            }

            bool fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool superFastMode = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float movementSpeed = fastMode ? 20f : 2f;

            if (superFastMode)
            {
                movementSpeed *= 8;
            }

            if (Input.GetKey(leftKey) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position += -transform.right * (movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(rightKey) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position += transform.right * (movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(forwardKey) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += transform.forward * (movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(backKey) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position += -transform.forward * (movementSpeed * Time.deltaTime);
            }

            // Teleportation
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    return;
                }

                Player player = Singleton<GameWorld>.Instance.MainPlayer;

                if (!coopHandler.ExtractedPlayers.Contains(((CoopPlayer)player).NetId) && player.HealthController.IsAlive)
                {
                    player?.Teleport(transform.position);
                }
            }

            if (true)
            {
                if (Input.GetKey(relUpKey))
                {
                    transform.position += transform.up * (movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(relDownKey))
                {
                    transform.position += -transform.up * (movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(upKey) || Input.GetKey(KeyCode.PageUp))
                {
                    transform.position += Vector3.up * (movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(downKey) || Input.GetKey(KeyCode.PageDown))
                {
                    transform.position += -Vector3.up * (movementSpeed * Time.deltaTime);
                }
            }

            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * 3f;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * 3f;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        private void SetLeftShoulderMode(bool enabled)
        {
            if (enabled)
            {
                // Use different coordinates for headcam
                if (transform.localPosition.z == -0.17f)
                {
                    transform.localPosition = new(transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
                }
                else
                {
                    transform.localPosition = new(-transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
                }
                leftMode = true;

                return;
            }

            // Use different coordinates for headcam
            if (transform.localPosition.z == 0.17f)
            {
                transform.localPosition = new(transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
            }
            else
            {
                transform.localPosition = new(-transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
            }
            leftMode = false;
        }

        private void ToggleVision()
        {
            if (nightVision != null && thermalVision != null)
            {
                if (!nightVision.On && !thermalVision.On)
                {
                    nightVision.On = true;
                }
                else if (nightVision.On && !thermalVision.On)
                {
                    nightVision.On = false;
                    thermalVision.On = true;
                }
                else if (thermalVision.On)
                {
                    thermalVision.On = false;
                }
            }
        }

        public void JumpToPlayer()
        {
            transform.position = new Vector3(CurrentPlayer.Transform.position.x - 2, CurrentPlayer.Transform.position.y + 2, CurrentPlayer.Transform.position.z);
            transform.LookAt(new Vector3(CurrentPlayer.Transform.position.x, CurrentPlayer.Transform.position.y + 1, CurrentPlayer.Transform.position.z));
            if (isFollowing)
            {
                isFollowing = false;
                leftMode = false;
                transform.parent = null;
            }
        }

        public void AttachToPlayer()
        {
            transform.parent = CurrentPlayer.PlayerBones.Head.Original;
            transform.localPosition = new Vector3(-0.1f, -0.07f, -0.17f);
            transform.localEulerAngles = new Vector3(260, 80, 0);
            isFollowing = true;
        }

        public void Attach3rdPerson()
        {
            transform.parent = CurrentPlayer.RaycastCameraTransform;
            transform.localPosition = new Vector3(0.3f, 0.2f, -0.65f);
            transform.localEulerAngles = new Vector3(4.3f, 5.9f, 0f);
            isFollowing = true;
        }

        public void SetActive(bool status)
        {
            if (!status)
            {
                if (nightVision != null && nightVision.On)
                {
                    nightVision.method_1(false);
                }

                if (thermalVision != null && thermalVision.On)
                {
                    thermalVision.method_1(false);
                }
            }

            if (status)
            {
                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    if (player.NightVisionObserver.Component != null && player.NightVisionObserver.Component.Togglable.On)
                    {
                        player.NightVisionObserver.Component.Togglable.ForceToggle(false);
                    }

                    if (player.ThermalVisionObserver.Component != null && player.ThermalVisionObserver.Component.Togglable.On)
                    {
                        player.ThermalVisionObserver.Component.Togglable.ForceToggle(false);
                    }
                }
                else if (player != null && !player.HealthController.IsAlive)
                {
                    if (nightVision != null && nightVision.On)
                    {
                        nightVision.method_1(false);
                    }

                    if (thermalVision != null && thermalVision.On)
                    {
                        thermalVision.method_1(false);
                    }
                }
            }

            IsActive = status;
            isFollowing = false;
            leftMode = false;
            transform.parent = null;
        }

        protected void OnDestroy()
        {
            Destroy(this);
        }
    }
}