using BepInEx;
using LC_API.BundleAPI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using UnityEngine;

namespace MetalPipe;

[BepInPlugin("Isbjorn52.MetalPipe", "Metal Pipe", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    //private Harmony _harmony;
    //public static ManualLogSource logger;

    public void Awake()
    {
        //logger = Logger;

        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        
        new Hook(typeof(Shovel).GetMethod(nameof(Shovel.Start), bindingFlags), typeof(Plugin).GetMethod(nameof(Shovel_Start)));
        
        new ILHook(typeof(Shovel).GetMethod(nameof(Shovel.HitShovelClientRpc), bindingFlags), IL_Shovel_Hit);
        new ILHook(typeof(Shovel).GetMethod(nameof(Shovel.HitShovel), bindingFlags), IL_Shovel_Hit);
    }

    public static void IL_Shovel_Hit(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(
            x => x.MatchCall<RoundManager>(nameof(RoundManager.PlayRandomClip))
            );

        cursor.Index -= 7; // This is bad maybe?

        float maxDistance = 1f;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Shovel>>((self) =>
        {
            maxDistance = self.shovelAudio.maxDistance;
            self.shovelAudio.maxDistance = 50f;
        });

        // Audio plays

        cursor.GotoNext(
            x => x.MatchCall<RoundManager>(nameof(RoundManager.PlayRandomClip))
            );

        cursor.Index += 2;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Shovel>>((self) =>
        {
            // Reset audio distance
            self.shovelAudio.maxDistance = maxDistance;
        });
    }

    public static void Shovel_Start(Action<Shovel> orig, Shovel self)
    {
        orig(self);

        if (self.transform.Find("mesh") != null)
        {
            self.hitSFX = new AudioClip[] { BundleLoader.GetLoadedAsset<AudioClip>("Assets/MetalPipe.mp3") };
            Material metalMaterial = self.transform.Find("mesh").GetComponent<MeshRenderer>().materials[0];
            GameObject.Destroy(self.transform.Find("mesh").gameObject);
            GameObject metalPipe = GameObject.Instantiate(BundleLoader.GetLoadedAsset<GameObject>("Assets/MetalPipe.prefab"), self.transform);
            metalPipe.GetComponentInChildren<MeshRenderer>().material = metalMaterial;
        }
        //else
        //{
        //    Debug.Log("Mesh is null?!?");
        //}
    }
}
