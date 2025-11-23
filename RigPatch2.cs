// Decompiled with JetBrains decompiler
// Type: TemplateGUI.RigPatch2
// Assembly: DCM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 50D3356B-D964-482F-86F0-A1FDFD923388
// Assembly location: C:\Users\asher\Downloads\DCM.dll

using HarmonyLib;
using UnityEngine;

#nullable disable
namespace TemplateGUI;

[HarmonyPatch(typeof (VRRig), "Awake")]
public class RigPatch2
{
  public static bool Prefix(VRRig __instance)
  {
    return ((Object) ((Component) __instance).gameObject).name != "Local Gorilla Player(Clone)";
  }
}
