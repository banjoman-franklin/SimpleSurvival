﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    /// <summary>
    /// EVA LifeSupport PartModule, lives in EVA Kerbals.
    /// </summary>
    public class EVALifeSupportModule : PartModule
    {
        public override void OnStart(StartState state)
        {
            Util.Log("EVALifeSupportModule OnStart()");

            // To avoid conflicts with Unity variable "name"
            // Assume EVA Kerbals will always have exactly one ProtoCrewMember
            string kerbal_name = part.protoModuleCrew[0].name;

            PartResource resource = null;

            foreach (PartResource pr in part.Resources)
            {
                if (pr.resourceName == C.NAME_EVA_LIFESUPPORT)
                {
                    resource = pr;
                    break;
                }
            }

            // Kerbals assigned after this mod's installation should already be tracked,
            // but for Kerbals already in flight, add EVA LS according to current state
            // of astronaut complex
            EVALifeSupportTracker.AddKerbalToTracking(kerbal_name);

            if (resource == null)
            {
                // If not found, add EVA LS resource to this PartModule.
                Util.Log("Adding " + C.NAME_EVA_LIFESUPPORT + " resource to " + part.name);

                var info = EVALifeSupportTracker.GetEVALSInfo(kerbal_name);

                ConfigNode resource_node = new ConfigNode("RESOURCE");
                resource_node.AddValue("name", C.NAME_EVA_LIFESUPPORT);
                resource_node.AddValue("amount", info.current.ToString());
                resource_node.AddValue("maxAmount", info.max.ToString());

                resource = part.AddResource(resource_node);

                Util.Log("Added EVA LS resource to " + part.name);
            }
            else
            {
                // If found, this EVA is already active - deduct LS.
                Util.StartupRequest(this, C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC);

                if (resource.amount < C.KILL_BUFFER)
                    resource.amount = C.KILL_BUFFER;
                else if (resource.amount < C.EVA_LS_30_SECONDS)
                    Util.PostUpperMessage(kerbal_name + " has " + (int)(resource.amount / C.EVA_LS_DRAIN_PER_SEC) + " seconds to live!", 1);
            }

            base.OnStart(state);
        }

        public void FixedUpdate()
        {
            PartResource resource = part.Resources[C.NAME_EVA_LIFESUPPORT];
            double initial_value = resource.amount;
            string kerbal_name = part.protoModuleCrew[0].name;

            // Update tracking info.
            // While Kerbal is in EVA, PartResource contains the "primary" value,
            // and tracking is only updated as a consequence.
            // It will be a frame behind, but that should be okay.
            EVALifeSupportTracker.SetCurrentEVAAmount(kerbal_name, initial_value);

            // If Kerbal is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
                return;

            // -- Reduce resource --
            double retd = part.RequestResource(C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime);

            if (initial_value > C.EVA_LS_30_SECONDS &&
                resource.amount <= C.EVA_LS_30_SECONDS)
            {
                TimeWarp.SetRate(0, true);
                Util.PostUpperMessage(kerbal_name + " has 30 seconds to live!", 1);
                resource.amount = C.EVA_LS_30_SECONDS;
            }

            // Necessary to check if crew count > 0?
            if (retd == 0.0)
            {
                Util.KillKerbals(this);
                part.explode();
            }
        }
    }
}
