// Decompiled with JetBrains decompiler
// Type: TemplateGUI.TemplateGUI
// Assembly: DCM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 50D3356B-D964-482F-86F0-A1FDFD923388
// Assembly location: C:\Users\asher\Downloads\DCM.dll

using BepInEx;
using BepInEx.Configuration;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

#nullable disable
namespace TemplateGUI;

[BepInPlugin("com.dcm.gorillag", "// dcm \\", "2.5.1")]
public class TemplateGUI : BaseUnityPlugin
{
  private List<Texture2D> animatedBorderFrames = new List<Texture2D>();
  private ConfigEntry<FollowTarget> cameraFollowTargetConfig;
  private ConfigEntry<float> fovConfig;
  private ConfigEntry<float> nearClipConfig;
  private ConfigEntry<float> headDistanceConfig;
  private ConfigEntry<float> yOffsetConfig;
  private ConfigEntry<float> xOffsetConfig;
  private ConfigEntry<bool> firstPersonToggleConfig;
  private ConfigEntry<SmoothingType> smoothingTypeConfig;
  private ConfigEntry<float> positionSmoothingFactorConfig;
  private ConfigEntry<float> rotationSmoothingFactorConfig;
  private ConfigEntry<float> lerpingConfig;
  private ConfigEntry<bool> isFlyEnabledConfig;
  private ConfigEntry<float> flySpeedConfig;
  private ConfigEntry<bool> isFlyStationaryConfig;
  private ConfigEntry<bool> nameTagsEnabledConfig;
  private ConfigEntry<bool> hideMyTagConfig;
  private ConfigEntry<int> fontIndexConfig;
  private ConfigEntry<float> nameSizeConfig;
  private ConfigEntry<Vector3> nametagOffsetConfig;
  private ConfigEntry<bool> showFPSConfig;
  private ConfigEntry<float> fpsSizeConfig;
  private ConfigEntry<Vector2> fpsOffsetConfig;
  private ConfigEntry<bool> enableSelfRigLerpConfig;
  private ConfigEntry<float> selfRigLerpAmountConfig;
  private bool localSmoothingReady = false;
  private Vector3 smoothHeadPos;
  private Quaternion smoothHeadRot;
  private Quaternion smoothBodyRot;
  private Vector3 smoothLeftHandPos;
  private Quaternion smoothLeftHandRot;
  private Vector3 smoothRightHandPos;
  private Quaternion smoothRightHandRot;
  private Rect windowRect = new Rect(20f, 20f, 250f, 260f);
  private Rect cameraWindowRect = new Rect(280f, 20f, 280f, 550f);
  private Rect nametagsWindowRect = new Rect(280f, 20f, 280f, 400f);
  private Rect worldWindowRect = new Rect(280f, 20f, 280f, 220f);
  private Rect presetsWindowRect = new Rect(20f, 290f, 250f, 100f);
  private Rect photonWindowRect = new Rect(280f, 20f, 280f, 180f);
  private bool showMainMenu = true;
  private bool showCameraWindow = false;
  private bool showNametagsWindow = false;
  private bool showWorldWindow = false;
  private bool showPresetsWindow = false;
  private bool showPhotonWindow = false;
  private Vector2 cameraScrollPos;
  private Vector2 nametagsScrollPos;
  private Vector2 worldScrollPos;
  private Vector2 presetsScrollPos;
  private RenderTexture cameraOutput;
  private bool stylesInitialized = false;
  private GUIStyle windowStyle;
  private GUIStyle labelStyle;
  private GUIStyle buttonStyle;
  private GUIStyle sliderStyle;
  private GUIStyle thumbStyle;
  private Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
  private InputAction toggleGuiAction;
  private Camera spectatorCam;
  private AudioListener spectatorCamListener;
  private AudioListener mainCamListener;
  private bool isModEnabled = false;
  private VRRig cameraTarget = (VRRig) null;
  private string currentTargetName = "Yourself";
  private Vector3 smoothDampVelocity = Vector3.zero;
  private bool shouldEnableMod = false;
  private bool shouldDisableMod = false;
  private float lerp2 = 1f;
  private float startX = -1f;
  private float startY = -1f;
  private float subThingy = -1f;
  private float subThingyZ = -1f;
  private Vector3 lastPosition = Vector3.zero;
  private readonly Color taggedColor = new Color(1f, 0.3f, 0.3f);
  private readonly Dictionary<VRRig, TemplateGUI.TemplateGUI.PlayerTag> playerTags = new Dictionary<VRRig, TemplateGUI.TemplateGUI.PlayerTag>();
  private readonly List<TMP_FontAsset> allTheFonts = new List<TMP_FontAsset>();
  private TMP_FontAsset currentFont;
  private string roomCodeToJoin = "";

  private void Awake()
  {
    this.SetupConfig();
    new Harmony("com.yourname.gorillag.combinedgui").PatchAll(Assembly.GetExecutingAssembly());
    this.toggleGuiAction = new InputAction("ToggleGUI", (InputActionType) 0, "<Keyboard>/tab", (string) null, (string) null, (string) null);
    this.toggleGuiAction.performed += (Action<InputAction.CallbackContext>) (ctx => this.ToggleMenu());
  }

  private void OnEnable() => this.toggleGuiAction?.Enable();

  private void OnDisable() => this.toggleGuiAction?.Disable();

  private void OnDestroy()
  {
    foreach (Texture2D animatedBorderFrame in this.animatedBorderFrames)
    {
      if (Object.op_Inequality((Object) animatedBorderFrame, (Object) null))
        Object.Destroy((Object) animatedBorderFrame);
    }
    this.animatedBorderFrames.Clear();
    foreach (Texture2D texture2D in this.cachedTextures.Values)
    {
      if (Object.op_Inequality((Object) texture2D, (Object) null))
        Object.Destroy((Object) texture2D);
    }
    this.cachedTextures.Clear();
  }

  private void Update()
  {
    if (this.shouldEnableMod)
    {
      this.EnableMod();
      this.shouldEnableMod = false;
    }
    if (this.shouldDisableMod)
    {
      this.DisableMod();
      this.shouldDisableMod = false;
    }
    if (this.isModEnabled)
    {
      if (PhotonNetwork.InRoom)
        PhotonNetworkController.Instance.disableAFKKick = true;
      this.SwitchTarget();
    }
    if (this.isFlyEnabledConfig.Value && Object.op_Inequality((Object) GorillaTagger.Instance, (Object) null))
      this.WASDFly();
    if (!Object.op_Inequality((Object) GorillaParent.instance, (Object) null))
      return;
    this.UpdateLerpValueForPlayers();
  }

  private void LateUpdate()
  {
    if (Object.op_Inequality((Object) GorillaTagger.Instance, (Object) null))
      this.UpdateSelfRigSmoothing();
    if (this.isModEnabled && Object.op_Inequality((Object) this.spectatorCam, (Object) null) && Object.op_Inequality((Object) Camera.main, (Object) null))
    {
      this.UpdateCamera();
      this.ManageAudioListeners();
    }
    if (!Object.op_Inequality((Object) GorillaParent.instance, (Object) null))
      return;
    this.UpdatePlayerTags();
  }

  private void ManageAudioListeners()
  {
    bool flag = Object.op_Equality((Object) this.cameraTarget, (Object) null) || Object.op_Equality((Object) this.cameraTarget, (Object) GorillaTagger.Instance.offlineVRRig);
    if (!Object.op_Inequality((Object) this.mainCamListener, (Object) null) || !Object.op_Inequality((Object) this.spectatorCamListener, (Object) null))
      return;
    ((Behaviour) this.mainCamListener).enabled = flag;
    ((Behaviour) this.spectatorCamListener).enabled = !flag;
  }

  private void UpdateCamera()
  {
    VRRig vrRig = this.cameraTarget ?? GorillaTagger.Instance.offlineVRRig;
    if (Object.op_Equality((Object) vrRig, (Object) null))
      return;
    if (this.firstPersonToggleConfig.Value)
    {
      Transform transform = Object.op_Equality((Object) vrRig, (Object) GorillaTagger.Instance.offlineVRRig) ? GorillaTagger.Instance.mainCamera.transform : vrRig.head.rigTarget;
      if (Object.op_Inequality((Object) transform, (Object) null))
      {
        ((Component) this.spectatorCam).transform.position = transform.position;
        ((Component) this.spectatorCam).transform.rotation = transform.rotation;
      }
      this.spectatorCam.fieldOfView = this.fovConfig.Value;
      this.spectatorCam.nearClipPlane = this.nearClipConfig.Value;
    }
    else
    {
      Transform transform1 = this.cameraFollowTargetConfig.Value == FollowTarget.Head ? vrRig.head.rigTarget : ((Component) vrRig).transform;
      Transform transform2 = this.cameraFollowTargetConfig.Value == FollowTarget.Head ? vrRig.head.rigTarget : ((Component) vrRig).transform;
      if (Object.op_Equality((Object) vrRig, (Object) GorillaTagger.Instance.offlineVRRig) && this.cameraFollowTargetConfig.Value == FollowTarget.Head && !this.enableSelfRigLerpConfig.Value)
        transform2 = transform1 = GorillaTagger.Instance.mainCamera.transform;
      if (Object.op_Equality((Object) transform1, (Object) null) || Object.op_Equality((Object) transform2, (Object) null))
        return;
      Vector3 vector3 = Vector3.op_Subtraction(Vector3.op_Addition(Vector3.op_Addition(transform1.position, Vector3.op_Multiply(transform2.right, this.xOffsetConfig.Value)), Vector3.op_Multiply(transform2.up, this.yOffsetConfig.Value)), Vector3.op_Multiply(transform2.forward, this.headDistanceConfig.Value));
      Quaternion quaternion = this.cameraFollowTargetConfig.Value != FollowTarget.Head ? Quaternion.Euler(0.0f, transform2.eulerAngles.y, 0.0f) : Quaternion.Euler(transform2.eulerAngles.x, transform2.eulerAngles.y, 0.0f);
      switch (this.smoothingTypeConfig.Value)
      {
        case SmoothingType.Lerp:
          ((Component) this.spectatorCam).transform.position = Vector3.Lerp(((Component) this.spectatorCam).transform.position, vector3, (float) ((double) Time.deltaTime * (double) this.positionSmoothingFactorConfig.Value * 10.0));
          ((Component) this.spectatorCam).transform.rotation = Quaternion.Slerp(((Component) this.spectatorCam).transform.rotation, quaternion, (float) ((double) Time.deltaTime * (double) this.rotationSmoothingFactorConfig.Value * 10.0));
          break;
        case SmoothingType.SmoothDamp:
          ((Component) this.spectatorCam).transform.position = Vector3.SmoothDamp(((Component) this.spectatorCam).transform.position, vector3, ref this.smoothDampVelocity, this.positionSmoothingFactorConfig.Value);
          ((Component) this.spectatorCam).transform.rotation = Quaternion.Slerp(((Component) this.spectatorCam).transform.rotation, quaternion, (float) ((double) Time.deltaTime * (double) this.rotationSmoothingFactorConfig.Value * 10.0));
          break;
        case SmoothingType.Expo:
          ((Component) this.spectatorCam).transform.position = Vector3.Lerp(((Component) this.spectatorCam).transform.position, vector3, 1f - Mathf.Exp((float) (-(double) this.positionSmoothingFactorConfig.Value * 10.0) * Time.deltaTime));
          ((Component) this.spectatorCam).transform.rotation = Quaternion.Slerp(((Component) this.spectatorCam).transform.rotation, quaternion, 1f - Mathf.Exp((float) (-(double) this.rotationSmoothingFactorConfig.Value * 10.0) * Time.deltaTime));
          break;
        case SmoothingType.Lunar:
          ((Component) this.spectatorCam).transform.position = Vector3.SmoothDamp(((Component) this.spectatorCam).transform.position, vector3, ref this.smoothDampVelocity, this.positionSmoothingFactorConfig.Value * 1.5f);
          ((Component) this.spectatorCam).transform.rotation = Quaternion.Slerp(((Component) this.spectatorCam).transform.rotation, quaternion, (float) ((double) Time.deltaTime * (double) this.rotationSmoothingFactorConfig.Value * 5.0));
          break;
        default:
          ((Component) this.spectatorCam).transform.position = vector3;
          ((Component) this.spectatorCam).transform.rotation = quaternion;
          break;
      }
      this.spectatorCam.fieldOfView = this.fovConfig.Value;
      this.spectatorCam.nearClipPlane = this.nearClipConfig.Value;
    }
  }

  private void OnGUI()
  {
    if (this.isModEnabled && Object.op_Inequality((Object) this.cameraOutput, (Object) null))
      GUI.DrawTexture(new Rect(0.0f, 0.0f, (float) Screen.width, (float) Screen.height), (Texture) this.cameraOutput, (ScaleMode) 1, false);
    if (!Object.op_Inequality((Object) GorillaTagger.Instance, (Object) null))
      return;
    if (!this.stylesInitialized)
    {
      this.InitializeGuiStyles();
      this.LoadMyImagesAndFonts();
    }
    if (this.showMainMenu)
    {
      this.UpdateAnimatedStyles();
      GUI.skin.window = this.windowStyle;
      GUI.skin.label = this.labelStyle;
      GUI.skin.button = this.buttonStyle;
      GUI.skin.horizontalSlider = this.sliderStyle;
      GUI.skin.horizontalSliderThumb = this.thumbStyle;
      // ISSUE: method pointer
      this.windowRect = GUI.Window(0, this.windowRect, new GUI.WindowFunction((object) this, __methodptr(MainWindow)), "// dcm \\");
      if (this.showCameraWindow)
      {
        // ISSUE: method pointer
        this.cameraWindowRect = GUI.Window(1, this.cameraWindowRect, new GUI.WindowFunction((object) this, __methodptr(CameraWindow)), "Camera Settings");
      }
      if (this.showNametagsWindow)
      {
        // ISSUE: method pointer
        this.nametagsWindowRect = GUI.Window(2, this.nametagsWindowRect, new GUI.WindowFunction((object) this, __methodptr(NametagsWindow)), "Nametag Settings");
      }
      if (this.showWorldWindow)
      {
        // ISSUE: method pointer
        this.worldWindowRect = GUI.Window(3, this.worldWindowRect, new GUI.WindowFunction((object) this, __methodptr(WorldWindow)), "World Settings");
      }
      if (this.showPresetsWindow)
      {
        // ISSUE: method pointer
        this.presetsWindowRect = GUI.Window(4, this.presetsWindowRect, new GUI.WindowFunction((object) this, __methodptr(PresetsWindow)), "Camera Presets");
      }
      if (this.showPhotonWindow)
      {
        // ISSUE: method pointer
        this.photonWindowRect = GUI.Window(5, this.photonWindowRect, new GUI.WindowFunction((object) this, __methodptr(PhotonWindow)), "Photon");
      }
    }
  }

  private void MainWindow(int windowID)
  {
    float num1 = 30f;
    float num2 = ((Rect) ref this.windowRect).width - 20f;
    if (GUI.Button(new Rect(10f, num1, num2, 25f), this.isModEnabled ? "Unload Camera" : "Load Camera"))
    {
      if (this.isModEnabled)
        this.shouldDisableMod = true;
      else
        this.shouldEnableMod = true;
    }
    float num3 = num1 + 35f;
    if (GUI.Button(new Rect(10f, num3, num2, 25f), "Camera"))
      this.showCameraWindow = !this.showCameraWindow;
    float num4 = num3 + 35f;
    if (GUI.Button(new Rect(10f, num4, num2, 25f), "Presets"))
      this.showPresetsWindow = !this.showPresetsWindow;
    float num5 = num4 + 35f;
    if (GUI.Button(new Rect(10f, num5, num2, 25f), "Nametags"))
      this.showNametagsWindow = !this.showNametagsWindow;
    float num6 = num5 + 35f;
    if (GUI.Button(new Rect(10f, num6, num2, 25f), "Photon"))
      this.showPhotonWindow = !this.showPhotonWindow;
    if (GUI.Button(new Rect(10f, num6 + 35f, num2, 25f), "World"))
      this.showWorldWindow = !this.showWorldWindow;
    GUI.DragWindow();
  }

  private void CameraWindow(int windowID)
  {
    float num1 = ((Rect) ref this.cameraWindowRect).width - 40f;
    this.cameraScrollPos = GUI.BeginScrollView(new Rect(10f, 30f, ((Rect) ref this.cameraWindowRect).width - 20f, ((Rect) ref this.cameraWindowRect).height - 40f), this.cameraScrollPos, new Rect(0.0f, 0.0f, num1, 800f), GUIStyle.none, GUIStyle.none);
    float num2 = 0.0f;
    if (this.isModEnabled)
    {
      GUI.Label(new Rect(0.0f, num2, num1, 20f), "Spectating: " + this.currentTargetName);
      float num3 = num2 + 25f;
      this.firstPersonToggleConfig.Value = this.CustomToggleButton(new Rect(0.0f, num3, num1, 25f), this.firstPersonToggleConfig.Value, "First Person View");
      float num4 = num3 + 35f;
      if (!this.firstPersonToggleConfig.Value)
      {
        if (GUI.Button(new Rect(0.0f, num4, num1, 25f), $"Follow Target: {this.cameraFollowTargetConfig.Value}"))
          this.cameraFollowTargetConfig.Value = (FollowTarget) ((int) (this.cameraFollowTargetConfig.Value + 1) % Enum.GetValues(typeof (FollowTarget)).Length);
        float num5 = num4 + 35f;
        if (GUI.Button(new Rect(0.0f, num5, num1, 25f), $"Smoothing: {this.smoothingTypeConfig.Value}"))
          this.smoothingTypeConfig.Value = (SmoothingType) ((int) (this.smoothingTypeConfig.Value + 1) % Enum.GetValues(typeof (SmoothingType)).Length);
        float num6 = num5 + 35f;
        if (this.smoothingTypeConfig.Value > SmoothingType.None)
        {
          GUI.Label(new Rect(0.0f, num6, num1, 20f), $"Position Speed: {this.positionSmoothingFactorConfig.Value:F2}");
          float num7 = num6 + 20f;
          this.positionSmoothingFactorConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num7, num1, 20f), this.positionSmoothingFactorConfig.Value, 0.01f, 1f);
          float num8 = num7 + 25f;
          GUI.Label(new Rect(0.0f, num8, num1, 20f), $"Rotation Speed: {this.rotationSmoothingFactorConfig.Value:F2}");
          float num9 = num8 + 20f;
          this.rotationSmoothingFactorConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num9, num1, 20f), this.rotationSmoothingFactorConfig.Value, 0.01f, 1f);
          num6 = num9 + 25f;
        }
        GUI.Label(new Rect(0.0f, num6, num1, 20f), $"Head Distance: {this.headDistanceConfig.Value:F2}");
        float num10 = num6 + 20f;
        this.headDistanceConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num10, num1, 20f), this.headDistanceConfig.Value, -1f, 5f);
        float num11 = num10 + 25f;
        GUI.Label(new Rect(0.0f, num11, num1, 20f), $"Y Offset: {this.yOffsetConfig.Value:F2}");
        float num12 = num11 + 20f;
        this.yOffsetConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num12, num1, 20f), this.yOffsetConfig.Value, -1f, 1f);
        float num13 = num12 + 25f;
        GUI.Label(new Rect(0.0f, num13, num1, 20f), $"X Offset: {this.xOffsetConfig.Value:F2}");
        float num14 = num13 + 20f;
        this.xOffsetConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num14, num1, 20f), this.xOffsetConfig.Value, -1f, 1f);
        num4 = num14 + 30f;
      }
      GUI.Label(new Rect(0.0f, num4, num1, 20f), $"FOV: {this.fovConfig.Value:F0}");
      float num15 = num4 + 20f;
      this.fovConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num15, num1, 20f), this.fovConfig.Value, 60f, 120f);
      float num16 = num15 + 25f;
      GUI.Label(new Rect(0.0f, num16, num1, 20f), $"Near Clip: {this.nearClipConfig.Value:F2}");
      float num17 = num16 + 20f;
      this.nearClipConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num17, num1, 20f), this.nearClipConfig.Value, 0.01f, 1f);
      float num18 = num17 + 25f;
      GUI.Label(new Rect(0.0f, num18, num1, 20f), "--- Player Settings ---");
      float num19 = num18 + 25f;
      GUI.Label(new Rect(0.0f, num19, num1, 20f), $"Player Lerp: {this.lerpingConfig.Value:F2}");
      float num20 = num19 + 20f;
      this.lerpingConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num20, num1, 20f), this.lerpingConfig.Value, 0.0f, 3f);
      float num21 = num20 + 25f;
      GUI.Label(new Rect(0.0f, num21, num1, 20f), "--- Self Rig Settings ---");
      float num22 = num21 + 25f;
      this.enableSelfRigLerpConfig.Value = this.CustomToggleButton(new Rect(0.0f, num22, num1, 25f), this.enableSelfRigLerpConfig.Value, "Enable Self Rig Lerp");
      float num23 = num22 + 35f;
      if (this.enableSelfRigLerpConfig.Value)
      {
        GUI.Label(new Rect(0.0f, num23, num1, 20f), $"Self Rig Lerp: {this.selfRigLerpAmountConfig.Value:F2}");
        float num24 = num23 + 20f;
        this.selfRigLerpAmountConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num24, num1, 20f), this.selfRigLerpAmountConfig.Value, -5f, 5f);
        float num25 = num24 + 25f;
      }
    }
    else
      GUI.Label(new Rect(0.0f, num2, num1, 20f), "Load camera to see settings.");
    GUI.EndScrollView();
    GUI.DragWindow();
  }

  private void NametagsWindow(int windowID)
  {
    float num1 = ((Rect) ref this.nametagsWindowRect).width - 40f;
    this.nametagsScrollPos = GUI.BeginScrollView(new Rect(10f, 30f, ((Rect) ref this.nametagsWindowRect).width - 20f, ((Rect) ref this.nametagsWindowRect).height - 40f), this.nametagsScrollPos, new Rect(0.0f, 0.0f, num1, 400f), GUIStyle.none, GUIStyle.none);
    float num2 = 0.0f;
    this.nameTagsEnabledConfig.Value = this.CustomToggleButton(new Rect(0.0f, num2, num1, 25f), this.nameTagsEnabledConfig.Value, "Enable Nametags");
    float num3 = num2 + 35f;
    this.hideMyTagConfig.Value = this.CustomToggleButton(new Rect(0.0f, num3, num1, 25f), this.hideMyTagConfig.Value, "Hide Own Tag");
    float num4 = num3 + 35f;
    this.showFPSConfig.Value = this.CustomToggleButton(new Rect(0.0f, num4, num1, 25f), this.showFPSConfig.Value, "Show FPS");
    float num5 = num4 + 35f;
    string str = Object.op_Inequality((Object) this.currentFont, (Object) null) ? ((Object) this.currentFont).name : "Default";
    if (GUI.Button(new Rect(0.0f, num5, num1, 25f), "Font: " + str) && this.allTheFonts.Count > 0)
    {
      this.fontIndexConfig.Value = (this.fontIndexConfig.Value + 1) % this.allTheFonts.Count;
      this.currentFont = this.allTheFonts[this.fontIndexConfig.Value];
    }
    float num6 = num5 + 35f;
    GUI.Label(new Rect(0.0f, num6, num1, 20f), $"Name Size: {this.nameSizeConfig.Value:F2}");
    float num7 = num6 + 20f;
    this.nameSizeConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num7, num1, 20f), this.nameSizeConfig.Value, 0.1f, 2f);
    float num8 = num7 + 25f;
    GUI.Label(new Rect(0.0f, num8, num1, 20f), $"FPS Size: {this.fpsSizeConfig.Value:F2}");
    float num9 = num8 + 20f;
    this.fpsSizeConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num9, num1, 20f), this.fpsSizeConfig.Value, 0.1f, 2f);
    float num10 = num9 + 30f;
    GUI.Label(new Rect(0.0f, num10, num1, 20f), "--- Element Offsets ---");
    float num11 = num10 + 25f;
    Vector3 vector3 = this.nametagOffsetConfig.Value;
    GUI.Label(new Rect(0.0f, num11, num1, 20f), $"Global Y Offset: {vector3.y:F2}");
    float num12 = num11 + 20f;
    vector3.y = GUI.HorizontalSlider(new Rect(0.0f, num12, num1, 20f), vector3.y, -2f, 2f);
    float num13 = num12 + 25f;
    this.nametagOffsetConfig.Value = vector3;
    Vector2 vector2 = this.fpsOffsetConfig.Value;
    GUI.Label(new Rect(0.0f, num13, num1, 20f), $"FPS Y Offset: {vector2.y:F2}");
    float num14 = num13 + 20f;
    vector2.y = GUI.HorizontalSlider(new Rect(0.0f, num14, num1, 20f), vector2.y, -1f, 1f);
    float num15 = num14 + 25f;
    this.fpsOffsetConfig.Value = vector2;
    GUI.EndScrollView();
    GUI.DragWindow();
  }

  private void WorldWindow(int windowID)
  {
    float num1 = ((Rect) ref this.worldWindowRect).width - 40f;
    this.worldScrollPos = GUI.BeginScrollView(new Rect(10f, 30f, ((Rect) ref this.worldWindowRect).width - 20f, ((Rect) ref this.worldWindowRect).height - 40f), this.worldScrollPos, new Rect(0.0f, 0.0f, num1, 150f), GUIStyle.none, GUIStyle.none);
    float num2 = 0.0f;
    GUI.Label(new Rect(0.0f, num2, num1, 20f), "--- Player Mods ---");
    float num3 = num2 + 30f;
    this.isFlyEnabledConfig.Value = this.CustomToggleButton(new Rect(0.0f, num3, num1, 25f), this.isFlyEnabledConfig.Value, "WASD Fly");
    float num4 = num3 + 35f;
    this.isFlyStationaryConfig.Value = this.CustomToggleButton(new Rect(20f, num4, num1 - 20f, 25f), this.isFlyStationaryConfig.Value, "Stationary Fly");
    float num5 = num4 + 35f;
    GUI.Label(new Rect(0.0f, num5, num1, 20f), $"Fly Speed: {this.flySpeedConfig.Value:F1}");
    float num6 = num5 + 20f;
    this.flySpeedConfig.Value = GUI.HorizontalSlider(new Rect(0.0f, num6, num1, 20f), this.flySpeedConfig.Value, 1f, 50f);
    float num7 = num6 + 25f;
    GUI.EndScrollView();
    GUI.DragWindow();
  }

  private void PresetsWindow(int windowID)
  {
    float num1 = ((Rect) ref this.presetsWindowRect).width - 20f;
    this.presetsScrollPos = GUI.BeginScrollView(new Rect(10f, 30f, ((Rect) ref this.presetsWindowRect).width - 20f, ((Rect) ref this.presetsWindowRect).height - 40f), this.presetsScrollPos, new Rect(0.0f, 0.0f, num1, 300f), GUIStyle.none, GUIStyle.none);
    float num2 = 0.0f;
    if (GUI.Button(new Rect(0.0f, num2, num1, 25f), "Good Cfg Idk"))
      this.LoadPreset("Good Cfg Idk");
    float num3 = num2 + 35f;
    GUI.EndScrollView();
    GUI.DragWindow();
  }

  private void PhotonWindow(int windowID)
  {
    float num1 = 30f;
    float num2 = ((Rect) ref this.photonWindowRect).width - 20f;
    GUI.Label(new Rect(10f, num1, num2, 20f), "Room Code:");
    float num3 = num1 + 25f;
    this.roomCodeToJoin = GUI.TextField(new Rect(10f, num3, num2, 25f), this.roomCodeToJoin);
    float num4 = num3 + 35f;
    if (GUI.Button(new Rect(10f, num4, num2, 25f), "Join Room") && !string.IsNullOrEmpty(this.roomCodeToJoin))
      PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(this.roomCodeToJoin.ToUpper(), JoinType.Solo);
    if (GUI.Button(new Rect(10f, num4 + 35f, num2, 25f), "Disconnect"))
      PhotonNetwork.Disconnect();
    GUI.DragWindow();
  }

  private void LoadPreset(string presetName)
  {
    if (!(presetName == "Good Cfg Idk"))
      return;
    this.positionSmoothingFactorConfig.Value = 0.61f;
    this.rotationSmoothingFactorConfig.Value = 0.66f;
    this.fovConfig.Value = 90f;
    this.nearClipConfig.Value = 0.21f;
    this.headDistanceConfig.Value = 2.59f;
    this.yOffsetConfig.Value = 0.4f;
    this.xOffsetConfig.Value = 0.0f;
    this.lerpingConfig.Value = 0.53f;
    this.smoothingTypeConfig.Value = SmoothingType.Lerp;
    this.cameraFollowTargetConfig.Value = FollowTarget.Head;
  }

  private void UpdateLerpValueForPlayers()
  {
    if ((double) this.lerp2 == (double) this.lerpingConfig.Value)
      return;
    foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
    {
      if (Object.op_Inequality((Object) vrrig, (Object) GorillaTagger.Instance.offlineVRRig))
      {
        vrrig.lerpValueBody = GorillaTagger.Instance.offlineVRRig.lerpValueBody * this.lerpingConfig.Value;
        vrrig.lerpValueFingers = GorillaTagger.Instance.offlineVRRig.lerpValueFingers * this.lerpingConfig.Value;
      }
    }
    this.lerp2 = this.lerpingConfig.Value;
  }

  private void WASDFly()
  {
    if (Keyboard.current == null)
      return;
    bool isPressed1 = ((ButtonControl) Keyboard.current.wKey).isPressed;
    bool isPressed2 = ((ButtonControl) Keyboard.current.aKey).isPressed;
    bool isPressed3 = ((ButtonControl) Keyboard.current.sKey).isPressed;
    bool isPressed4 = ((ButtonControl) Keyboard.current.dKey).isPressed;
    bool isPressed5 = ((ButtonControl) Keyboard.current.spaceKey).isPressed;
    bool isPressed6 = ((ButtonControl) Keyboard.current.leftCtrlKey).isPressed;
    if (this.isFlyStationaryConfig.Value | isPressed1 | isPressed2 | isPressed3 | isPressed4 | isPressed5 | isPressed6)
      GorillaTagger.Instance.rigidbody.velocity = Vector3.zero;
    Transform transform1 = ((Component) Camera.main).transform;
    if (Mouse.current != null && Mouse.current.rightButton.isPressed)
    {
      Quaternion rotation = transform1.rotation;
      Vector3 eulerAngles = ((Quaternion) ref rotation).eulerAngles;
      Vector2 vector2 = ((InputControl<Vector2>) ((Pointer) Mouse.current).position).ReadValue();
      if ((double) this.startX < 0.0)
      {
        this.startX = eulerAngles.y;
        this.subThingy = vector2.x / (float) Screen.width;
      }
      if ((double) this.startY < 0.0)
      {
        this.startY = eulerAngles.x;
        this.subThingyZ = vector2.y / (float) Screen.height;
      }
      float num1 = this.startX + (float) (((double) vector2.x / (double) Screen.width - (double) this.subThingy) * 360.0);
      float num2 = this.startY - (float) (((double) vector2.y / (double) Screen.height - (double) this.subThingyZ) * 360.0);
      transform1.rotation = Quaternion.Euler(num2, num1, eulerAngles.z);
    }
    else
    {
      this.startX = -1f;
      this.startY = -1f;
    }
    float num = ((ButtonControl) Keyboard.current.leftShiftKey).isPressed ? this.flySpeedConfig.Value * 2f : this.flySpeedConfig.Value;
    Vector3 vector3 = Vector3.zero;
    if (isPressed1)
      vector3 = Vector3.op_Addition(vector3, transform1.forward);
    if (isPressed3)
      vector3 = Vector3.op_Subtraction(vector3, transform1.forward);
    if (isPressed4)
      vector3 = Vector3.op_Addition(vector3, transform1.right);
    if (isPressed2)
      vector3 = Vector3.op_Subtraction(vector3, transform1.right);
    Transform transform2 = ((Component) GorillaTagger.Instance).transform;
    transform2.position = Vector3.op_Addition(transform2.position, Vector3.op_Multiply(Vector3.op_Multiply(((Vector3) ref vector3).normalized, num), Time.deltaTime));
    if (isPressed5)
    {
      Transform transform3 = ((Component) GorillaTagger.Instance).transform;
      transform3.position = Vector3.op_Addition(transform3.position, Vector3.op_Multiply(Vector3.op_Multiply(Vector3.up, num), Time.deltaTime));
    }
    if (isPressed6)
    {
      Transform transform4 = ((Component) GorillaTagger.Instance).transform;
      transform4.position = Vector3.op_Subtraction(transform4.position, Vector3.op_Multiply(Vector3.op_Multiply(Vector3.up, num), Time.deltaTime));
    }
    if (!isPressed1 && !isPressed2 && !isPressed3 && !isPressed4 && !isPressed5 && !isPressed6 && Vector3.op_Inequality(this.lastPosition, Vector3.zero) && this.isFlyStationaryConfig.Value)
      ((Component) GorillaTagger.Instance).transform.position = this.lastPosition;
    else
      this.lastPosition = ((Component) GorillaTagger.Instance).transform.position;
  }

  private void SwitchTarget()
  {
    if (Keyboard.current == null || Object.op_Equality((Object) GorillaParent.instance, (Object) null))
      return;
    List<VRRig> list = GorillaParent.instance.vrrigs.Where<VRRig>((Func<VRRig, bool>) (r => Object.op_Inequality((Object) r, (Object) null) && Object.op_Inequality((Object) r, (Object) GorillaTagger.Instance.offlineVRRig))).ToList<VRRig>();
    if (list.Count != 0)
    {
      int index = -1;
      if (((ButtonControl) Keyboard.current.digit1Key).wasPressedThisFrame)
        index = 0;
      else if (((ButtonControl) Keyboard.current.digit2Key).wasPressedThisFrame)
        index = 1;
      else if (((ButtonControl) Keyboard.current.digit3Key).wasPressedThisFrame)
        index = 2;
      else if (((ButtonControl) Keyboard.current.digit4Key).wasPressedThisFrame)
        index = 3;
      else if (((ButtonControl) Keyboard.current.digit5Key).wasPressedThisFrame)
        index = 4;
      else if (((ButtonControl) Keyboard.current.digit6Key).wasPressedThisFrame)
        index = 5;
      else if (((ButtonControl) Keyboard.current.digit7Key).wasPressedThisFrame)
        index = 6;
      else if (((ButtonControl) Keyboard.current.digit8Key).wasPressedThisFrame)
        index = 7;
      else if (((ButtonControl) Keyboard.current.digit9Key).wasPressedThisFrame)
        index = 8;
      if (((ButtonControl) Keyboard.current.digit0Key).wasPressedThisFrame)
      {
        this.cameraTarget = (VRRig) null;
        this.currentTargetName = "Yourself";
      }
      else if (index != -1 && index < list.Count)
      {
        this.cameraTarget = list[index];
        TextMeshPro playerText1 = this.cameraTarget.playerText1;
        this.currentTargetName = (Object.op_Inequality((Object) playerText1, (Object) null) ? ((TMP_Text) playerText1).text : (string) null) ?? "Unknown";
      }
    }
  }

  private void EnableMod()
  {
    if (Object.op_Equality((Object) this.spectatorCam, (Object) null) && Object.op_Inequality((Object) Camera.main, (Object) null))
    {
      GameObject gameObject = new GameObject("// dcm \\ SpectatorCam");
      this.spectatorCam = gameObject.AddComponent<Camera>();
      this.spectatorCam.cameraType = (CameraType) 4;
      this.spectatorCam.cullingMask = Camera.main.cullingMask;
      this.spectatorCamListener = gameObject.AddComponent<AudioListener>();
      this.mainCamListener = ((Component) Camera.main).GetComponent<AudioListener>();
      gameObject.tag = "Untagged";
    }
    if (Object.op_Equality((Object) this.cameraOutput, (Object) null))
      this.cameraOutput = new RenderTexture(Screen.width, Screen.height, 24);
    if (Object.op_Inequality((Object) this.spectatorCam, (Object) null))
    {
      this.spectatorCam.targetTexture = this.cameraOutput;
      ((Component) this.spectatorCam).gameObject.SetActive(true);
    }
    this.isModEnabled = true;
  }

  private void DisableMod()
  {
    if (Object.op_Inequality((Object) this.spectatorCam, (Object) null))
    {
      Object.Destroy((Object) ((Component) this.spectatorCam).gameObject);
      this.spectatorCam = (Camera) null;
    }
    if (Object.op_Inequality((Object) this.cameraOutput, (Object) null))
    {
      this.cameraOutput.Release();
      this.cameraOutput = (RenderTexture) null;
    }
    if (Object.op_Inequality((Object) this.mainCamListener, (Object) null))
      ((Behaviour) this.mainCamListener).enabled = true;
    this.isModEnabled = false;
  }

  private void UpdatePlayerTags()
  {
    GorillaParent instance1 = GorillaParent.instance;
    if ((Object.op_Inequality((Object) instance1, (Object) null) ? instance1.vrrigs : (List<VRRig>) null) == null)
      return;
    HashSet<VRRig> activeRigs = new HashSet<VRRig>(GorillaParent.instance.vrrigs.Where<VRRig>((Func<VRRig, bool>) (r => Object.op_Inequality((Object) r, (Object) null) && ((Behaviour) r).isActiveAndEnabled)));
    GorillaTagger instance2 = GorillaTagger.Instance;
    if (Object.op_Inequality(Object.op_Inequality((Object) instance2, (Object) null) ? (Object) instance2.offlineVRRig : (Object) null, (Object) null))
      activeRigs.Add(GorillaTagger.Instance.offlineVRRig);
    foreach (VRRig key in this.playerTags.Keys.Where<VRRig>((Func<VRRig, bool>) (rig => Object.op_Equality((Object) rig, (Object) null) || !activeRigs.Contains(rig))).ToList<VRRig>())
    {
      TemplateGUI.TemplateGUI.PlayerTag playerTag;
      if (this.playerTags.TryGetValue(key, out playerTag) && Object.op_Inequality((Object) playerTag.go, (Object) null))
        Object.Destroy((Object) playerTag.go);
      this.playerTags.Remove(key);
    }
    foreach (VRRig vrRig in activeRigs)
    {
      if (!Object.op_Equality((Object) vrRig, (Object) null))
      {
        bool flag = Object.op_Equality((Object) vrRig, (Object) GorillaTagger.Instance.offlineVRRig);
        if (flag && this.hideMyTagConfig.Value)
        {
          TemplateGUI.TemplateGUI.PlayerTag playerTag;
          if (this.playerTags.TryGetValue(vrRig, out playerTag) && Object.op_Inequality((Object) playerTag.go, (Object) null))
            playerTag.go.SetActive(false);
        }
        else
        {
          TemplateGUI.TemplateGUI.PlayerTag playerTag;
          if (!this.playerTags.TryGetValue(vrRig, out playerTag) || Object.op_Equality((Object) playerTag.go, (Object) null))
          {
            playerTag = this.MakeAPlayerTag(vrRig);
            this.playerTags[vrRig] = playerTag;
          }
          playerTag.go.SetActive(this.nameTagsEnabledConfig.Value);
          if (this.nameTagsEnabledConfig.Value)
          {
            Transform transform = flag ? GorillaTagger.Instance.mainCamera.transform : vrRig.head.rigTarget;
            if (!Object.op_Equality((Object) transform, (Object) null))
            {
              playerTag.go.transform.position = Vector3.op_Addition(transform.position, this.nametagOffsetConfig.Value);
              if (Object.op_Inequality((Object) this.spectatorCam, (Object) null) && ((Behaviour) this.spectatorCam).isActiveAndEnabled)
                playerTag.go.transform.rotation = ((Component) this.spectatorCam).transform.rotation;
              else if (Object.op_Inequality((Object) Camera.main, (Object) null))
                playerTag.go.transform.rotation = ((Component) Camera.main).transform.rotation;
              ((TMP_Text) playerTag.nameTMP).text = vrRig.playerNameVisible;
              ((Graphic) playerTag.nameTMP).color = this.IsPlayerIt(vrRig) ? this.taggedColor : vrRig.playerColor;
              playerTag.nameTMP.transform.localScale = Vector3.op_Multiply(Vector3.one, this.nameSizeConfig.Value);
              if (Object.op_Inequality((Object) this.currentFont, (Object) null))
                ((TMP_Text) playerTag.nameTMP).font = this.currentFont;
              playerTag.fpsGO.SetActive(this.showFPSConfig.Value);
              if (this.showFPSConfig.Value)
              {
                int playerFps = this.GetPlayerFPS(vrRig);
                ((TMP_Text) playerTag.fpsTMP).text = $"{playerFps} FPS";
                ((Graphic) playerTag.fpsTMP).color = playerFps > 72 ? Color.green : (playerFps > 60 ? Color.yellow : this.taggedColor);
                if (Object.op_Inequality((Object) this.currentFont, (Object) null))
                  ((TMP_Text) playerTag.fpsTMP).font = this.currentFont;
                playerTag.fpsGO.transform.localPosition = Vector2.op_Implicit(this.fpsOffsetConfig.Value);
                playerTag.fpsGO.transform.localScale = Vector3.op_Multiply(Vector3.one, this.fpsSizeConfig.Value);
              }
            }
          }
        }
      }
    }
  }

  private void ToggleNameTags()
  {
    this.nameTagsEnabledConfig.Value = !this.nameTagsEnabledConfig.Value;
  }

  private TemplateGUI.TemplateGUI.PlayerTag MakeAPlayerTag(VRRig r)
  {
    GameObject gameObject1 = new GameObject("Nametag_" + r.playerNameVisible);
    GameObject gameObject2 = new GameObject("Name");
    gameObject2.transform.SetParent(gameObject1.transform, false);
    TextMeshPro textMeshPro1 = gameObject2.AddComponent<TextMeshPro>();
    ((TMP_Text) textMeshPro1).alignment = (TextAlignmentOptions) 514;
    ((TMP_Text) textMeshPro1).fontSize = 4f;
    ((TMP_Text) textMeshPro1).rectTransform.sizeDelta = new Vector2(3f, 1f);
    ((TMP_Text) textMeshPro1).enableWordWrapping = false;
    ((TMP_Text) textMeshPro1).overflowMode = (TextOverflowModes) 0;
    GameObject gameObject3 = new GameObject("FPS");
    gameObject3.transform.SetParent(gameObject1.transform, false);
    TextMeshPro textMeshPro2 = gameObject3.AddComponent<TextMeshPro>();
    ((TMP_Text) textMeshPro2).alignment = (TextAlignmentOptions) 514;
    ((TMP_Text) textMeshPro2).fontSize = 2f;
    ((TMP_Text) textMeshPro2).rectTransform.sizeDelta = new Vector2(2f, 0.5f);
    return new TemplateGUI.TemplateGUI.PlayerTag()
    {
      go = gameObject1,
      nameTMP = textMeshPro1,
      fpsGO = gameObject3,
      fpsTMP = textMeshPro2
    };
  }

  private int GetPlayerFPS(VRRig rig)
  {
    try
    {
      Traverse traverse = Traverse.Create((object) rig).Field("fps");
      if (!traverse.FieldExists() || !(traverse.GetValue() is int _))
        ;
    }
    catch
    {
    }
    return 0;
  }

  private bool IsPlayerIt(VRRig rig)
  {
    bool flag;
    if (Object.op_Inequality((Object) rig, (Object) null))
    {
      SkinnedMeshRenderer mainSkin = rig.mainSkin;
      if (Object.op_Equality((Object) mainSkin, (Object) null))
      {
        flag = false;
      }
      else
      {
        Material material = ((Renderer) mainSkin).material;
        flag = (Object.op_Inequality((Object) material, (Object) null) ? new bool?(((Object) material).name.ToLower().Contains("infec")) : new bool?()).GetValueOrDefault();
      }
    }
    else
      flag = false;
    return flag;
  }

  private void SetupConfig()
  {
    this.cameraFollowTargetConfig = this.Config.Bind<FollowTarget>("Camera", "FollowTarget", FollowTarget.Head, "Which part of the rig the camera should follow.");
    this.fovConfig = this.Config.Bind<float>("Camera", "FieldOfView", 90f, "The field of view for the spectator camera.");
    this.nearClipConfig = this.Config.Bind<float>("Camera", "NearClip", 0.01f, "The near clipping plane of the camera.");
    this.headDistanceConfig = this.Config.Bind<float>("Camera", "HeadDistance", 1.5f, "How far the camera is from the player's head.");
    this.yOffsetConfig = this.Config.Bind<float>("Camera", "Y_Offset", 0.2f, "The vertical offset of the camera.");
    this.xOffsetConfig = this.Config.Bind<float>("Camera", "X_Offset", 0.0f, "The horizontal offset of the camera.");
    this.firstPersonToggleConfig = this.Config.Bind<bool>("Camera", "FirstPerson", false, "Toggles a first-person perspective, ignoring all offsets and smoothing.");
    this.smoothingTypeConfig = this.Config.Bind<SmoothingType>("Smoothing", "Type", SmoothingType.None, "The type of smoothing to apply to camera movement.");
    this.positionSmoothingFactorConfig = this.Config.Bind<float>("Smoothing", "PositionSpeed", 0.2f, "How quickly the camera's position updates.");
    this.rotationSmoothingFactorConfig = this.Config.Bind<float>("Smoothing", "RotationSpeed", 0.2f, "How quickly the camera's rotation updates.");
    this.lerpingConfig = this.Config.Bind<float>("Player", "Lerping", 1f, "How much to smooth other players' movements.");
    this.enableSelfRigLerpConfig = this.Config.Bind<bool>("Self Rig", "EnableSelfRigLerp", false, "Enable smoothing/lerping for your own rig.");
    this.selfRigLerpAmountConfig = this.Config.Bind<float>("Self Rig", "SelfRigLerpAmount", 1f, "How much to smooth your own rig's movements. Positive values smooth, negative values jitter.");
    this.isFlyEnabledConfig = this.Config.Bind<bool>("World", "FlyEnabled", false, "Enable WASD flying.");
    this.flySpeedConfig = this.Config.Bind<float>("World", "FlySpeed", 15f, "The speed of WASD flying.");
    this.isFlyStationaryConfig = this.Config.Bind<bool>("World", "StationaryFly", true, "Whether to stay in place when not moving.");
    this.nameTagsEnabledConfig = this.Config.Bind<bool>("Nametags", "Enabled", true, "Enable player nametags.");
    this.hideMyTagConfig = this.Config.Bind<bool>("Nametags", "HideOwnTag", false, "Hide your own nametag.");
    this.fontIndexConfig = this.Config.Bind<int>("Nametags", "FontIndex", 0, "The index of the font to use for nametags.");
    this.nameSizeConfig = this.Config.Bind<float>("Nametags", "NameSize", 1f, "The size of the player name text.");
    this.nametagOffsetConfig = this.Config.Bind<Vector3>("Nametags", "GlobalOffset", new Vector3(0.0f, 0.4f, 0.0f), "The overall vertical offset for nametags.");
    this.showFPSConfig = this.Config.Bind<bool>("Nametags", "ShowFPS", true, "Show player FPS below their name.");
    this.fpsSizeConfig = this.Config.Bind<float>("Nametags", "FpsSize", 0.5f, "The size of the FPS text.");
    this.fpsOffsetConfig = this.Config.Bind<Vector2>("Nametags", "FpsOffset", new Vector2(0.0f, -0.5f), "The vertical offset for the FPS text.");
  }

  private Texture2D LoadImageFromResource(string filename)
  {
    Texture2D texture2D1;
    try
    {
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      string name = ((IEnumerable<string>) executingAssembly.GetManifestResourceNames()).Single<string>((Func<string, bool>) (str => str.EndsWith(filename)));
      using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(name))
      {
        if (manifestResourceStream == null)
        {
          texture2D1 = (Texture2D) null;
        }
        else
        {
          byte[] buffer = new byte[manifestResourceStream.Length];
          manifestResourceStream.Read(buffer, 0, buffer.Length);
          Texture2D texture2D2 = new Texture2D(2, 2);
          ImageConversion.LoadImage(texture2D2, buffer);
          texture2D1 = texture2D2;
        }
      }
    }
    catch (Exception ex)
    {
      this.Logger.LogError((object) $"Failed to load resource {filename}: {ex.Message}");
      texture2D1 = (Texture2D) null;
    }
    return texture2D1;
  }

  private void LoadMyImagesAndFonts()
  {
    try
    {
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      IEnumerable<string> strings = ((IEnumerable<string>) executingAssembly.GetManifestResourceNames()).Where<string>((Func<string, bool>) (res => res.EndsWith(".ttf") || res.EndsWith(".otf")));
      TMP_FontAsset builtinResource = Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.ttf");
      if (Object.op_Inequality((Object) builtinResource, (Object) null))
      {
        ((Object) builtinResource).name = "Default";
        this.allTheFonts.Add(builtinResource);
      }
      foreach (string str in strings)
      {
        using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(str))
        {
          if (manifestResourceStream != null)
          {
            byte[] numArray = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(numArray, 0, numArray.Length);
            string path = Path.Combine(Application.temporaryCachePath, Path.GetFileName(str));
            File.WriteAllBytes(path, numArray);
            Font font = new Font(path);
            if (Object.op_Inequality((Object) font, (Object) null))
            {
              TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
              ((Object) fontAsset).name = Path.GetFileNameWithoutExtension(str);
              this.allTheFonts.Add(fontAsset);
            }
            File.Delete(path);
          }
        }
      }
    }
    catch (Exception ex)
    {
      this.Logger.LogError((object) ("Error loading fonts: " + ex.Message));
    }
  }

  private Texture2D MakeSolidColorTex(int width, int height, Color col)
  {
    Color[] colorArray = new Color[width * height];
    for (int index = 0; index < colorArray.Length; ++index)
      colorArray[index] = col;
    Texture2D texture2D = new Texture2D(width, height);
    texture2D.SetPixels(colorArray);
    texture2D.Apply();
    return texture2D;
  }

  private bool CustomToggleButton(Rect position, bool value, string text)
  {
    string str = value ? text + ": ON" : text + ": OFF";
    return !GUI.Button(position, str) ? value : !value;
  }

  private void UpdateAnimatedStyles()
  {
    if (this.animatedBorderFrames.Count <= 0)
      return;
    Texture2D animatedBorderFrame = this.animatedBorderFrames[(int) ((double) Time.time * 30.0) % this.animatedBorderFrames.Count];
    this.windowStyle.normal.background = animatedBorderFrame;
    this.windowStyle.onNormal.background = animatedBorderFrame;
  }

  private void InitializeGuiStyles()
  {
    Color color1;
    // ISSUE: explicit constructor call
    ((Color) ref color1).\u002Ector(0.05f, 0.05f, 0.05f, 0.9f);
    Color color2;
    // ISSUE: explicit constructor call
    ((Color) ref color2).\u002Ector(0.8f, 0.1f, 0.1f);
    Color color3;
    // ISSUE: explicit constructor call
    ((Color) ref color3).\u002Ector(0.1f, 0.1f, 0.1f, 0.9f);
    Color white = Color.white;
    this.windowStyle = new GUIStyle(GUI.skin.window)
    {
      border = new RectOffset(12, 12, 12, 12),
      padding = new RectOffset(10, 10, 25, 10),
      fontStyle = (FontStyle) 1
    };
    if (this.animatedBorderFrames.Count == 0)
    {
      int num = 60;
      for (int index = 0; index < num; ++index)
      {
        float time = (float) ((double) index / (double) num * 6.2831854820251465);
        this.animatedBorderFrames.Add(this.MakeAnimatedRoundTexture(64 /*0x40*/, 64 /*0x40*/, color1, Color.black, Color.red, 4, 16 /*0x10*/, time));
      }
    }
    if (this.animatedBorderFrames.Count > 0)
    {
      this.windowStyle.normal.background = this.animatedBorderFrames[0];
      this.windowStyle.onNormal.background = this.animatedBorderFrames[0];
    }
    this.labelStyle = new GUIStyle(GUI.skin.label)
    {
      normal = {
        textColor = white
      },
      alignment = (TextAnchor) 3
    };
    this.buttonStyle = new GUIStyle(GUI.skin.button)
    {
      normal = {
        background = this.MakeRoundTexture(32 /*0x20*/, 32 /*0x20*/, color1, color2, 2, 8),
        textColor = white
      },
      hover = {
        background = this.MakeRoundTexture(32 /*0x20*/, 32 /*0x20*/, color2, color2, 2, 8),
        textColor = white
      },
      active = {
        background = this.MakeRoundTexture(32 /*0x20*/, 32 /*0x20*/, color2, color2, 2, 8),
        textColor = white
      },
      border = new RectOffset(8, 8, 8, 8),
      padding = new RectOffset(0, 0, 2, 0)
    };
    this.sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
    {
      normal = {
        background = this.MakeRoundTexture(32 /*0x20*/, 6, color3, 3)
      },
      fixedHeight = 6f,
      border = new RectOffset(3, 3, 3, 3),
      margin = new RectOffset(4, 4, 7, 7)
    };
    this.thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
    {
      normal = {
        background = this.MakeSquareTexture(12, 12, color2)
      },
      hover = {
        background = this.MakeSquareTexture(12, 12, color2)
      },
      active = {
        background = this.MakeSquareTexture(12, 12, color2)
      },
      fixedWidth = 12f,
      fixedHeight = 12f,
      border = new RectOffset(0, 0, 0, 0)
    };
    this.stylesInitialized = true;
  }

  private Texture2D MakeSquareTexture(int width, int height, Color color)
  {
    string key = $"sqtex_{width}x{height}_{color}";
    Texture2D texture2D1;
    if (this.cachedTextures.ContainsKey(key))
    {
      texture2D1 = this.cachedTextures[key];
    }
    else
    {
      Texture2D texture2D2 = new Texture2D(width, height);
      Color[] colorArray = new Color[width * height];
      for (int index = 0; index < colorArray.Length; ++index)
        colorArray[index] = color;
      texture2D2.SetPixels(colorArray);
      texture2D2.Apply();
      this.cachedTextures[key] = texture2D2;
      texture2D1 = texture2D2;
    }
    return texture2D1;
  }

  private Texture2D MakeRoundTexture(int width, int height, Color color, int radius)
  {
    return this.MakeRoundTexture(width, height, color, Color.clear, 0, radius);
  }

  private Texture2D MakeRoundTexture(
    int width,
    int height,
    Color color,
    Color border,
    int borderSize,
    int radius)
  {
    string key = $"tex_{width}x{height}_{color}_{border}_{borderSize}_{radius}";
    Texture2D texture2D1;
    if (this.cachedTextures.ContainsKey(key))
    {
      texture2D1 = this.cachedTextures[key];
    }
    else
    {
      Texture2D texture2D2 = new Texture2D(width, height, (TextureFormat) 4, false);
      for (int y = 0; y < height; ++y)
      {
        for (int x = 0; x < width; ++x)
          texture2D2.SetPixel(x, y, this.GetPixelColor(x, y, width, height, color, border, borderSize, radius));
      }
      texture2D2.Apply();
      this.cachedTextures[key] = texture2D2;
      texture2D1 = texture2D2;
    }
    return texture2D1;
  }

  private Texture2D MakeAnimatedRoundTexture(
    int width,
    int height,
    Color color,
    Color border1,
    Color border2,
    int borderSize,
    int radius,
    float time)
  {
    Texture2D texture2D = new Texture2D(width, height, (TextureFormat) 4, false);
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        float num = (float) (((double) Mathf.Sin((float) (((double) (Mathf.Atan2((float) y - (float) height / 2f, (float) x - (float) width / 2f) * 57.29578f) + 180.0) / 360.0 * 3.1415927410125732 * 2.0) + time) + 1.0) / 2.0);
        Color border = Color.Lerp(border1, border2, num);
        texture2D.SetPixel(x, y, this.GetPixelColor(x, y, width, height, color, border, borderSize, radius));
      }
    }
    texture2D.Apply();
    return texture2D;
  }

  private Color GetPixelColor(
    int x,
    int y,
    int width,
    int height,
    Color color,
    Color border,
    int borderSize,
    int radius)
  {
    float num = -1f;
    if (x < radius && y < radius)
      num = Vector2.Distance(new Vector2((float) x, (float) y), new Vector2((float) radius, (float) radius));
    else if (x > width - radius && y < radius)
      num = Vector2.Distance(new Vector2((float) x, (float) y), new Vector2((float) (width - radius), (float) radius));
    else if (x < radius && y > height - radius)
      num = Vector2.Distance(new Vector2((float) x, (float) y), new Vector2((float) radius, (float) (height - radius)));
    else if (x > width - radius && y > height - radius)
      num = Vector2.Distance(new Vector2((float) x, (float) y), new Vector2((float) (width - radius), (float) (height - radius)));
    return (double) num <= (double) radius ? (x >= borderSize && x < width - borderSize && y >= borderSize && y < height - borderSize && ((double) num == -1.0 || (double) num <= (double) (radius - borderSize)) ? color : border) : Color.clear;
  }

  private void ToggleMenu()
  {
    this.showMainMenu = !this.showMainMenu;
    Cursor.lockState = this.showMainMenu ? (CursorLockMode) 0 : (CursorLockMode) 1;
    Cursor.visible = this.showMainMenu;
  }

  private void SetupSmoothRig()
  {
    if (!Object.op_Inequality((Object) GorillaTagger.Instance.offlineVRRig, (Object) null))
      return;
    Transform transform = ((Component) GorillaTagger.Instance.headCollider).transform;
    Transform leftHandTransform = GorillaTagger.Instance.leftHandTransform;
    Transform rightHandTransform = GorillaTagger.Instance.rightHandTransform;
    this.smoothHeadPos = transform.position;
    this.smoothHeadRot = transform.rotation;
    Quaternion rotation = transform.rotation;
    this.smoothBodyRot = Quaternion.Euler(0.0f, ((Quaternion) ref rotation).eulerAngles.y, 0.0f);
    this.smoothLeftHandPos = leftHandTransform.position;
    this.smoothLeftHandRot = leftHandTransform.rotation;
    this.smoothRightHandPos = rightHandTransform.position;
    this.smoothRightHandRot = rightHandTransform.rotation;
    this.localSmoothingReady = true;
  }

  private void UpdateSelfRigSmoothing()
  {
    GorillaTagger instance = GorillaTagger.Instance;
    VRRig offlineVrRig = Object.op_Inequality((Object) instance, (Object) null) ? instance.offlineVRRig : (VRRig) null;
    if (Object.op_Equality((Object) offlineVrRig, (Object) null))
      return;
    if (!this.enableSelfRigLerpConfig.Value)
    {
      if (!this.localSmoothingReady)
        return;
      ((Behaviour) offlineVrRig).enabled = true;
      this.localSmoothingReady = false;
    }
    else
    {
      ((Behaviour) offlineVrRig).enabled = false;
      if (!this.localSmoothingReady)
        this.SetupSmoothRig();
      Transform transform = ((Component) GorillaTagger.Instance.headCollider).transform;
      Transform leftHandTransform = GorillaTagger.Instance.leftHandTransform;
      Transform rightHandTransform = GorillaTagger.Instance.rightHandTransform;
      float num1 = this.selfRigLerpAmountConfig.Value;
      if ((double) num1 >= 0.0)
      {
        float num2 = Mathf.Lerp(30f, 0.5f, num1 / 5f);
        this.smoothHeadPos = Vector3.Lerp(this.smoothHeadPos, transform.position, Time.deltaTime * num2);
        this.smoothHeadRot = Quaternion.Slerp(this.smoothHeadRot, transform.rotation, Time.deltaTime * num2);
        Quaternion smoothBodyRot = this.smoothBodyRot;
        Quaternion rotation = transform.rotation;
        Quaternion quaternion = Quaternion.Euler(0.0f, ((Quaternion) ref rotation).eulerAngles.y, 0.0f);
        double num3 = (double) Time.deltaTime * (double) num2;
        this.smoothBodyRot = Quaternion.Slerp(smoothBodyRot, quaternion, (float) num3);
        this.smoothLeftHandPos = Vector3.Lerp(this.smoothLeftHandPos, leftHandTransform.position, Time.deltaTime * num2);
        this.smoothLeftHandRot = Quaternion.Slerp(this.smoothLeftHandRot, leftHandTransform.rotation, Time.deltaTime * num2);
        this.smoothRightHandPos = Vector3.Lerp(this.smoothRightHandPos, rightHandTransform.position, Time.deltaTime * num2);
        this.smoothRightHandRot = Quaternion.Slerp(this.smoothRightHandRot, rightHandTransform.rotation, Time.deltaTime * num2);
      }
      else if ((double) Random.value < 1.0 - (double) Mathf.Abs(num1) / 5.0999999046325684)
      {
        this.smoothHeadPos = transform.position;
        this.smoothHeadRot = transform.rotation;
        Quaternion rotation = transform.rotation;
        this.smoothBodyRot = Quaternion.Euler(0.0f, ((Quaternion) ref rotation).eulerAngles.y, 0.0f);
        this.smoothLeftHandPos = leftHandTransform.position;
        this.smoothLeftHandRot = leftHandTransform.rotation;
        this.smoothRightHandPos = rightHandTransform.position;
        this.smoothRightHandRot = rightHandTransform.rotation;
      }
      ((Component) offlineVrRig).transform.position = Vector3.op_Subtraction(this.smoothHeadPos, new Vector3(0.0f, 0.15f, 0.0f));
      ((Component) offlineVrRig).transform.rotation = this.smoothBodyRot;
      offlineVrRig.head.rigTarget.rotation = this.smoothHeadRot;
      Quaternion quaternion1 = Quaternion.Euler(180f, 180f, 0.0f);
      offlineVrRig.leftHand.rigTarget.position = this.smoothLeftHandPos;
      offlineVrRig.leftHand.rigTarget.rotation = Quaternion.op_Multiply(this.smoothLeftHandRot, quaternion1);
      offlineVrRig.rightHand.rigTarget.position = this.smoothRightHandPos;
      offlineVrRig.rightHand.rigTarget.rotation = Quaternion.op_Multiply(this.smoothRightHandRot, quaternion1);
    }
  }

  private class PlayerTag
  {
    public GameObject go;
    public TextMeshPro nameTMP;
    public GameObject fpsGO;
    public TextMeshPro fpsTMP;
  }
}
