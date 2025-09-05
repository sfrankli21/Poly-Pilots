using System.Collections.Generic;

namespace AerodynamicObjects
{
    /// <summary>
    /// The flow interaction manager maintains a look up table for ID pairs of all objects which can interact.
    /// It is queried whenever an interaction occurs to ensure that interaction is allowed.
    /// </summary>
    public static class FlowInteractionManager
    {
        static int nextID = 0;

        public static int GetUniqueID()
        {
            // Because we have two distinct types of objects interacting in pairs, we could have separate
            // IDs for each and it wouldn't matter if an object and a flow primitive had the same ID number
            // this could vastly increase the potential number of unique IDs, but seeing as we already have
            // a few billion at our disposal I don't think we need to bother
            return nextID++;
        }

        static Dictionary<int, HashSet<int>> objectFlowIgnoreInteractions = new Dictionary<int, HashSet<int>>();
        static HashSet<int> flowIdHashSet;

        public static int NumEntries => objectFlowIgnoreInteractions.Count;

        public static void IgnoreInteraction(int affectedByFlowID, int flowInfluencerID)
        {
            if (!objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out HashSet<int> flowIDHashSet))
            {
                flowIDHashSet = new HashSet<int>();
                objectFlowIgnoreInteractions[affectedByFlowID] = flowIDHashSet;
            }

            flowIDHashSet.Add(flowInfluencerID);

            //// If the object's ID has no existing entries then we add a new entry to the dictionary
            //if (!objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    objectFlowIgnoreInteractions.Add(affectedByFlowID, new List<int>());
            //    // And we add the flow primitive ID to the list of flow primitives to ignore for this object
            //    objectFlowIgnoreInteractions[affectedByFlowID].Add(flowInfluencerID);
            //}
            //else
            //{
            //    // If the ID already exists then we need to make sure the flow primitive ID isn't already added
            //    if (objectFlowIgnoreInteractions[affectedByFlowID].Contains(flowInfluencerID))
            //    {
            //        // If the flow primitive ID is already in the list then the interaction is already ignored
            //        // and we don't need to do anything
            //        return;
            //    }

            //    // Otherwise, we need to add the flow primitive ID to the list of flow primitives to ignore for this object
            //    objectFlowIgnoreInteractions[affectedByFlowID].Add(flowInfluencerID);
            //}
        }

        public static bool IsInteractionIgnored(int affectedByFlowID, int flowInfluencerID)
        {
            return objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out flowIdHashSet) && flowIdHashSet.Contains(flowInfluencerID);

            //// First check if there is an entry for the object
            //if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    // If the object has an entry in the ignore interactions table, then check if the list of IDs
            //    // contains the ID of the flow primitive
            //    if (objectFlowIgnoreInteractions[affectedByFlowID].Contains(flowInfluencerID))
            //    {
            //        // If both the object and the flow primitive are listed in this way then
            //        // we have an entry which means we need to ignore the interaction
            //        return true;
            //    }
            //}

            //// If we didn't find the object and flow primitive IDs as a pair then they have not been
            //// added to the ignore interactions table and they should interact with each other
            //return false;
        }

        public static void AddIgnoreList(int affectedByFlowID, List<FlowPrimitive> ignoreList)
        {
            if (!objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out HashSet<int> flowIDHashSet))
            {
                flowIDHashSet = new HashSet<int>();
                objectFlowIgnoreInteractions[affectedByFlowID] = flowIDHashSet;
            }

            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (ignoreList[i] != null)
                {
                    flowIDHashSet.Add(ignoreList[i].interactionID);
                }
            }
            //// If there's already an entry for the object then we need to make sure all the primitives are being ignored
            //if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    List<int> currentIgnores = objectFlowIgnoreInteractions[affectedByFlowID];

            //    for (int i = 0; i < ignoreList.Count; i++)
            //    {
            //        // If we're not already ignoring the primitive's ID then add it to the ignore list
            //        if (ignoreList[i] != null && !currentIgnores.Contains(ignoreList[i].interactionID))
            //        {
            //            objectFlowIgnoreInteractions[affectedByFlowID].Add(ignoreList[i].interactionID);
            //        }
            //    }
            //}
            //else
            //{
            //    // Add all of the ignore list IDs to the dictionary
            //    objectFlowIgnoreInteractions.Add(affectedByFlowID, ignoreList.Select(o => o.interactionID).ToList());
            //}
        }

        public static void AddIgnoreList(int affectedByFlowID, List<FluidVolume> ignoreList)
        {
            if (!objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out HashSet<int> flowIDHashSet))
            {
                flowIDHashSet = new HashSet<int>();
                objectFlowIgnoreInteractions[affectedByFlowID] = flowIDHashSet;
            }

            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (ignoreList[i] != null)
                {
                    flowIDHashSet.Add(ignoreList[i].interactionID);
                }
            }

            //// If there's already an entry for the object then we need to make sure all the primitives are being ignored
            //if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    List<int> currentIgnores = objectFlowIgnoreInteractions[affectedByFlowID];

            //    for (int i = 0; i < ignoreList.Count; i++)
            //    {
            //        // If we're not already ignoring the primitive's ID then add it to the ignore list
            //        if (ignoreList[i] != null && !currentIgnores.Contains(ignoreList[i].interactionID))
            //        {
            //            objectFlowIgnoreInteractions[affectedByFlowID].Add(ignoreList[i].interactionID);
            //        }
            //    }
            //}
            //else
            //{
            //    // Add all of the ignore list IDs to the dictionary
            //    objectFlowIgnoreInteractions.Add(affectedByFlowID, ignoreList.Select(o => o.interactionID).ToList());
            //}
        }

        public static void RemoveIgnoreList(int affectedByFlowID, List<FlowPrimitive> ignoreList)
        {
            if (!objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out HashSet<int> flowIDHashSet))
            {
                return;
            }

            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (ignoreList[i] != null)
                {
                    flowIDHashSet.Remove(ignoreList[i].interactionID);
                }
            }

            //// If there's already an entry for the object then we need to make sure all the primitives are being ignored
            //if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    List<int> currentIgnores = objectFlowIgnoreInteractions[affectedByFlowID];

            //    for (int i = 0; i < ignoreList.Count; i++)
            //    {
            //        // If we're not already ignoring the primitive's ID then add it to the ignore list
            //        if (ignoreList[i] != null && currentIgnores.Contains(ignoreList[i].interactionID))
            //        {
            //            objectFlowIgnoreInteractions[affectedByFlowID].Remove(ignoreList[i].interactionID);
            //        }
            //    }
            //}
        }

        public static void RemoveIgnoreList(int affectedByFlowID, List<FluidVolume> ignoreList)
        {
            if (!objectFlowIgnoreInteractions.TryGetValue(affectedByFlowID, out HashSet<int> flowIDHashSet))
            {
                return;
            }

            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (ignoreList[i] != null)
                {
                    flowIDHashSet.Remove(ignoreList[i].interactionID);
                }
            }

            //// If there's already an entry for the object then we need to make sure all the primitives are being ignored
            //if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            //{
            //    List<int> currentIgnores = objectFlowIgnoreInteractions[affectedByFlowID];

            //    for (int i = 0; i < ignoreList.Count; i++)
            //    {
            //        // If we're not already ignoring the primitive's ID then add it to the ignore list
            //        if (ignoreList[i] != null && currentIgnores.Contains(ignoreList[i].interactionID))
            //        {
            //            objectFlowIgnoreInteractions[affectedByFlowID].Remove(ignoreList[i].interactionID);
            //        }
            //    }
            //}
        }

        public static void RemoveFlowAffected(int affectedByFlowID)
        {
            if (objectFlowIgnoreInteractions.ContainsKey(affectedByFlowID))
            {
                objectFlowIgnoreInteractions.Remove(affectedByFlowID);
            }
        }
    }
}
