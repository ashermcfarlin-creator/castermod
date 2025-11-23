// Decompiled with JetBrains decompiler
// Type: TemplateGUI.RigPatch3
// Assembly: DCM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 50D3356B-D964-482F-86F0-A1FDFD923388
// Assembly location: C:\Users\asher\Downloads\DCM.dll

using HarmonyLib;

#nullable disable
namespace TemplateGUI;

[HarmonyPatch(typeof (VRRigJobManager), "DeregisterVRRig")]
public static class RigPatch3
{
  public static bool Prefix(VRRigJobManager __instance, VRRig rig) => !rig.isLocal;
}
