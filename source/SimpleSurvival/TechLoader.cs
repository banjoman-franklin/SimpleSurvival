﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Add the tech icon to the R&D tech tree.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TechLoader : MonoBehaviour
    {
        /// <summary>
        /// Part->Texture dict to apply to RnD part icons
        /// </summary>
        public static Dictionary<AvailablePart, Texture2D> IconTextures =
            new Dictionary<AvailablePart, Texture2D>();

        public void Awake()
        {
            KSP.UI.Screens.RDController.OnRDTreeSpawn.Add(OnRDTreeSpawn);
        }

        private void OnRDTreeSpawn(KSP.UI.Screens.RDController rd)
        {
            Util.Log("Call: OnRDTreeSpawn");

            SetPartIcons();
            SetNodeIcon(rd);
        }

        /// <summary>
        /// Update the RnD part icons that have had their textures
        /// procedurally modified.
        /// </summary>
        private void SetPartIcons()
        {
            foreach (AvailablePart part in IconTextures.Keys)
            {
                Util.Log($"Updating iconPrefab for {part.name}");
                MeshRenderer mesh = part.iconPrefab.GetComponentInChildren<MeshRenderer>();

                if (mesh == null)
                {
                    Util.Warn($"MeshRenderer is null for {part.name}, skipping...");
                    continue;
                }
                try
                {
                    mesh.material.mainTexture = IconTextures[part];
                    mesh.sharedMaterial.mainTexture = IconTextures[part];
                }
                catch (NullReferenceException)
                {
                    Util.Warn($"Caught NRE while updating iconPrefab for {part.name}");
                }
            }
        }

        /// <summary>
        /// Set the tech tree node icon for the new node.
        /// </summary>
        /// <param name="rd"></param>
        /// <param name="iconName"></param>
        private void SetNodeIcon(KSP.UI.Screens.RDController rd)
        {
            string refname = "RD_node_icon_simplesurvivalbasic";
            // This file has the .truecolor extension in order to force KSP
            // to use this icon without mipmaps. The stock toolbar and RnD icons
            // do not scale down with lower user-set texture resolutions,
            // so neither should this.
            Texture2D texture = GameDatabase.Instance.GetTexture($"SimpleSurvival/Tech/{refname}", false);
            RUI.Icons.Simple.Icon icon = new RUI.Icons.Simple.Icon(refname, texture);

            rd.iconLoader.iconDictionary.Add(refname, icon);

            // Custom nodes will be last on the list.
            // Speed up load time by beginning iteration backwards.
            for (int i = rd.nodes.Count - 1; i >= 0; i--)
            {
                KSP.UI.Screens.RDNode node = rd.nodes[i];

                // This is the "id" field in the tech tree config
                if (node.tech.techID == "simplesurvivalBasic")
                {
                    Util.Log("Found SimpleSurvival Basic tech node");

                    // Sets the large icon in the righthand info panel
                    node.icon = icon;
                    node.iconRef = refname;

                    // Sets the tree icon
                    node.graphics.SetIcon(icon);

                    node.UpdateGraphics();
                    return;
                }
            }
        }
    }
}
